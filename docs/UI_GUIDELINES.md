# UI Guidelines

## Technology Decisions

| Concern           | Choice                                      |
|-------------------|---------------------------------------------|
| Framework         | Angular 21.2.10 — standalone components     |
| State             | Angular Signals (local); RxJS (async)       |
| Styling           | **Tailwind CSS 4.3** — utility-first; see rules below |
| UI Component Base | Angular Material 3 (M3)                     |
| Icons             | Material Symbols (variable font)            |
| Animations        | Angular Animations API; no raw CSS keyframes in components |
| Real-time         | `@microsoft/signalr` npm package            |

---

## Component Architecture

### Rule: Smart vs. Dumb

| Type  | Location              | Responsibility                                  |
|-------|-----------------------|-------------------------------------------------|
| Smart | `features/*/components/` | Owns data, talks to services, passes props down |
| Dumb  | `shared/components/`  | Pure display; accepts `@Input()`, emits `@Output()` |

Dumb components **never** inject services.
Smart components **never** contain display logic (style decisions, conditional CSS classes based on raw scores).

### Rule: One Component, One Responsibility

- `score-display` only shows a score — it never knows what sport it is.
- `set-tracker` only shows set counts — it does not increment them.
- `match-toolbar` owns the action buttons — it does not know the score.

### Rule: Standalone Only

All components use `standalone: true`. No `NgModule`. No `declarations` array.

---

## Tailwind CSS 4.3 — Rules

### Setup

Tailwind CSS 4 uses a **CSS-first configuration** — no `tailwind.config.js`.
The entire setup lives in `src/styles/styles.css`:

```css
@import "tailwindcss";
@import "./theme.css";
```

Install:
```bash
npm install tailwindcss@^4.3 @tailwindcss/vite  # or @tailwindcss/postcss if using PostCSS
```

For Angular CLI, use the PostCSS integration:
```bash
npm install -D @tailwindcss/postcss
```

Add to `postcss.config.js`:
```js
module.exports = {
  plugins: {
    '@tailwindcss/postcss': {},
  },
};
```

---

### Global Theme System

All design tokens are defined as **CSS custom properties** using Tailwind 4's `@theme` directive.
This is the single source of truth — no SCSS variables, no separate token files.

**File layout:**
```
src/styles/
├── styles.css       # Entry point — @import "tailwindcss" + @import "./theme.css"
├── theme.css        # @theme block: all design tokens + Angular Material M3 overrides
└── base.css         # Optional global base styles (body, focus rings, etc.)
```

**`theme.css` — define all tokens here:**

```css
@import "tailwindcss/theme" reference;

@theme {
  /* Colour palette — extend, don't replace Tailwind defaults */
  --color-primary:       #1565C0;
  --color-primary-light: #5E92F3;
  --color-primary-dark:  #003C8F;
  --color-on-primary:    #FFFFFF;

  --color-secondary:     #00897B;
  --color-on-secondary:  #FFFFFF;

  --color-surface:       #F8F9FA;
  --color-on-surface:    #1C1B1F;

  --color-error:         #B00020;
  --color-on-error:      #FFFFFF;

  /* Spacing — 8-pt grid mapped to Tailwind scale */
  --spacing-xs:  4px;
  --spacing-sm:  8px;
  --spacing-md:  16px;
  --spacing-lg:  24px;
  --spacing-xl:  40px;
  --spacing-2xl: 64px;

  /* Typography */
  --font-family-base: 'Roboto', sans-serif;
  --font-size-hero:   3rem;
  --line-height-hero: 1.1;

  /* Border radius */
  --radius-sm:  4px;
  --radius-md:  8px;
  --radius-lg:  16px;
  --radius-full: 9999px;
}
```

These tokens become Tailwind utility classes automatically:
- `bg-primary`, `text-on-primary`, `text-error`, `rounded-md`, etc.

### Dark Mode

Tailwind 4 dark mode is configured via `@variant`:

```css
/* In theme.css */
@variant dark (&:where([data-theme="dark"] *));
```

The theme toggle writes `data-theme="dark"` to `<html>`.
Use `dark:` utilities in templates: `class="bg-surface dark:bg-neutral-900"`.

---

### Angular Material + Tailwind Coexistence

Angular Material M3 uses its own CSS custom properties (`--mat-*`).
Map them to your Tailwind tokens in `theme.css` so they stay in sync:

```css
@theme {
  /* Map Angular Material tokens to your palette */
  --mat-primary: var(--color-primary);
  --mat-on-primary: var(--color-on-primary);
  --mat-surface: var(--color-surface);
}
```

Use Tailwind's `@layer` to avoid specificity fights with Material:

```css
@layer base {
  /* Global resets that apply under Material */
}
@layer components {
  /* Custom component classes built from Tailwind utilities */
}
```

---

### Utility-First Rules

