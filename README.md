# Points Tracker

A real-time sport point counter and tournament management platform.

**Stack:** Angular 21 · .NET 9 · PostgreSQL · SignalR · Authentik (OIDC) · Docker

---

## Documentation

| Document | Purpose |
|---|---|
| [CLAUDE.md](CLAUDE.md) | AI assistant instructions — start here |
| [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) | System design, bounded contexts, real-time model |
| [docs/FEATURES.md](docs/FEATURES.md) | Feature specifications per phase |
| [docs/UI_GUIDELINES.md](docs/UI_GUIDELINES.md) | Angular component rules, theming, accessibility |
| [docs/SECURITY.md](docs/SECURITY.md) | Threat model, auth, token design, headers |
| [docs/DATA_MODEL.md](docs/DATA_MODEL.md) | PostgreSQL schema, entities, indexes |
| [docs/ROLES_PERMISSIONS.md](docs/ROLES_PERMISSIONS.md) | Global and tournament role system |
| [docs/DEPLOYMENT.md](docs/DEPLOYMENT.md) | Docker Compose, Nginx, TrueNAS setup |

## Quick Start (local dev)

> Prerequisites: Docker Desktop, Node 22, .NET 9 SDK

```bash
# 1. Copy env template
cp .env.example .env
# edit .env with your local values

# 2. Start DB
docker compose -f docker/docker-compose.yml up db -d

# 3. Start backend (runs EF migrations automatically)
cd backend && dotnet run --project src/PointsTracker.Api

# 4. Start frontend
cd frontend && npm ci && npm start
```

Frontend → http://localhost:4200  
API → http://localhost:8080  
API docs → http://localhost:8080/scalar
