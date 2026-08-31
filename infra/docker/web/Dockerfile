# syntax=docker/dockerfile:1.7

# Production multi-stage image for the ContentForge admin SPA (static + nginx reverse proxy)
# Build context: repository root (see infra/docker/README.md)

ARG NODE_IMAGE=node:20-alpine3.21
ARG NGINX_IMAGE=nginxinc/nginx-unprivileged:1.27-alpine

FROM ${NODE_IMAGE} AS build
WORKDIR /app

ENV npm_config_update_notifier=false \
    npm_config_fund=false

COPY frontend/contentforge-web/package.json frontend/contentforge-web/package-lock.json ./

RUN --mount=type=cache,target=/root/.npm,id=contentforge-npm \
    npm ci --ignore-scripts \
    && npm rebuild esbuild

COPY frontend/contentforge-web/ ./

# Empty default: browser calls same-origin /api; nginx proxies to the API service.
ARG VITE_API_BASE_URL=
ENV VITE_API_BASE_URL=${VITE_API_BASE_URL}

RUN npm run build \
    && rm -rf node_modules

FROM ${NGINX_IMAGE} AS final

ENV API_UPSTREAM=api:8080 \
    API_UPSTREAM_SCHEME=http \
    API_UPSTREAM_HOST=api \
    NGINX_ENVSUBST_OUTPUT_DIR=/etc/nginx/conf.d

COPY infra/docker/web/nginx.conf.template /etc/nginx/templates/default.conf.template
COPY --from=build --chown=nginx:nginx /app/dist /usr/share/nginx/html

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD wget -qO- http://127.0.0.1:8080/health >/dev/null 2>&1 || exit 1
