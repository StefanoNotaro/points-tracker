# Deployment

## Platform

**TrueNAS Scale** running Docker containers via Docker Compose.
All services run in isolated containers with no host-level dependencies beyond Docker.

---

## Services

| Service         | Image                    | Port (internal) | Notes                              |
|-----------------|--------------------------|-----------------|------------------------------------|
| `frontend`      | Custom Nginx image       | 80              | Serves Angular SPA + proxies API   |
| `api`           | Custom .NET 9 image      | 8080            | .NET Minimal API + SignalR         |
| `db`            | `postgres:17-alpine`     | 5432            | PostgreSQL                         |

---

## Docker Compose Structure

```
docker/
├── docker-compose.yml            # Production-ready base
├── docker-compose.override.yml   # Local dev overrides (hot reload, debug ports)
├── nginx/
│   ├── nginx.conf                # Main Nginx config
│   └── default.conf              # Site config — routing, proxy, headers
└── postgres/
    └── init.sql                  # DB init scripts (roles, extensions)
```

---

## `docker-compose.yml` (skeleton)

```yaml
version: "3.9"

services:

  frontend:
    build:
      context: ../frontend
      dockerfile: Dockerfile
    depends_on:
      - api
    restart: unless-stopped
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx/nginx.conf:/etc/nginx/nginx.conf:ro
      - ./nginx/default.conf:/etc/nginx/conf.d/default.conf:ro
      - /path/to/certs:/etc/nginx/certs:ro
    networks:
      - pts-net

  api:
    build:
      context: ../backend
      dockerfile: Dockerfile
    restart: unless-stopped
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__Default=${DB_CONNECTION_STRING}
      - Authentik__Authority=${AUTHENTIK_AUTHORITY}
      - Authentik__ClientId=${AUTHENTIK_CLIENT_ID}
      - ShareToken__Secret=${SHARE_TOKEN_SECRET}
    depends_on:
      db:
        condition: service_healthy
    networks:
      - pts-net

  db:
    image: postgres:17-alpine
    restart: unless-stopped
    environment:
      POSTGRES_DB: points_tracker
      POSTGRES_USER: ${DB_USER}
      POSTGRES_PASSWORD: ${DB_PASSWORD}
    volumes:
      - db-data:/var/lib/postgresql/data
      - ./postgres/init.sql:/docker-entrypoint-initdb.d/init.sql:ro
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${DB_USER} -d points_tracker"]
      interval: 10s
      timeout: 5s
      retries: 5
    networks:
      - pts-net

volumes:
  db-data:

networks:
  pts-net:
    driver: bridge
```

---

## Environment Variables

All secrets are stored in a `.env` file on the TrueNAS host, **never committed to git**.

```dotenv
# Database
DB_USER=pts_app
DB_PASSWORD=<strong-random-password>
DB_CONNECTION_STRING=Host=db;Port=5432;Database=points_tracker;Username=pts_app;Password=<password>

# Authentik
AUTHENTIK_AUTHORITY=https://auth.yourdomain.com/application/o/points-tracker/
AUTHENTIK_CLIENT_ID=<authentik-client-id>

# Share tokens
SHARE_TOKEN_SECRET=<64-char-random-hex>
```

A `.env.example` file with all keys (no values) is committed to the repository.

---

## Nginx Reverse Proxy Config

```nginx
server {
    listen 443 ssl http2;
    server_name pts.yourdomain.com;

    ssl_certificate     /etc/nginx/certs/fullchain.pem;
    ssl_certificate_key /etc/nginx/certs/privkey.pem;
    ssl_protocols       TLSv1.2 TLSv1.3;
    ssl_ciphers         HIGH:!aNULL:!MD5;

    # Security headers
    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header X-Frame-Options "DENY" always;
    add_header Referrer-Policy "strict-origin-when-cross-origin" always;

    # Angular SPA — serve index.html for all non-API routes
    location / {
        root /usr/share/nginx/html;
        try_files $uri $uri/ /index.html;
        expires 1h;
    }

    # Static assets — long cache
    location ~* \.(js|css|png|jpg|woff2)$ {
        root /usr/share/nginx/html;
        expires 1y;
        add_header Cache-Control "public, immutable";
    }

    # API proxy
    location /api/ {
        proxy_pass         http://api:8080;
        proxy_set_header   Host $host;
        proxy_set_header   X-Real-IP $remote_addr;
        proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
    }

    # SignalR WebSocket proxy
    location /hubs/ {
        proxy_pass         http://api:8080;
        proxy_http_version 1.1;
        proxy_set_header   Upgrade $http_upgrade;
        proxy_set_header   Connection "upgrade";
        proxy_set_header   Host $host;
        proxy_read_timeout 86400s;
    }
}

server {
    listen 80;
    server_name pts.yourdomain.com;
    return 301 https://$host$request_uri;
}
```

---

## Dockerfiles

### Frontend (`frontend/Dockerfile`)

```dockerfile
# Build stage
FROM node:22-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build -- --configuration production

# Serve stage
FROM nginx:alpine
COPY --from=build /app/dist/points-tracker/browser /usr/share/nginx/html
EXPOSE 80
```

### Backend (`backend/Dockerfile`)

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/PointsTracker.Api/PointsTracker.Api.csproj", "src/PointsTracker.Api/"]
COPY ["src/PointsTracker.Application/PointsTracker.Application.csproj", "src/PointsTracker.Application/"]
COPY ["src/PointsTracker.Domain/PointsTracker.Domain.csproj", "src/PointsTracker.Domain/"]
COPY ["src/PointsTracker.Infrastructure/PointsTracker.Infrastructure.csproj", "src/PointsTracker.Infrastructure/"]
RUN dotnet restore "src/PointsTracker.Api/PointsTracker.Api.csproj"
COPY . .
RUN dotnet publish "src/PointsTracker.Api/PointsTracker.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PointsTracker.Api.dll"]
```

---

## Database Migrations on Deploy

Migrations run automatically on API startup:

```csharp
// Program.cs
using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
await db.Database.MigrateAsync();
```

This is safe because EF Core Migrations are idempotent.

---

## Backup Strategy

- PostgreSQL daily dump via a cron job on the TrueNAS host:
  ```bash
  docker exec pts-db-1 pg_dump -U pts_app points_tracker | gzip > /mnt/backups/pts-$(date +%Y%m%d).sql.gz
  ```
- Retain 30 daily backups.
- Store backups on a separate TrueNAS dataset with its own replication.

---

## Health Checks

| Endpoint           | Expected Response                 |
|--------------------|-----------------------------------|
| `GET /health`      | 200 `{"status":"Healthy"}`        |
| `GET /health/ready`| 200 when DB is reachable          |

Nginx can be configured to check `/health/ready` before routing traffic.

---

## Updating the Application

```bash
# Pull latest images / rebuild
docker compose -f docker/docker-compose.yml pull
docker compose -f docker/docker-compose.yml up -d --build

# Or for a zero-downtime update (if multiple replicas configured):
docker compose -f docker/docker-compose.yml up -d --build --no-deps api
```