| Rule | Detail |
|------|--------|
| **Utilities in templates** | Use Tailwind utility classes directly in Angular templates — this is the primary styling method. |
| **No inline `style` attributes** | Never use `style="..."` in templates. Use utility classes or `[class]` binding. |
| **Component classes for repeated patterns** | If the same utility combination appears 3+ times, extract it with `@apply` in a `@layer components` block. Do not create one-off component classes. |
| **No arbitrary values without justification** | Avoid `w-[347px]` — use the design scale. If a token is missing, add it to `@theme`. |
| **No raw colours** | Never use `bg-[#ff0000]` or hardcoded hex in templates. Reference only `@theme` tokens. |
| **Responsive via Tailwind breakpoints** | Use `sm:`, `md:`, `lg:` prefixes — never raw `@media` queries in component styles. |

Tailwind 4 breakpoints (match to layout table below):

```css
/* Default Tailwind 4 breakpoints — do not override */
/* sm: 640px, md: 768px, lg: 1024px, xl: 1280px */
```

---

### `@apply` — When and How

Only use `@apply` for shared, stable patterns that would otherwise be repeated verbatim:

```css
/* In theme.css or base.css — NOT inside component stylesheets */
@layer components {
  .pts-btn-primary {
    @apply bg-primary text-on-primary rounded-md px-md py-sm font-medium
           hover:bg-primary-dark focus-visible:outline-2 focus-visible:outline-primary
           disabled:opacity-50 disabled:cursor-not-allowed;
  }

  .pts-card {
    @apply bg-surface rounded-lg shadow-sm p-md;
  }
}
```

Components reference these class names like any utility — they are **not component-scoped styles**.

---

## Naming Conventions

| Element             | Convention                          | Example                          |
|---------------------|-------------------------------------|----------------------------------|
| Component selector  | `pts-` prefix + kebab-case          | `pts-score-display`              |
| Component file      | kebab-case + `.component.ts`        | `score-display.component.ts`     |
| Service             | kebab-case + `.service.ts`          | `counter.service.ts`             |
| Custom CSS class    | `pts-` prefix + kebab-case (only for `@layer components` entries) | `pts-btn-primary` |
| Signal              | `camelCase$` suffix                 | `currentScore$`                  |
| Observable          | `camelCase$` suffix                 | `scoreUpdates$`                  |

---

## Shared Component Catalogue (Phase 1)

These must be built as generic, reusable components in `shared/components/`:

| Component               | Inputs                              | Outputs            |
|-------------------------|-------------------------------------|--------------------|
| `pts-score-board`       | `teamA, teamB, scores`              | —                  |
| `pts-score-button`      | `label, disabled`                   | `increment, decrement` |
| `pts-set-indicator`     | `setsWon, totalSets`                | —                  |
| `pts-share-dialog`      | `counterId`                         | `tokenGenerated`   |
| `pts-sport-selector`    | `sports[]`                          | `sportSelected`    |
| `pts-team-name-editor`  | `teamName`                          | `nameChanged`      |
| `pts-confirm-dialog`    | `title, message`                    | `confirmed`        |
| `pts-toast`             | `message, type`                     | —                  |
| `pts-loading-spinner`   | `size, overlay`                     | —                  |

---

## Accessibility (a11y)

- All interactive elements must have ARIA labels if they lack visible text.
- Score buttons use `aria-label="Increment Team A score"`.
- Colour alone must never convey information — pair with icon or label.
- Focus management on dialog open/close using `cdkFocusTrap`.
- Minimum touch target: 48×48 px.
- Keyboard navigation must work for every user action.

---

## Responsive Design

| Breakpoint | Min Width | Tailwind prefix | Layout behaviour                       |
|------------|-----------|-----------------|----------------------------------------|
| (default)  | 0         | (none)          | Single column, full-width score boards |
| `sm`       | 640px     | `sm:`           | Two-column score layout                |
| `md`       | 768px     | `md:`           | Side-by-side + action panel            |
| `lg`       | 1024px    | `lg:`           | Full dashboard layout                  |

Use Tailwind responsive prefixes in templates (`sm:grid-cols-2`, `lg:flex`).
Never write raw `@media` queries in component styles.

---

## Route Structure

```
/                        → Home / sport selector
/counter/new             → Create counter (sport selection)
/counter/:id             → Live counter
/counter/join/:token     → Enter via share token
/settings                → User settings (authenticated)
/tournament              → Tournament list (Phase 2)
/tournament/:id          → Tournament detail (Phase 2)
/admin                   → Admin panel (role-gated, Phase 2)
```

All feature routes are **lazy-loaded**.

---

## Error Handling in the UI

- HTTP errors are caught by the global `ErrorInterceptor` in `core/interceptors/`.
- 401 → redirect to Authentik login.
- 403 → show `pts-access-denied` component.
- 404 → show `pts-not-found` component.
- 5xx / network → show `pts-toast` with retry option.
- Never show raw error messages or stack traces to users.

---

## Security in Templates

- Never use `[innerHTML]` with unsanitised strings. Use Angular's `DomSanitizer` only when unavoidable and with explicit review comment.
- Never interpolate user-supplied strings into `href`, `src`, or `style` bindings without sanitisation.
- All user-entered names (team names, tournament names) are escaped at render time — Angular's template engine handles this by default, do not bypass it.
