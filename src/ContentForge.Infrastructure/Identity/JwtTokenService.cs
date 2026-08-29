namespace ContentForge.Infrastructure.Identity;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ContentForge.Domain.Authorization;
using ContentForge.Infrastructure.Authorization;
using ContentForge.Infrastructure.Options;
using ContentForge.Infrastructure.Persistence.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

internal sealed class JwtTokenService(IOptions<JwtOptions> options, TimeProvider timeProvider)
{
    internal const string SecurityStampClaimType = "sstamp";

    internal (string Token, DateTimeOffset ExpiresAt) CreateAccessToken(ContentForgeUser user, RoleName role)
    {
        var jwtOptions = options.Value;
        var expiresAt = timeProvider.GetUtcNow().AddMinutes(jwtOptions.AccessTokenLifetimeMinutes);
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Name, user.DisplayName),
            new(ClaimTypes.Name, user.DisplayName),
            new(ClaimTypes.Role, role.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(SecurityStampClaimType, user.SecurityStamp ?? string.Empty),
        };

        if (DefaultRoleDefinitions.All.TryGetValue(role, out var roleDefinition))
        {
            foreach (var permission in roleDefinition.Permissions)
            {
                claims.Add(new Claim(PermissionAuthorizationHandler.PermissionClaimType, permission.Value));
            }
        }

        var token = new JwtSecurityToken(
            issuer: jwtOptions.Issuer,
            audience: jwtOptions.Audience,
            claims: claims,
            notBefore: timeProvider.GetUtcNow().UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    internal static string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    internal static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}
