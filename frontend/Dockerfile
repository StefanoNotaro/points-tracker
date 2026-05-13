# syntax=docker/dockerfile:1.7

# ---- Build ----
FROM node:22-alpine AS build
WORKDIR /app

COPY package*.json ./
RUN npm ci --prefer-offline --no-audit --fund=false

COPY . .
RUN npm run build -- --configuration production

# ---- Runtime ----
FROM nginx:alpine AS runtime

# Minimal SPA-friendly config for standalone image use. The reverse proxy in
# docker-compose (see docker/nginx/) handles TLS + API/SignalR upstreams; this
# config just serves the bundle and routes deep links back to index.html.
COPY nginx.conf /etc/nginx/conf.d/default.conf

COPY --from=build /app/dist/frontend/browser /usr/share/nginx/html

EXPOSE 8080
