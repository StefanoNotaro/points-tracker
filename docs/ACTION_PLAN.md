# Action Plan - Standards & Quality Remediation

Date: 2026-05-14
Scope: `backend/` + `frontend/`
Source: Architecture/security review against `docs/ARCHITECTURE.md`, `docs/SECURITY.md`, `docs/UI_GUIDELINES.md`, and implementation scan.

---

## How to use this file

- Mark items as done by changing `[ ]` to `[x]`.
- Keep PR references in the `Notes` column.
- Execute work in priority order (`P0` -> `P1` -> `P2` -> `P3`).
- Any DB change must include an EF Core migration in `backend/src/PointsTracker.Infrastructure/Persistence/Migrations/` (schema and required data migration steps).

---

## Role model strategy (critical design decisions)

- Global platform roles from OIDC (`pts_roles`) are useful for platform administration.
- Tournament permissions are a separate scope and should remain tournament-scoped (`organiser`, `scorer`, `viewer`) in tournament role tables.
- Recommended policy:
  - OIDC claim is the runtime identity/role signal.
  - Backend DB persists effective role metadata for revocation, auditability, and operational safety (see `D1`).
  - Tournament match control uses scoped permissions, not broad platform admin rights.
- Referee workflow should use match-scoped scorer access (least privilege), not generic counter owner-level access.
- Confirmed product decisions:
  - Anonymous scoped referee links are allowed.
  - Referee permissions include score, timeout, undo/redo, and end-match.
  - Referee links are reusable until the match ends.

---

## P0 (Critical - fix first)

| ID | Task | Owner | Status | Notes |
|---|---|---|---|---|
| FE-01 | Fix route collision in `frontend/src/app/app.routes.ts` by placing `counter/join/:token` before `counter/:id` |  | [x] | Completed 2026-05-14: route order corrected |
| BE-01 | Enable FluentValidation pipeline in MediatR (`ValidationBehavior`, `AddValidatorsFromAssembly`) |  | [x] | Completed 2026-05-14: global `IPipelineBehavior` validation wired |
| BE-02 | Map `DbUpdateConcurrencyException` to `409 Conflict` in `backend/src/PointsTracker.Api/Middleware/ExceptionMiddleware.cs` |  | [x] | Completed 2026-05-14: middleware now returns 409 for concurrency conflicts |
| BE-03 | Re-enable strict JWT audience validation in `backend/src/PointsTracker.Api/Program.cs` |  | [x] | Completed 2026-05-14: audience validation enabled (`ValidateAudience = true`) |
| BE-04 | Apply rate limiting policies to endpoints and add missing policies (`share`, `join`, reads/writes) |  | [x] | Completed 2026-05-14: partitioned policies added and applied to counter/tournament endpoints |
| FE-03 | Align OIDC token storage with security policy (no JWT in `localStorage`) |  | [x] | Completed 2026-05-14: OAuth storage moved to `sessionStorage` (no JWT persistence in localStorage) |
| FE-04 | Fix role resolution path for OIDC role claims (`pts_roles`/`pts_role`) and token source mismatch |  | [x] | Completed 2026-05-14: deterministic role extraction from access-token + id-token claims |

---

## P1 (High priority)

