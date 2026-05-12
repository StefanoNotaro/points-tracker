# Data Model

All IDs are `UUID v4`. All entities use soft-delete (`deleted_at` nullable timestamp).
Timestamps are `TIMESTAMP WITH TIME ZONE` stored in UTC.

---

## Entity Relationship Overview

```
User ─────────────────┐
  │                   │
  │ owns              │ organises
  ▼                   ▼
Counter            Tournament
  │                   │
  │ has               │ has
  ▼                   ▼
CounterSession     TournamentMatch ─── Counter (linked)
  │                   │
  │ generates         │ has
  ▼                   ▼
ShareToken         TournamentTeam
```

---

## Tables

### `users`

Mirrors essential fields from Authentik — populated on first OIDC login.

| Column         | Type                     | Notes                              |
|----------------|--------------------------|------------------------------------|
| `id`           | UUID PK                  |                                    |
| `external_id`  | VARCHAR(255) UNIQUE       | Authentik `sub` claim              |
| `email`        | VARCHAR(255) UNIQUE       |                                    |
| `display_name` | VARCHAR(100)              |                                    |
| `role`         | VARCHAR(50)               | `user`, `admin`, `super_admin`     |
| `created_at`   | TIMESTAMPTZ               |                                    |
| `updated_at`   | TIMESTAMPTZ               |                                    |
| `deleted_at`   | TIMESTAMPTZ NULLABLE      | Soft delete                        |

---

### `counters`

| Column           | Type                | Notes                                                |
|------------------|---------------------|------------------------------------------------------|
| `id`             | UUID PK             |                                                      |
| `sport_type`     | VARCHAR(50)         | Enum: `volleyball`, `beach_volleyball`               |
| `owner_user_id`  | UUID FK NULLABLE    | `users.id` — null for anonymous counters             |
| `session_token_hash` | VARCHAR(64) NULLABLE | SHA-256 of anon session token; null if owned     |
| `team_a_name`    | VARCHAR(100)        |                                                      |
| `team_b_name`    | VARCHAR(100)        |                                                      |
| `status`         | VARCHAR(50)         | `active`, `finished`, `abandoned`                    |
| `xmin`           | xid                 | PostgreSQL system column — used for optimistic concurrency |
| `created_at`     | TIMESTAMPTZ         |                                                      |
| `updated_at`     | TIMESTAMPTZ         |                                                      |
| `deleted_at`     | TIMESTAMPTZ NULLABLE|                                                      |

---

### `counter_sets`

One row per completed or current set within a counter.

| Column         | Type          | Notes                                  |
|----------------|---------------|----------------------------------------|
| `id`           | UUID PK       |                                        |
| `counter_id`   | UUID FK       | `counters.id`                          |
| `set_number`   | SMALLINT      | 1-based                                |
| `score_a`      | SMALLINT      | Team A final score for this set        |
| `score_b`      | SMALLINT      | Team B final score for this set        |
| `winner`       | CHAR(1)       | `A`, `B`, or NULL (in progress)        |
| `started_at`   | TIMESTAMPTZ   |                                        |
| `ended_at`     | TIMESTAMPTZ NULLABLE |                                 |

---

### `counter_events`

Append-only audit log of all score changes. Enables undo and replay.

| Column         | Type          | Notes                                       |
|----------------|---------------|---------------------------------------------|
| `id`           | UUID PK       |                                             |
| `counter_id`   | UUID FK       | `counters.id`                               |
| `set_number`   | SMALLINT      |                                             |
| `event_type`   | VARCHAR(50)   | `score_increment`, `score_decrement`, `undo`, `set_reset`, `set_complete` |
| `team`         | CHAR(1)       | `A` or `B`                                  |
| `score_a_after`| SMALLINT      | Score A after this event                    |
| `score_b_after`| SMALLINT      | Score B after this event                    |
| `actor_user_id`| UUID NULLABLE | Authenticated actor; null = anonymous       |
| `created_at`   | TIMESTAMPTZ   |                                             |

---

### `share_tokens`

| Column         | Type                | Notes                                         |
|----------------|---------------------|-----------------------------------------------|
| `id`           | UUID PK             |                                               |
| `counter_id`   | UUID FK             | `counters.id`                                 |
| `token`        | VARCHAR(512) UNIQUE | Full signed token string                      |
| `scope`        | VARCHAR(20)         | `read`, `edit`                                |
| `created_by_user_id` | UUID NULLABLE | Null for anonymous-created tokens           |
| `expires_at`   | TIMESTAMPTZ         |                                               |
| `revoked_at`   | TIMESTAMPTZ NULLABLE|                                               |
| `created_at`   | TIMESTAMPTZ         |                                               |

---

### `tournaments` (Phase 2)

| Column         | Type          | Notes                                              |
|----------------|---------------|----------------------------------------------------|
| `id`           | UUID PK       |                                                    |
| `name`         | VARCHAR(200)  |                                                    |
| `sport_type`   | VARCHAR(50)   |                                                    |
| `format`       | VARCHAR(50)   | `single_elimination`, `double_elimination`, `round_robin` |
| `status`       | VARCHAR(50)   | `draft`, `registration`, `active`, `completed`     |
| `owner_user_id`| UUID FK       | `users.id`                                         |
| `starts_at`    | TIMESTAMPTZ NULLABLE |                                               |
| `ends_at`      | TIMESTAMPTZ NULLABLE |                                               |
| `created_at`   | TIMESTAMPTZ   |                                                    |
| `updated_at`   | TIMESTAMPTZ   |                                                    |
| `deleted_at`   | TIMESTAMPTZ NULLABLE |                                               |

---

### `tournament_participants` (Phase 2)

| Column            | Type        | Notes                              |
|-------------------|-------------|------------------------------------|
| `id`              | UUID PK     |                                    |
| `tournament_id`   | UUID FK     | `tournaments.id`                   |
| `user_id`         | UUID FK NULLABLE | `users.id`                    |
| `team_name`       | VARCHAR(100)|                                    |
| `seed`            | SMALLINT NULLABLE |                               |
| `registered_at`   | TIMESTAMPTZ |                                    |

---

### `tournament_roles` (Phase 2)

Junction table assigning per-tournament roles to users.

| Column          | Type        | Notes                                         |
|-----------------|-------------|-----------------------------------------------|
| `tournament_id` | UUID FK     |                                               |
| `user_id`       | UUID FK     |                                               |
| `role`          | VARCHAR(50) | `organiser`, `scorer`, `viewer`               |
| PRIMARY KEY: (`tournament_id`, `user_id`) |  |                              |

---

## Indexes

```sql
-- Performance
CREATE INDEX idx_counters_owner ON counters(owner_user_id) WHERE deleted_at IS NULL;
CREATE INDEX idx_counter_sets_counter ON counter_sets(counter_id);
CREATE INDEX idx_counter_events_counter ON counter_events(counter_id, created_at DESC);
CREATE INDEX idx_share_tokens_counter ON share_tokens(counter_id) WHERE revoked_at IS NULL;
CREATE INDEX idx_share_tokens_token ON share_tokens(token);

-- Tournament indexes added in Phase 2 migrations
```

---

## Migration Strategy

- All schema changes via EF Core Migrations.
- Migrations are idempotent and reviewed in PR before merge.
- No breaking schema changes to production tables — use additive migrations + code compatibility window.
- Never rename or drop columns directly — add new column, migrate data, drop old in a subsequent release.
