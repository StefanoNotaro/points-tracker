# CLAUDE.md — Points Tracker Project

This file is the single source of truth Claude reads to understand how to work in this repository.
Before writing any code, read this file fully and follow every guideline here.

---

## Project in One Line

A real-time sport point counter and tournament manager, starting with Volleyball and Beach Volleyball,
designed for anonymous + authenticated use, shareable sessions, and future multi-sport tournament brackets.

---

## Tech Stack

| Layer      | Technology                          | Notes                              |
|------------|-------------------------------------|------------------------------------|
| Frontend   | Angular 21.2.10                     | Standalone components only         |
| Styling    | Tailwind CSS 4.3                    | CSS-first config; `@theme` tokens  |
| Backend    | .NET 9 (latest LTS)                 | Minimal API + Clean Architecture   |
| Database   | PostgreSQL (latest stable)          | Via EF Core                        |
| Auth       | Authentik (external, self-hosted)   | OIDC / OAuth2 — already deployed   |
| Deployment | Docker Compose on TrueNAS Scale     | See `docs/DEPLOYMENT.md`           |

---

## Non-Negotiable Principles

### 1. SOLID — Always
- **S** — Every class/component/service has one reason to change.
- **O** — Extend via new abstractions, never patch existing logic.
- **L** — Subtypes must be substitutable for base types.
- **I** — No interface should force implementors to carry methods they don't need.
- **D** — Depend on abstractions (interfaces/tokens), never concrete implementations.

### 2. Security First
- Anonymous access is allowed on the frontend — treat it as a **privilege, not a default**.
- Every API endpoint is **deny-by-default**; explicitly grant access.
- Never trust client-supplied IDs for ownership checks — always validate server-side.
- Share tokens are cryptographically random, time-aware, and scoped (read vs. edit).
- No sensitive data (tokens, session IDs) in URLs — use headers or `sessionStorage` with care.
- Input validation at every system boundary: Angular reactive forms + .NET FluentValidation.
- Full details: `docs/SECURITY.md`.

### 3. No Magic Numbers / Strings
- Enums for sports, roles, permission levels, match states.
- Constants file for shared literals.

### 4. No Over-Engineering
- Build what the feature needs today, designed to extend tomorrow.
- Do not pre-build tournament features while working on counters, and vice versa.

---

## Repository Layout (target — do not deviate)

```
points-tracker/
├── frontend/                  # Angular app
│   ├── src/
│   │   ├── app/
│   │   │   ├── core/          # Singleton services, guards, interceptors, auth
│   │   │   ├── shared/        # Reusable dumb components, pipes, directives
│   │   │   ├── features/      # Feature modules (counter, tournament, settings)
│   │   │   └── layout/        # Shell, nav, sidebar
│   │   ├── assets/
│   │   └── styles/            # Global SCSS theme
├── backend/
│   ├── src/
│   │   ├── PointsTracker.Api/          # Minimal API, controllers, middleware
│   │   ├── PointsTracker.Application/  # Use-cases, CQRS handlers, DTOs
│   │   ├── PointsTracker.Domain/       # Entities, value objects, domain events
│   │   └── PointsTracker.Infrastructure/ # EF Core, repos, external services
│   └── tests/
│       ├── PointsTracker.UnitTests/
│       └── PointsTracker.IntegrationTests/
├── docker/
│   ├── docker-compose.yml
│   ├── docker-compose.override.yml    # local dev overrides
│   └── nginx/                         # Frontend reverse proxy config
├── docs/
│   ├── ARCHITECTURE.md
│   ├── FEATURES.md
│   ├── UI_GUIDELINES.md
│   ├── SECURITY.md
│   ├── DATA_MODEL.md
│   ├── ROLES_PERMISSIONS.md
│   └── DEPLOYMENT.md
└── CLAUDE.md  ← you are here
```

---

## Frontend Rules

- **Standalone components everywhere** — no NgModules unless forced by a third-party library.
- Component responsibility: **presentation only** — no HTTP calls inside components.
- All HTTP calls go through feature services in `core/` or `features/<name>/services/`.
- Use **Angular Signals** for local reactive state; RxJS only for async streams (HTTP, WebSocket).
- Use **Tailwind CSS 4.3** utility classes in all templates — never hardcode colours, spacings, or raw CSS in component files.
- All design tokens (colours, spacing, radii) are defined in `styles/theme.css` using Tailwind's `@theme` directive — the single source of truth.
- Custom CSS classes (shared patterns) go in `@layer components` in `theme.css` only, prefixed with `pts-`. Use `@apply` sparingly.
- Every shared component lives in `shared/components/` and is documented in its own README or Storybook story.
- Lazy-load every feature route.
- Full guidelines: `docs/UI_GUIDELINES.md`.

## Backend Rules

- Follow **Clean Architecture** layers strictly — the Domain layer has zero external dependencies.
- Use **CQRS with MediatR** for all use-cases.
- Use **FluentValidation** for all command/query validation.
- Use **EF Core** with explicit configurations (`IEntityTypeConfiguration<T>`) — no data annotations on domain entities.
- Every endpoint returns a typed Result/Problem Details response — never raw exceptions.
- **Optimistic concurrency** on all counters and tournament entities via row version.

## Database Rules

- Migrations are code-first via EF Core Migrations.
- Never run raw SQL outside of migration files or read-model projections.
- All IDs are UUIDs (Guid).
- Soft-delete pattern for all user-facing entities (no hard deletes).
- Schema: `docs/DATA_MODEL.md`.

---

## Key Behaviours to Implement

| Behaviour                        | Mechanism                                                     |
|----------------------------------|---------------------------------------------------------------|
| Persist data on page refresh     | Counter state stored in backend; frontend reconnects by ID   |
| Anonymous counter creation       | Server creates a session token stored in `localStorage`       |
| Share counter (read)             | Short-lived signed URL with read-only scope token            |
| Share counter (edit)             | Separate token with edit scope; server enforces permissions  |
| Real-time score sync             | SignalR hub per counter session                               |
| Tournament management            | REST; role-gated — see `docs/ROLES_PERMISSIONS.md`           |
| Authentication                   | Authentik OIDC — see `docs/SECURITY.md`                      |

---

## What to Read Before Each Task

| Task                        | Docs to read first                         |
|-----------------------------|--------------------------------------------|
| UI component work           | `docs/UI_GUIDELINES.md`                   |
| New feature / endpoint      | `docs/ARCHITECTURE.md`, `docs/FEATURES.md`|
| Auth / permissions work     | `docs/SECURITY.md`, `docs/ROLES_PERMISSIONS.md` |
| Database schema changes     | `docs/DATA_MODEL.md`                       |
| Docker / infra work         | `docs/DEPLOYMENT.md`                       |
| Anything involving roles    | `docs/ROLES_PERMISSIONS.md`               |