| ID | Task | Owner | Status | Notes |
|---|---|---|---|---|
| FE-02 | Refactor `frontend/src/app/shared/components/share-dialog/share-dialog.component.ts` into dumb/presentational pattern |  | [x] | Completed 2026-05-18: `CounterService`/`NotificationService`/`TranslateService` removed; `generate` callback injected via `ShareDialogData`; inline error + copy-confirmation signals replace toasts |
| BE-05 | Add MediatR cross-cutting behaviors (`LoggingBehavior`, validation behavior) |  | [x] | Completed 2026-05-14: `LoggingBehavior` added (request name + elapsed ms, no payload), registered before `ValidationBehavior` |
| BE-06 | Migrate exception responses to framework `ProblemDetails` (`AddProblemDetails`) |  | [x] | Completed 2026-05-18: `AddProblemDetails()` registered; `ExceptionMiddleware` rewrites to `IProblemDetailsService` + typed `HttpValidationProblemDetails` for validation errors |
| FE-05 | Replace hardcoded English errors in `frontend/src/app/features/counter/store/counter.store.ts` with i18n keys |  | [x] | Completed 2026-05-18: all 11 hardcoded strings replaced with `counter.store.errors.*` / `counter.store.success.*` keys; keys added to en/es/ca locale files |
| FE-07 | Improve 401 handling in `frontend/src/app/core/interceptors/error.interceptor.ts` (avoid forced login in anonymous flows) |  | [x] | Completed 2026-05-14: re-auth only when previously authenticated; anonymous 401s re-thrown for caller; concurrent-redirect guard + `apiErrors.sessionExpired` toast |
| BE-10 | Enforce read authorization on SignalR hub joins (`/hubs/counter`) |  | [x] | Completed 2026-05-14: server-side join authorization added, with frontend denial handling |
| RBAC-01 | Define and document final RBAC contract: global roles vs tournament roles vs match-scoped scorer permissions |  | [x] | Completed 2026-05-14: three-scope model, Option B authority, audit trail, and API auth matrix added to `docs/ROLES_PERMISSIONS.md` |
| RBAC-02 | Implement OIDC role-claim sync policy (`pts_roles`) in backend user sync with safe precedence rules |  | [x] | Completed 2026-05-14: `UserSyncService.ReconcileRoleAsync` applies Option B precedence; drift events logged on manual_override mismatch and refused last-super_admin demotions |
| RBAC-03 | Implement Option B authority model: token claim + DB persisted effective global role (runtime checks read effective role from DB) |  | [x] | Completed 2026-05-14: `GlobalRole`/`RoleSource` enums, persisted `role_source`/`role_updated_at`/`role_updated_by` columns, migration `AddRoleSourceMetadata` |
| RBAC-04 | Add emergency admin controls: immediate role revoke/downgrade path and "last active super_admin" guard |  | [x] | Completed 2026-05-14: `PATCH /api/admin/users/{id}/role` (super_admin only) with `Confirm` flag; `LastSuperAdminException` enforced in IdP-sync and manual paths |
| RBAC-05 | Add role-change audit trail and source metadata (`idp_claim`, `manual_override`, actor, timestamp) |  | [x] | Completed 2026-05-14: `RoleAuditLog` entity + `IRoleAuditLogRepository` + migration `AddRoleAuditLog`; written from both IdP-sync and manual override paths, incl. `drift_detected` |
| ADMIN-01 | Add admin dashboard MVP for cleanups (stale anonymous counters/tournaments, expired tokens, orphaned records) |  | [x] | Completed 2026-05-14: `ICleanupService` + `CleanupAuditLog` + `/api/admin/cleanup/*` endpoints (preview, run-policy, ad-hoc soft-delete, super_admin hard-purge, expired-token sweep) and `/admin/cleanup` Angular route. Retention policy: docs/ADMIN_CLEANUP.md. Migration: `AddCleanupAuditLog`. |

### P1 execution order (most important -> least important)

1. `BE-10` - Enforce read authorization on SignalR hub joins (`/hubs/counter`).
2. `RBAC-01` - Define and document final RBAC contract.
3. `RBAC-03` - Implement Option B authority model (token + DB effective role).
4. `RBAC-02` - Implement OIDC role-claim sync policy (`pts_roles`).
5. `RBAC-04` - Add emergency admin controls (revoke/downgrade + last `super_admin` guard).
6. `RBAC-05` - Add role-change audit trail metadata.
7. `FE-07` - Improve 401 handling in `error.interceptor.ts`.
8. `BE-05` - Add MediatR cross-cutting behaviors (logging + pipeline consistency).
9. `ADMIN-01` - Add admin dashboard MVP for cleanups.
10. `BE-06` - Migrate exception responses to framework `ProblemDetails`.
11. `FE-02` - Refactor `share-dialog` to dumb/presentational pattern.
12. `FE-05` - Replace hardcoded English errors in `counter.store.ts` with i18n keys.

---

## P2 (Important structural improvements)

| ID | Task | Owner | Status | Notes |
|---|---|---|---|---|
| BE-07 | Ensure `IUserRepository` is implemented/used consistently (avoid direct persistence access in auth sync path) |  | [x] | Completed 2026-05-18: extracted `IUserSyncService` to Application layer; `UserSyncService` implements it; DI and `Program.cs` resolve via interface |
| BE-09 | Evaluate migration from UUID v4 to UUID v7 for index-friendlier inserts |  | [ ] | Plan as non-breaking migration strategy |
| FE-06 | Align feature folder structure to architecture (`features/*/components/`) for dashboard/settings |  | [ ] | Conformance with `docs/ARCHITECTURE.md` |
| FE-09 | Add `pts-not-found` component and dedicated 404 route |  | [ ] | Current wildcard redirects to root |
| FE-08 | Remove/justify `[innerHTML]` use in `create-counter` template per security guidance |  | [~] | Deferred — risk accepted for now; single `<b>` tag in a static translation string, no untrusted input |
| TOUR-REF-01 | Add tournament match officiating model: assign scorer/referee users (or invite tokens) per match |  | [ ] | Separate from global admin role |
| TOUR-REF-02 | Add organizer-generated match scorer links (anonymous allowed, reusable, revocable) |  | [ ] | Link lifetime: valid until match end; no ownership grant |
| TOUR-REF-03 | Enforce scorer authorization in backend commands/hubs (`increment`, `decrement`, `timeout`, `undo`, `redo`, `end-match`) |  | [ ] | Block non-scoped operations (e.g., delete counter, rename tournament) |
| TOUR-REF-04 | Add UI flows: assign referee/scorer, generate/revoke link, show active officiator in match screen |  | [ ] | Include status indicators and recovery path if scorer disconnects |

### P2 execution order (most important -> least important)

