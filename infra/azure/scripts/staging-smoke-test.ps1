# Post-deploy smoke tests for ContentForge staging/production.
# Usage:
#   $env:WEB_HOST = "app-cf-web-staging.azurewebsites.net"
#   ./infra/azure/scripts/staging-smoke-test.ps1
#
# Optional:
#   $env:ADMIN_EMAIL / $env:ADMIN_PASSWORD — run authenticated admin API checks
#   $env:PUBLIC_SLUG — public content slug to probe (default: skipped if unset)

param(
    [string]$WebHost = $env:WEB_HOST,
    [string]$AdminEmail = $env:ADMIN_EMAIL,
    [string]$AdminPassword = $env:ADMIN_PASSWORD,
    [string]$PublicSlug = $env:PUBLIC_SLUG,
    [int]$MaxAttempts = 30,
    [int]$RetrySeconds = 10
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($WebHost)) {
    Write-Error "WEB_HOST is required (e.g. app-cf-web-staging.azurewebsites.net)."
}

$baseUrl = "https://$WebHost"
$failures = New-Object System.Collections.Generic.List[string]

function Invoke-SmokeRequest {
    param(
        [Parameter(Mandatory = $true)][string]$Uri,
        [string]$Method = "GET",
        [hashtable]$Headers = $null,
        [string]$ContentType = $null,
        [string]$Body = $null,
        [int]$TimeoutSec = 30
    )

    # Windows PowerShell 5.1 has no -SkipHttpErrorCheck; capture HTTP error responses instead.
    # Note: [string]$Body = $null becomes "" — never attach Body for GET/HEAD.
    try {
        $params = @{
            Uri             = $Uri
            Method          = $Method
            UseBasicParsing = $true
            TimeoutSec      = $TimeoutSec
        }
        if ($null -ne $Headers) { $params.Headers = $Headers }
        if (-not [string]::IsNullOrWhiteSpace($ContentType)) { $params.ContentType = $ContentType }
        if (-not [string]::IsNullOrEmpty($Body)) { $params.Body = $Body }

        return Invoke-WebRequest @params
    }
    catch [System.Net.WebException] {
        $response = $_.Exception.Response
        if ($null -eq $response) {
            throw
        }

        $statusCode = [int]$response.StatusCode
        $stream = $response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($stream)
        try {
            $content = $reader.ReadToEnd()
        }
        finally {
            $reader.Dispose()
        }

        return [pscustomobject]@{
            StatusCode = $statusCode
            Content    = $content
        }
    }
}

function Test-Eventually {
    param(
        [string]$Name,
        [scriptblock]$Probe,
        [int]$Attempts = $MaxAttempts,
        [int]$DelaySeconds = $RetrySeconds
    )

    for ($i = 1; $i -le $Attempts; $i++) {
        try {
            $result = & $Probe
            if ($result) {
                Write-Host "PASS: $Name (attempt $i)"
                return $true
            }

            Write-Host "WAIT: $Name (attempt $i/$Attempts) - probe returned false"
        }
        catch {
            Write-Host "WAIT: $Name (attempt $i/$Attempts) - $($_.Exception.Message)"
        }

        Start-Sleep -Seconds $DelaySeconds
    }

    $failures.Add($Name) | Out-Null
    Write-Host "FAIL: $Name"
    return $false
}

Write-Host "=== ContentForge smoke tests ==="
Write-Host "Target: $baseUrl"
Write-Host ""

# Frontend shell
Test-Eventually "Frontend (HTML shell)" {
    $response = Invoke-SmokeRequest -Uri "$baseUrl/"
    return ($response.StatusCode -eq 200 -and $response.Content -match "(?i)<!doctype html>")
} | Out-Null

# Web nginx health
Test-Eventually "Web /health" {
    $response = Invoke-SmokeRequest -Uri "$baseUrl/health"
    return $response.StatusCode -eq 200
} | Out-Null

# API liveness via Web proxy (API blocks direct public access in staging/production)
Test-Eventually "API /health/live via proxy" {
    $response = Invoke-SmokeRequest -Uri "$baseUrl/health/live"
    return $response.StatusCode -eq 200
} | Out-Null

# API readiness (DB connectivity, migrations applied, blob storage)
Test-Eventually "API /health/ready via proxy (database + storage)" {
    $response = Invoke-SmokeRequest -Uri "$baseUrl/health/ready" -TimeoutSec 60
    if ($response.StatusCode -ne 200) {
        Write-Host "  ready body: $($response.Content)"
        return $false
    }

    $payload = $response.Content | ConvertFrom-Json
    if ($payload.status -ne "Healthy") {
        Write-Host "  ready status: $($payload.status)"
        return $false
    }

    return $true
} | Out-Null

# Authentication endpoint (expect rejection without credentials)
Test-Eventually "Authentication login endpoint" {
    $body = '{"email":"smoke-check@invalid.local","password":"invalid-password"}'
    $response = Invoke-SmokeRequest -Uri "$baseUrl/api/v1/auth/login" -Method POST `
        -ContentType "application/json" -Body $body
    return @(400, 401, 422) -contains [int]$response.StatusCode
} | Out-Null

# Administrative API (requires auth)
if (-not [string]::IsNullOrWhiteSpace($AdminEmail) -and -not [string]::IsNullOrWhiteSpace($AdminPassword)) {
    Test-Eventually "Administrative API (authenticated)" {
        $loginBody = @{ email = $AdminEmail; password = $AdminPassword } | ConvertTo-Json
        $login = Invoke-SmokeRequest -Uri "$baseUrl/api/v1/auth/login" -Method POST `
            -ContentType "application/json" -Body $loginBody
        $payload = $login.Content | ConvertFrom-Json
        $token = $payload.accessToken
        if ([string]::IsNullOrWhiteSpace($token)) { return $false }

        $headers = @{ Authorization = "Bearer $token" }
        $dashboard = Invoke-SmokeRequest -Uri "$baseUrl/api/v1/admin/dashboard" -Headers $headers
        return $dashboard.StatusCode -eq 200
    } | Out-Null
}
else {
    Write-Host "SKIP: Administrative API (set ADMIN_EMAIL and ADMIN_PASSWORD to enable)"
}

# Public API
if (-not [string]::IsNullOrWhiteSpace($PublicSlug)) {
    Test-Eventually "Public API content by slug" {
        $response = Invoke-SmokeRequest -Uri "$baseUrl/api/v1/public/content/$PublicSlug"
        return @(200, 404) -contains [int]$response.StatusCode
    } | Out-Null
}
else {
    Write-Host "SKIP: Public API slug probe (set PUBLIC_SLUG to enable)"
}

Write-Host ""
if ($failures.Count -gt 0) {
    Write-Host "=== Smoke tests FAILED ==="
    $failures | ForEach-Object { Write-Host " - $_" }
    exit 1
}

Write-Host "=== Smoke tests PASSED ==="
exit 0
