# syntax=docker/dockerfile:1.7

# Production multi-stage image for ContentForge.Api
# Build context: repository root (see infra/docker/README.md)

ARG DOTNET_SDK_IMAGE=mcr.microsoft.com/dotnet/sdk:8.0.424-noble-amd64
ARG DOTNET_ASPNET_IMAGE=mcr.microsoft.com/dotnet/aspnet:8.0-alpine-amd64

FROM ${DOTNET_SDK_IMAGE} AS build
WORKDIR /src

# Pin SDK via global.json and keep repo-relative paths so .editorconfig globs apply
COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./

COPY src/ContentForge.Domain/ContentForge.Domain.csproj src/ContentForge.Domain/
COPY src/ContentForge.Application/ContentForge.Application.csproj src/ContentForge.Application/
COPY src/ContentForge.Infrastructure/ContentForge.Infrastructure.csproj src/ContentForge.Infrastructure/
COPY src/ContentForge.Api/ContentForge.Api.csproj src/ContentForge.Api/

RUN --mount=type=cache,target=/root/.nuget/packages,id=contentforge-nuget \
    dotnet restore src/ContentForge.Api/ContentForge.Api.csproj

COPY src/ContentForge.Domain/ src/ContentForge.Domain/
COPY src/ContentForge.Application/ src/ContentForge.Application/
COPY src/ContentForge.Infrastructure/ src/ContentForge.Infrastructure/
COPY src/ContentForge.Api/ src/ContentForge.Api/

RUN --mount=type=cache,target=/root/.nuget/packages,id=contentforge-nuget \
    dotnet publish src/ContentForge.Api/ContentForge.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false \
    /p:Deterministic=true \
    && rm -f /app/publish/appsettings.Development.json /app/publish/appsettings.Testing.json

FROM ${DOTNET_ASPNET_IMAGE} AS final

RUN addgroup -g 10001 -S appgroup \
    && adduser -u 10001 -S appuser -G appgroup -h /app -s /sbin/nologin \
    && mkdir -p /app/App_Data/media \
    && chown -R appuser:appgroup /app \
    && apk add --no-cache icu-libs wget \
    && rm -rf /var/cache/apk/*

WORKDIR /app

COPY --from=build --chown=appuser:appgroup /app/publish ./

USER appuser

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080 \
    DOTNET_EnableDiagnostics=0 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    Media__LocalRoot=/app/App_Data/media

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=45s --retries=3 \
    CMD wget -qO- http://127.0.0.1:8080/health/live >/dev/null 2>&1 || exit 1

ENTRYPOINT ["dotnet", "ContentForge.Api.dll"]