1. ~~`FE-08`~~ — Deferred (risk accepted; static translation string, no untrusted input).
2. `BE-07` — ✓ Done. Extracted `IUserSyncService` to Application layer; `UserSyncService` implements it; DI and `Program.cs` resolve via interface.
3. `TOUR-REF-01` — Add tournament match officiating domain model (**prerequisite** — all TOUR-REF items depend on this).
4. `TOUR-REF-03` — Enforce scorer authorization in backend commands/hubs (**security gate** — must land before scorer links are usable).
5. `TOUR-REF-02` — Add organizer-generated match scorer links (**feature** — depends on TOUR-REF-01 and TOUR-REF-03).
6. `TOUR-REF-04` — Add UI flows: assign referee, generate/revoke link, active officiator indicator (**feature UI** — depends on 01/02/03).
7. `FE-09` — Add `pts-not-found` component and dedicated 404 route (**UX** — current wildcard silently redirects to root).
8. `FE-06` — Align feature folder structure to architecture (`features/*/components/`) (**maintainability** — no runtime impact).
9. `BE-09` — Evaluate UUID v4 → UUID v7 migration (**optimization** — no urgency at current scale; plan as non-breaking migration).

---

## P3 (Polish and maintainability)

| ID | Task | Owner | Status | Notes |
|---|---|---|---|---|
| BE-08 | Harden `SportRules` construction in `CreateCounterCommand` path (explicit named construction) |  | [ ] | Reduce accidental default-field regressions |
| FE-10 | Reduce unnecessary `NgClass` imports in simple class toggle cases |  | [ ] | Minor cleanup and bundle hygiene |
| BE-11 | Narrow `IShareTokenService` application surface (reduce crypto detail leakage to app layer) |  | [ ] | Improve separation of concerns |
| TOUR-REF-05 | Add audit trail for officiating actions (who scored, via user vs scoped link, when) |  | [ ] | Required for dispute resolution and admin cleanup confidence |

---

## New feature stream: admin dashboard + referee controls

### Stream A - Admin dashboard (platform-level)

- [ ] `A1` Define cleanup operations and retention policies (what is safe to auto-clean, what needs manual confirmation).
- [ ] `A2` Add backend admin endpoints under `/api/admin/*` with strict global role checks.
- [ ] `A3` Add frontend `admin` route + role guard and dashboards for cleanup actions.
- [ ] `A4` Add immutable audit events for each cleanup action.

### Stream B - Tournament officiating (tournament-level)

- [ ] `B1` Extend domain model for match officials / scorer assignment.
- [ ] `B2` Add match-scoped invite token model (anonymous-capable, reusable-until-match-end, revocable).
- [ ] `B3` Add endpoint to issue/revoke scorer links from organizer UI.
- [ ] `B4` Add scorer-access validation in counter/tournament command handlers and SignalR join rules.
- [ ] `B5` Add organizer UI to assign/reassign referee and monitor control ownership.

---

## Decision checkpoints (needs product/security confirmation)

- [x] `D1` Role authority model selected: `Option B` (recommended) - token + DB persisted effective role (audit + emergency override + provider outage tolerance).
- [x] `D2` Referee authentication requirement: anonymous scoped invite links are allowed.
- [x] `D3` Scorer link reuse model: reusable link until match end.
- [x] `D4` Scorer permissions: score + timeout + undo + redo + end-match.
- [x] `D5` Referee token lifetime policy: link is valid until match end, plus explicit revoke support.

---

## Database migration policy (mandatory)

- [ ] Every DB-related change (new column/table/index/constraint or required data backfill) must ship with an EF Core migration in the backend project.
- [ ] Migration files must be generated under `backend/src/PointsTracker.Infrastructure/Persistence/Migrations/` and reviewed in PR.
- [ ] No manual production-only schema edits; schema state must remain reproducible from migrations.
- [ ] If a feature requires data transformation, include migration-safe backfill/compatibility steps and rollback notes.

---

## Verification checklist (run after each completed item)

- [ ] Backend unit/integration tests pass (`backend/tests/`)
- [ ] Frontend tests pass (`frontend/src/app/**/*.spec.ts`)
- [ ] API auth paths verified (anonymous, authenticated, share token)
- [ ] RBAC paths verified (global admin, organiser, scorer/referee, viewer)
- [ ] SignalR join/update flows verified for authorization and reconnect behavior
- [ ] Lint/format checks pass
- [ ] Docs updated when behavior changes

---

## Suggested execution sequence

1. Fix routing + auth security blockers (`FE-01`, `BE-03`, `BE-04`).
2. Enable mandatory backend validation/error semantics (`BE-01`, `BE-02`).
3. Stabilize identity/role/token behavior (`FE-03`, `FE-04`, `FE-07`).
4. Lock RBAC contract and role sync policy (`RBAC-01`, `RBAC-02`).
5. Enforce architecture rules for shared components and hubs (`FE-02`, `BE-10`).
6. Implement admin cleanup dashboard stream (`ADMIN-01`, Stream A).
7. Implement tournament officiating stream (`TOUR-REF-*`, Stream B).
8. Complete i18n/structure/404/documentation cleanup (`P2`, `P3`).

