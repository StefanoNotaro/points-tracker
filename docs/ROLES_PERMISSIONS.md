# Roles and Permissions

## Overview

The system has **three orthogonal authorization scopes**. They are evaluated
independently — holding a role in one scope grants nothing in another.

1. **Global roles** — assigned to a user account, govern platform-level access.
   Stored in `users.role`. Sourced from the OIDC `pts_roles` claim, with
   server-side precedence rules (see [Authority model](#authority-model-option-b)).
2. **Tournament roles** — assigned per tournament (`organiser`, `scorer`,
   `viewer`), govern actions inside that tournament.
3. **Match-scoped scorer access** — granted via a revocable, match-bound invite
   token. Anonymous-capable. Does **not** grant a role and does **not** survive
   the match. See `docs/FEATURES.md` (tournament officiating) and the
   `TOUR-REF-*` action plan stream.

Anonymous users are a special case — they hold no role in any scope but can
perform a limited set of actions (counters via session token, match scoring
via a scorer invite link).

---

## Global Roles

| Role          | Description                                                           |
|---------------|-----------------------------------------------------------------------|
| `anonymous`   | No account. Can create/manage counters (session-token based). No tournaments. |
| `user`        | Authenticated. Full counter features. Can register for tournaments.   |
| `admin`       | Can create and manage tournaments. Can moderate content.             |
| `super_admin` | Full platform access. Can manage users, global settings, all data.   |

### Assignment

- Global roles are stored in `users.role` (enum: `user` | `admin` | `super_admin`).
- Default on first login: `user`.
- Promotion to `admin` or `super_admin` is done either:
  - automatically, by syncing the `pts_roles` claim from Authentik (`RoleSource = idp_claim`), or
  - manually, by a `super_admin` via the admin panel (`RoleSource = manual_override`).
- The runtime authority is always the database. The JWT `pts_roles` claim is
  one of two **inputs** to the effective role; see
  [Authority model](#authority-model-option-b) below.

---

## Authority model (Option B)

The platform uses a **token + DB persisted effective role** model. The OIDC
token carries identity and a suggested role; the database carries the
authoritative effective role plus the metadata required to audit it and to
override the IdP when needed.

### Persisted role metadata

The `users` table carries four role-related columns:

| Column            | Purpose                                                                 |
|-------------------|-------------------------------------------------------------------------|
| `role`            | Effective global role enum (`user` \| `admin` \| `super_admin`).        |
| `role_source`     | Why this role is set: `default` \| `idp_claim` \| `manual_override`.    |
| `role_updated_at` | When the role last changed.                                             |
| `role_updated_by` | Actor identifier — `idp:<external_id>`, `admin:<user_id>`, or `system`. |

### Precedence rules

When the OIDC sync runs (on every token validation), the effective role is
computed as:

1. **`manual_override`** wins. If the current `role_source` is
   `manual_override`, the IdP claim is **ignored** for role purposes. If the
   incoming claim disagrees with the persisted role, a *claim drift* audit
   event is recorded (informational, no demotion).
2. Otherwise, **`idp_claim`** wins. If `pts_roles` is present and maps to a
   known role, the DB is updated (if different) with `role_source = idp_claim`
   and an audit event is written.
3. Otherwise, the **persisted role is kept**. This includes the
   provider-outage / claim-missing case — *the system never silently demotes a
   user when the claim is absent*.

The `pts_role` claim attached to the principal at token validation is a
**per-request cache** of the effective role. All authorization checks read it,
but it is always derived from the DB during the same request, never from the
raw IdP token.

### Claim mapping

Authentik issues `pts_roles` as a JSON array of strings. Mapping into the
internal enum:

| Claim value          | Effective role |
|----------------------|----------------|
| `super_admin`        | `super_admin`  |
| `admin`              | `admin`        |
| any other / missing  | `user`         |

When multiple values are present, the **highest** maps. Unknown values are
ignored.

---

## Role-change audit trail

Every effective-role transition writes an immutable row to `role_audit_log`:

| Field         | Notes                                                           |
|---------------|-----------------------------------------------------------------|
| `id`          | UUID.                                                           |
| `user_id`     | The user whose role changed.                                    |
| `from_role`   | Previous effective role (nullable for the very first record).   |
| `to_role`     | New effective role.                                             |
| `source`      | `idp_claim` \| `manual_override` \| `default` \| `drift_detected`. |
| `actor`       | `idp:<external_id>`, `admin:<user_id>`, or `system`.            |
| `occurred_at` | UTC timestamp.                                                  |
| `reason`      | Optional free-text supplied by manual-override callers.         |

The audit log is append-only (no soft-delete, no edit endpoints). It powers
the admin dashboard governance view and is the canonical answer to "who
promoted user X and when".

---

## Permission Matrix — Global

| Action                               | anonymous | user | admin | super_admin |
|--------------------------------------|:---------:|:----:|:-----:|:-----------:|
| View public counters                 | ✅        | ✅   | ✅    | ✅          |
| Create a counter                     | ✅        | ✅   | ✅    | ✅          |
| Edit own counter                     | ✅ (token)| ✅   | ✅    | ✅          |
| Delete own counter                   | ✅ (token)| ✅   | ✅    | ✅          |
| Share counter                        | ✅ (token)| ✅   | ✅    | ✅          |
| Claim anonymous counter              | ❌        | ✅   | ✅    | ✅          |
| View tournaments (published)         | ✅        | ✅   | ✅    | ✅          |
| Register for a tournament            | ❌        | ✅   | ✅    | ✅          |
| Create tournament (own)              | ✅ (1)    | ✅   | ✅    | ✅          |
| Create multiple tournaments          | ❌        | ✅   | ✅    | ✅          |
| Manage any tournament                | ❌        | ❌   | ✅    | ✅          |
| Manage users                         | ❌        | ❌   | ❌    | ✅          |
| Change global roles                  | ❌        | ❌   | ❌    | ✅          |
| Access admin panel                   | ❌        | ❌   | ✅    | ✅          |
| View platform audit logs             | ❌        | ❌   | ❌    | ✅          |
| Hard delete any entity               | ❌        | ❌   | ❌    | ✅          |

(1) Anonymous users may have **at most one active tournament** at a time —
enforced server-side via the session-token hash. The tournament + its bracket
are accessible only while the browser keeps that token in `localStorage`.
Logged-in users have no cap.

---

## Tournament Roles (Phase 2)

Within a specific tournament, users can be assigned a tournament-scoped role
in addition to their global role.

| Tournament Role | Description                                             |
|-----------------|---------------------------------------------------------|
| `organiser`     | Full control over this tournament (the creator gets this automatically) |
| `scorer`        | Can enter match results; linked counter sessions       |
| `viewer`        | Explicitly added viewer; same as public unless tournament is private |

### Permission Matrix — Within a Tournament

| Action                          | viewer | scorer | organiser | admin (global) | super_admin |
|---------------------------------|:------:|:------:|:---------:|:--------------:|:-----------:|
| View bracket / standings        | ✅     | ✅     | ✅        | ✅             | ✅          |
| Enter match results             | ❌     | ✅     | ✅        | ✅             | ✅          |
| Edit draw / seeding             | ❌     | ❌     | ✅        | ✅             | ✅          |
| Publish tournament              | ❌     | ❌     | ✅        | ✅             | ✅          |
| Manage participant list         | ❌     | ❌     | ✅        | ✅             | ✅          |
| Assign tournament roles         | ❌     | ❌     | ✅        | ✅             | ✅          |
| Delete tournament               | ❌     | ❌     | ✅        | ✅             | ✅          |

---

## Enforcement

### Backend

Every command/query handler runs a permission check before acting:

```csharp
public class IncrementScoreCommandHandler
{
    public async Task Handle(IncrementScoreCommand cmd, CancellationToken ct)
    {
        var counter = await _repo.GetAsync(cmd.CounterId, ct);
        _permissionService.EnsureCanEdit(cmd.Actor, counter); // throws ForbiddenException
        // ...
    }
}
```

`EnsureCanEdit` checks in order:
1. Is the actor a `super_admin`? → allow.
2. Is the actor authenticated and the counter owner? → allow.
3. Does the actor hold a valid `edit`-scoped share token for this counter? → allow.
4. Does the actor hold a valid anonymous session token for this counter? → allow.
5. Otherwise → throw `ForbiddenException` → 403.

### Frontend

Guards prevent navigation to protected routes — **this is UX only**, not a security boundary:

```typescript
// auth.guard.ts
export const authGuard = () => {
  const auth = inject(AuthService);
  return auth.isAuthenticated() || redirect('/');
};

// role.guard.ts
export const roleGuard = (requiredRole: GlobalRole) => () => {
  const auth = inject(AuthService);
  return auth.hasRole(requiredRole) || redirect('/403');
};
```

Route definitions:
```typescript
{ path: 'admin', canActivate: [roleGuard('admin')], ... }
```

---

## Role Escalation Policy

- `super_admin` cannot be self-assigned — initial assignment is done via direct
  DB seed or TrueNAS admin (`role_source = manual_override`,
  `role_updated_by = system`).
- All role changes — both IdP-driven and manual — are recorded in
  `role_audit_log` (see [Role-change audit trail](#role-change-audit-trail)).
- Manual role changes are performed by a `super_admin` via `PATCH
  /api/admin/users/{id}/role` and always set `role_source = manual_override`,
  pinning the role against future IdP claim drift until another admin clears
  the override.
- A user with `role_source = manual_override` is never auto-demoted by an
  IdP claim. A mismatch is recorded as a `drift_detected` audit event for
  visibility but does not change the effective role.
- There must always be at least one active `super_admin` account. The domain
  refuses any operation (manual demotion, soft-delete, or IdP-driven
  downgrade) that would leave zero active super_admins. The IdP-sync path
  treats this as a drift event rather than enforcing the demotion.

## API authorization matrix

| Surface                              | Required scope                       | Source of truth                          |
|--------------------------------------|--------------------------------------|------------------------------------------|
| `GET /api/counters/{id}` (own)       | counter ownership OR share token     | `counters.owner_user_id` / share token   |
| `POST /api/counters/{id}/score`      | counter edit (owner / edit token / session token) | as above                          |
| Hub `/hubs/counter` join             | read access to the counter           | server-side authorization check          |
| `POST /api/tournaments`              | global `user` and above              | `users.role` (effective)                 |
| Tournament organiser actions         | tournament `organiser` membership    | tournament role table                    |
| Match score entry                    | tournament `scorer` OR match scorer link | tournament role / scorer invite token |
| `GET /api/admin/**`                  | global `admin` and above             | `users.role` (effective)                 |
| `PATCH /api/admin/users/{id}/role`   | global `super_admin`                 | `users.role` (effective)                 |
| `GET /api/admin/audit/roles`         | global `super_admin`                 | `users.role` (effective)                 |
