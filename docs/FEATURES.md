# Feature Specifications

## Phase 1 — Counter

### F-C01: Create a Counter

**Who:** Anyone (anonymous or authenticated)

**Behaviour:**
- User selects a sport type (Volleyball, Beach Volleyball).
- User optionally sets team/player names.
- System creates a counter with a unique ID and returns a session token for anonymous users.
- The counter page URL is `/counter/{id}` — shareable.

**Rules:**
- Sport type determines scoring rules (e.g. sets, points per set, deciding set cap).
- Counter starts at 0–0.
- Counter state is always persisted server-side.

---

### F-C02: Score Management

**Who:** Counter owner or edit-permission holder

**Behaviour:**
- Increment/decrement score per team.
- Undo last action (one step undo minimum).
- Reset current set.
- Advance to next set automatically when set-winning condition is met.
- Display current set score and total sets won per team.

**Rules per sport:**

| Rule                  | Volleyball (6-aside)  | Beach Volleyball       |
|-----------------------|-----------------------|------------------------|
| Points per set        | 25 (last set 15)      | 21 (last set 15)       |
| Win by                | 2 points              | 2 points               |
| Sets to win match     | 3 of 5                | 2 of 3                 |
| Tech timeout          | At 8 and 16 (set 1–4)| None                   |

These rules live in a configurable Sport Rules engine — not hardcoded UI logic.

---

### F-C03: Page Refresh Persistence

**Who:** Anyone viewing the counter

**Behaviour:**
- On refresh, the Angular app reads the `counter_id` from the URL and the `session_token` from `localStorage`.
- It calls `GET /api/counters/{id}` with the appropriate auth header.
- The full current state is returned and the UI re-hydrates.
- SignalR connection is re-established automatically.

**No data is ever stored only in memory or component state.**

---

### F-C04: Counter Sharing

**Who:** Counter owner (anonymous session token owner or authenticated user)

**Behaviour:**
- Owner opens share dialog.
- Selects permission level: **View** or **Edit**.
- System generates a share link: `/counter/join/{shareToken}`.
- Anyone opening that link sees (or can edit) the counter.
- Owner can revoke any share token at any time.
- Share tokens expire after a configurable duration (default: 7 days; configurable by authenticated users).

**Anonymous owner limitation:** Share tokens for anonymous counters are tied to the `session_token`.
If the owner clears `localStorage`, they lose ownership but existing share links still work until expiry.

---

### F-C05: Claim Anonymous Counter

**Who:** Authenticated user who created an anonymous counter

**Behaviour:**
- After login, if `localStorage` contains a `session_token` for a counter that has no owner, the user can claim it.
- Claiming transfers ownership to the authenticated user's account.
- The `session_token` is invalidated after claim.

---

## Phase 2 — Tournaments

### F-T01: Tournament Creation

**Who:** Admin or Super-Admin

**Behaviour:**
- Create a tournament with: name, sport, format (single elimination, double elimination, round robin), date range.
- Configure seeding, team sizes, number of participants.
- Tournament is in **Draft** status until published.

---

### F-T02: Tournament Draw & Brackets

**Who:** Admin

**Behaviour:**
- Generate bracket/draw from registered teams.
- Manual seeding overrides available.
- Bracket is visible (read-only) to all users and anonymous viewers once published.

---

### F-T03: Match Result Entry

**Who:** Admin, or delegated scorer using an in-match counter

**Behaviour:**
- Admins can manually enter results.
- Optionally link a live counter session to a tournament match — scores flow directly into bracket.

---

### F-T04: Standings & Statistics

**Who:** Anyone

**Behaviour:**
- Round-robin standings (wins, losses, sets, points).
- Bracket progression visualisation.
- Match history per team.

---

### F-T05: Tournament Roles

See `docs/ROLES_PERMISSIONS.md` for full role definitions.

---

## Phase 1 Scope Boundary

The following are explicitly **out of scope for Phase 1**:

- Tournaments of any kind.
- Player/team profiles beyond a display name per counter.
- Statistics or history dashboards.
- Push notifications.
- Mobile-native apps (PWA is acceptable but not required).
