# Roles and Permissions

## Overview

The system has two role layers:

1. **Global roles** — assigned to a user account, govern platform-level access.
2. **Tournament roles** — assigned per tournament, govern what a user can do within that tournament.

Anonymous users are a special case — they get no role but can perform a limited set of actions.

---

## Global Roles

| Role          | Description                                                           |
|---------------|-----------------------------------------------------------------------|
| `anonymous`   | No account. Can create/manage counters (session-token based). No tournaments. |
| `user`        | Authenticated. Full counter features. Can register for tournaments.   |
| `admin`       | Can create and manage tournaments. Can moderate content.             |
| `super_admin` | Full platform access. Can manage users, global settings, all data.   |

### Assignment

- Global roles are stored in `users.role`.
- Default on first login: `user`.
- Promotion to `admin` or `super_admin` is done by a `super_admin` via the admin panel.
- Roles are included in the Authentik user profile and reflected in the JWT `role` claim — but the backend **always re-validates from the database**, not the JWT claim. The JWT claim is informational only.

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
| Create tournament                    | ❌        | ❌   | ✅    | ✅          |
| Manage any tournament                | ❌        | ❌   | ✅    | ✅          |
| Manage users                         | ❌        | ❌   | ❌    | ✅          |
| Change global roles                  | ❌        | ❌   | ❌    | ✅          |
| Access admin panel                   | ❌        | ❌   | ✅    | ✅          |
| View platform audit logs             | ❌        | ❌   | ❌    | ✅          |
| Hard delete any entity               | ❌        | ❌   | ❌    | ✅          |

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

- `super_admin` cannot be self-assigned — initial assignment is done via direct DB seed or TrueNAS admin.
- Role changes are logged in the audit log.
- Downgrading a `super_admin` requires another `super_admin` to perform the action.
- There must always be at least one active `super_admin` account — the system blocks the last one from being downgraded.
