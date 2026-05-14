# Admin cleanup — retention policy & dashboard

Status: implemented in ADMIN-01 (2026-05-14).
Companion docs: [ARCHITECTURE.md](ARCHITECTURE.md), [ROLES_PERMISSIONS.md](ROLES_PERMISSIONS.md), [SECURITY.md](SECURITY.md).

This document is the canonical source of truth for *what gets cleaned up, when,
who can trigger it, and what is irreversible*. The cleanup background worker
and the admin dashboard both read these rules from `CounterCleanupOptions`.

---

## 1. What cleanup does

The system has three classes of debris:

| Class                          | What it is                                                                                 | Why it must be removed                                                  |
|--------------------------------|--------------------------------------------------------------------------------------------|-------------------------------------------------------------------------|
| **Stale anonymous counters**   | Counters with `OwnerUserId IS NULL` whose `UpdatedAt` is older than the inactivity window  | They cannot be recovered by their original creator (no account)         |
| **Stale anonymous tournaments**| Tournaments with `OwnerUserId IS NULL` whose `UpdatedAt` is older than the inactivity window | Same as above; their child counters get cascade-cleaned with them      |
| **Expired share tokens**       | `ShareToken` rows where `ExpiresAt < now` or `RevokedAt IS NOT NULL`                       | No security value after expiry; just rows                               |
| **Soft-deleted past grace**    | Any `Counter` or `Tournament` with `DeletedAt` older than the grace window                  | Hard-purge to free storage and remove personal data                     |

Authenticated users' data is never touched by automatic policy. Admins may
soft-delete specific authenticated rows by id (ad-hoc), but only with an
explicit reason and a confirmation step.

---

## 2. Retention windows (Aggressive — default for MVP)

These are bound from the `Cleanup` section of `appsettings.json` and changeable
without a code release.

| Setting                          | Default | Meaning                                                                |
|----------------------------------|---------|------------------------------------------------------------------------|
| `AnonymousInactiveDays`          | 14      | Anonymous counter/tournament idle > N days → soft-delete candidate     |
| `HardDeleteGraceDays`            | 30      | Soft-deleted row > N days → hard-purge candidate                       |
| `TournamentCompletedRetentionDays` | 90    | Anonymous tournament in `Completed`/`Abandoned` for > N days → soft-delete |
| `ExpiredShareTokenSweep`         | always  | Expired or revoked share tokens are deleted on every sweep             |
| `Interval`                       | 6h      | Background worker sweep cadence                                        |
| `Enabled`                        | true    | Master switch; off in tests/CI                                          |

Rationale: short windows surface bugs early (a regression that "leaks" rows
shows up within two weeks rather than months) and match the project's
"anonymous is privilege, not default" stance — anonymous users do not own
long-lived data.

---

## 3. Authority — who can run which action

Split model. Backend enforces; frontend mirrors for UX only.

| Action                                       | Required role  | Reversible?                                  |
|----------------------------------------------|----------------|----------------------------------------------|
| Preview cleanup candidates (dry-run)         | `admin`        | n/a — read-only                              |
| Soft-delete counter/tournament (ad-hoc)      | `admin`        | Yes — within grace window, by hard-purge skip |
| Sweep expired share tokens                   | `admin`        | No (but no security or recovery value)        |
| Run full policy sweep (soft + hard phases)   | `admin`        | Soft step reversible; hard step is not       |
| Hard-purge specific row (ad-hoc, by id)      | `super_admin`  | **No** — irreversible                        |

A `super_admin` always has `admin` capabilities by virtue of `GlobalRole`
ordering — see `GlobalRoleExtensions`.

---

## 4. Dashboard UX

Two execution modes coexist:

1. **List + multi-select + confirm.** The preview endpoint returns counts and
   id samples per candidate type. The admin picks specific rows, picks an
   action, and confirms in a modal that re-states the count and consequences.
2. **Run policy now.** A single button applies the configured retention
   windows to all eligible rows in one transaction. Confirmation modal still
   required. This is the same code path the background worker uses, so it is
   strictly a "no-wait" trigger of an action that would happen anyway within
   the next `Interval`.

Hard-purge always requires re-confirmation (a second click on a separate
button, not the same modal as soft-delete) to defeat muscle-memory.

---

## 5. Cascade behaviour

- Soft-deleting a `Counter` or `Tournament` sets `DeletedAt` on the parent
  only. Its children (`CounterSet`, `CounterEvent`, `ShareToken`,
  `TournamentParticipant`, `TournamentMatch`) carry no `DeletedAt` of their
  own — they become invisible because every read goes through the parent's
  EF Core query filter.
- Hard-purging a parent triggers EF Core relational `OnDelete(Cascade)` and
  physically removes children in the same transaction. See
  `CounterConfiguration` and `TournamentConfiguration`.
- A `Counter` whose `LinkedTournamentId` points at a soft-deleted tournament
  is itself soft-deleted on the next sweep — that branch is handled by the
  background service's phase-2 query and applies in manual policy runs too.

---

## 6. Audit log

Every executed cleanup action (not dry-run) writes one row to
`cleanup_audit_log` before the transaction commits:

- `action` — enum: `SoftDeleteCounters`, `SoftDeleteTournaments`,
  `HardPurgeCounters`, `HardPurgeTournaments`, `PurgeExpiredShareTokens`,
  `RunPolicy`.
- `actor` — string in the form `admin:<userId>` or `system:background` for
  the worker.
- `target_count` — number of rows the action touched.
- `target_ids` — JSON array of up to 50 ids (sampled if more); for batch
  policy runs this is `null` because the set can be very large.
- `reason` — free text, optional for policy runs, required for ad-hoc
  hard-purge.
- `occurred_at`.

The audit log is **append-only**. There is no endpoint to mutate or delete
its rows. The `LoggingBehavior` (BE-05) also emits a structured log line for
each command, so the application log retains a trail even if the audit table
is somehow truncated.

PII rule: do not copy team names, share-token plaintext, or user emails into
the audit log. Ids and counts only.

---

## 7. Failure modes & mitigations

- **Wide cutoff date footgun.** An admin entering a 1-day cutoff in the ad-hoc
  UI could nuke active counters. The dashboard caps the minimum cutoff at the
  server-side `AnonymousInactiveDays` value; the ad-hoc by-id flow is the
  escape hatch for genuine outliers.
- **Concurrency with active scoring.** Phase 1 uses `ExecuteUpdateAsync` and
  is set-based; phase 3 uses `ExecuteDeleteAsync`. A counter receiving a score
  event mid-purge will fail on the next `SaveChanges` because the row is gone
  — clients receive a `404` and the SignalR connection drops. Acceptable: the
  counter was already past its inactivity window, so there is no active user.
- **`super_admin` cannot purge themselves.** Enforced in the command
  validator. Mirrors the RBAC-04 last-super-admin guard.
- **Audit log corruption hides a bad actor.** Append-only at the application
  layer, and revoking write access to the table at the DB level is a future
  hardening (P3).

---

## 8. Open follow-ups (not blocking ADMIN-01)

- Restore (undelete) endpoint for soft-deleted rows within the grace window.
  Currently must be done by DB query.
- Scheduled-export of the audit log to cold storage.
- Rate-limit on cleanup endpoints — currently they ride the `write` policy.
- Cleanup of orphan `CounterEvent`/`CounterSet` rows whose parent counter was
  hard-deleted without cascade (cannot happen today, but a defensive sweep
  would catch a migration mistake).
