# Architecture

## Overview

Points Tracker is a **multi-tenant, real-time sport management platform**. The system is designed around
two primary bounded contexts:

1. **Counter** — Lightweight, ephemeral-or-persisted score sessions for one or more sports.
2. **Tournament** — Structured competition management with brackets, draws, and standings.

Both contexts share an authentication/authorisation layer backed by **Authentik** (external OIDC provider)
and a **PostgreSQL** database. Real-time updates are delivered via **SignalR**.

---

## High-Level Diagram

```
Browser (Angular 21)
        │
        │  HTTPS
        ▼
   [Nginx / Reverse Proxy]
        │
   ┌────┴──────────────┐
   │                   │
   ▼                   ▼
Angular SPA       .NET 9 API
(static files)    (Minimal API + SignalR)
                       │
               ┌───────┴───────┐
               │               │
        PostgreSQL         Authentik
        (via EF Core)      (OIDC IdP)
```

All services run as Docker containers on TrueNAS Scale.
Nginx handles TLS termination and routes `/api/*` and `/hubs/*` to the .NET container.

---

## Backend — Clean Architecture

```
PointsTracker.Domain          (no external dependencies)
    └── Entities, Value Objects, Domain Events, Interfaces

PointsTracker.Application     (depends on Domain only)
    └── CQRS Commands/Queries (MediatR)
    └── DTOs, Validators (FluentValidation)
    └── Interfaces for infrastructure services

PointsTracker.Infrastructure  (depends on Application + Domain)
    └── EF Core DbContext + Configurations
    └── Repository implementations
    └── SignalR hubs
    └── Authentik OIDC integration
    └── Background services

PointsTracker.Api             (depends on Application + Infrastructure)
    └── Minimal API endpoint definitions
    └── Middleware (error handling, auth, rate limiting)
    └── Dependency injection wiring
```

**Rule:** Arrows flow inward — outer layers depend on inner layers, never the reverse.

---

## Frontend — Feature-Sliced Structure

```
src/app/
├── core/
│   ├── auth/             # OIDC client, token management, Authentik adapter
│   ├── guards/           # Auth + role guards
│   ├── interceptors/     # JWT attach, error normalisation
│   └── services/         # App-level singletons (session, theme, notifications)
├── shared/
│   ├── components/       # Dumb/presentational components
│   ├── directives/
│   ├── pipes/
│   └── models/           # Shared TypeScript interfaces & enums
├── features/
│   ├── counter/          # Counter feature slice
│   │   ├── components/
│   │   ├── services/
│   │   ├── store/        # Signal-based state
│   │   └── counter.routes.ts
│   ├── tournament/       # Tournament feature slice (future)
│   └── settings/
└── layout/               # Shell, nav-bar, footer
```

---

## Real-Time Architecture

- Each active counter session opens a **SignalR connection** to `/hubs/counter/{counterId}`.
- Score updates are sent as commands (client → server → broadcast to all connected clients).
- **Optimistic updates**: the UI applies the change locally immediately; the server confirms or rolls back.
- If a client reconnects (page refresh, network drop) it fetches the current state via REST first, then re-joins the SignalR group.

---

## Anonymous vs. Authenticated Flow

### Anonymous Counter
1. User opens app without logging in.
2. Frontend calls `POST /api/counters` without a Bearer token.
3. API creates the counter and returns a `session_token` (cryptographically random, UUID-based).
4. Frontend stores `session_token` in `localStorage` keyed by `counter_id`.
5. Subsequent requests include this token as `X-Session-Token` header.
6. Server validates ownership via token — not user identity.

### Authenticated Counter
1. User logs in via Authentik OIDC flow.
2. Frontend obtains and stores JWT (access token).
3. All requests carry `Authorization: Bearer <jwt>`.
4. Counter is associated with the user's account — accessible from any device after login.
5. Anonymous counters can be **claimed** by an authenticated user post-login.

---

## Sharing Model

| Share Type  | Token Scope | Server Enforcement                         |
|-------------|-------------|---------------------------------------------|
| View only   | `read`      | All write endpoints reject the share token  |
| Edit        | `edit`      | Full counter control except delete          |

Share tokens are:
- UUID v4 + HMAC-signed with a server secret.
- Stored in the database with expiry and scope.
- Revocable server-side at any time by the owner.
- Never embedded in redirect URLs — delivered as a path segment (`/counter/join/{token}`).

---

## Bounded Contexts & Future Expansion

| Context          | Status     | Notes                                      |
|------------------|------------|--------------------------------------------|
| Counter          | Phase 1    | Volleyball, Beach Volleyball               |
| Tournament       | Phase 2    | Brackets, draws, standings, role system    |
| Sport Registry   | Phase 2    | Rules engine per sport type                |
| Notifications    | Phase 3    | Push / email for tournament events         |

Each bounded context maps to its own set of API endpoints, domain entities, and frontend feature slice.
They share the auth layer and the database but are otherwise decoupled.
