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

### `tournaments`

Match-rule columns mirror `counters` so the same `SportRules` engine and
`Counter.Create` constructor can be reused when a counter is spawned for a
tournament match.

| Column                              | Type                | Notes                                            |
|-------------------------------------|---------------------|--------------------------------------------------|
| `id`                                | UUID PK             |                                                  |
| `name`                              | VARCHAR(200)        |                                                  |
| `sport_type`                        | VARCHAR(50)         |                                                  |
| `format`                            | VARCHAR(50)         | `singleelimination`, `doubleelimination`, `roundrobin` |
| `status`                            | VARCHAR(50)         | `draft`, `registration`, `active`, `completed`, `abandoned` |
| `owner_user_id`                     | UUID FK NULLABLE    | null = anonymous tournament                      |
| `session_token_hash`                | VARCHAR(64) NULLABLE| SHA-256 of anon session token                    |
| `custom_points_per_set`             | INTEGER NULLABLE    | rule override                                    |
| `custom_last_set_points`            | INTEGER NULLABLE    |                                                  |
| `custom_sets_to_win`                | INTEGER NULLABLE    |                                                  |
| `custom_total_sets`                 | INTEGER NULLABLE    |                                                  |
| `custom_win_by_two`                 | BOOLEAN NULLABLE    |                                                  |
| `indoor_switch_every_sets`          | INTEGER NULLABLE    |                                                  |
| `beach_auto_switch_sides`           | BOOLEAN             | default true                                     |
| `custom_timeouts_per_set`           | INTEGER NULLABLE    |                                                  |
| `custom_timeout_duration_seconds`   | INTEGER NULLABLE    |                                                  |
| `starts_at`                         | TIMESTAMPTZ NULLABLE|                                                  |
| `ends_at`                           | TIMESTAMPTZ NULLABLE|                                                  |
| `created_at`                        | TIMESTAMPTZ         |                                                  |
| `updated_at`                        | TIMESTAMPTZ         |                                                  |
| `deleted_at`                        | TIMESTAMPTZ NULLABLE| soft delete                                      |

Constraint enforced in code (not DB): at most one row with
`session_token_hash IS NOT NULL` and `status IN ('draft','registration','active')`
per hash. Validated in `CreateTournamentHandler`.

---

### `tournament_participants`

| Column            | Type              | Notes                              |
|-------------------|-------------------|------------------------------------|
| `id`              | UUID PK           |                                    |
| `tournament_id`   | UUID FK           | `tournaments.id`                   |
| `user_id`         | UUID FK NULLABLE  | `users.id`                         |
| `team_name`       | VARCHAR(100)      | unique per tournament              |
| `seed`            | INTEGER NULLABLE  |                                    |
| `registered_at`   | TIMESTAMPTZ       |                                    |

---

### `tournament_matches`

One row per slot in the bracket — generated up-front by `IBracketGenerator`
when the tournament is started.

| Column                  | Type             | Notes                                                  |
|-------------------------|------------------|--------------------------------------------------------|
| `id`                    | UUID PK          |                                                        |
| `tournament_id`         | UUID FK          | `tournaments.id`                                       |
| `bracket_side`          | VARCHAR(20)      | `main`, `winners`, `losers`, `grandfinal`              |
| `round_number`          | INTEGER          | 1-based within bracket_side                            |
| `match_number`          | INTEGER          | 1-based within round                                   |
| `participant_a_id`      | UUID FK NULLABLE | filled in once feeder resolves                         |
| `participant_b_id`      | UUID FK NULLABLE |                                                        |
| `counter_id`            | UUID FK NULLABLE | lazy — set when scorer opens the match                 |
| `winner_participant_id` | UUID FK NULLABLE |                                                        |
| `loser_participant_id`  | UUID FK NULLABLE |                                                        |
| `status`                | VARCHAR(20)      | `pending`, `ready`, `inprogress`, `completed`, `walkover` |
| `next_match_id`         | UUID FK NULLABLE | winner advances here                                   |
| `next_loser_match_id`   | UUID FK NULLABLE | double-elim: loser drops here                          |
| `winner_to_side_a`      | BOOLEAN          | which slot of next match the winner fills              |
| `loser_to_side_a`       | BOOLEAN          | likewise for the loser drop-in                         |
| `scheduled_at`          | TIMESTAMPTZ NULLABLE |                                                    |
| `created_at`            | TIMESTAMPTZ      |                                                        |
| `updated_at`            | TIMESTAMPTZ      |                                                        |

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

-- Tournaments
CREATE INDEX idx_tournaments_owner ON tournaments(owner_user_id) WHERE deleted_at IS NULL;
CREATE INDEX idx_tournaments_session ON tournaments(session_token_hash) WHERE deleted_at IS NULL;
CREATE INDEX idx_tournament_participants_t ON tournament_participants(tournament_id);
CREATE UNIQUE INDEX idx_tournament_participants_uname ON tournament_participants(tournament_id, team_name);
CREATE INDEX idx_tournament_matches_t ON tournament_matches(tournament_id);
CREATE INDEX idx_tournament_matches_counter ON tournament_matches(counter_id) WHERE counter_id IS NOT NULL;
```

---

## Migration Strategy

- All schema changes via EF Core Migrations.
- Migrations are idempotent and reviewed in PR before merge.
- No breaking schema changes to production tables — use additive migrations + code compatibility window.
- Never rename or drop columns directly — add new column, migrate data, drop old in a subsequent release.
