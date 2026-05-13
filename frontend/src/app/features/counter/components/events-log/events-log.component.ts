import { Component, computed, input, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { CounterEvent } from '../../../../shared/models/counter.model';

interface DisplayEvent {
  id: string;
  setNumber: number;
  createdAt: string;
  // Material symbol name for the row's leading icon.
  icon: string;
  // Tailwind class controlling the icon tint.
  iconClass: string;
  // Short label, e.g. "Team A +1" or "Undo · Team A +1".
  label: string;
  // Optional sub-line with the resulting score, e.g. "3 → 4".
  scoreLine: string | null;
  // True when this event has been rolled back and should render dimmed/struck.
  isUndone: boolean;
  // True for synthetic undo/redo entries — rendered with a different background.
  isMeta: boolean;
}

@Component({
  selector: 'pts-events-log',
  imports: [DatePipe],
  template: `
    <div class="pts-card !p-0 overflow-hidden">
      <button
        type="button"
        class="w-full flex items-center justify-between gap-3 px-4 py-3 text-left
               hover:bg-surface-variant/40 active:bg-surface-variant/60 transition-colors"
        [attr.aria-expanded]="expanded()"
        aria-controls="events-log-body"
        (click)="toggle()"
      >
        <span class="flex items-center gap-2 text-on-surface">
          <span class="material-symbols-rounded text-base text-on-surface-muted">history</span>
          <span class="text-sm font-medium">History</span>
          @if (count()) {
            <span class="pts-badge bg-surface-variant text-on-surface-muted text-[11px]">
              {{ count() }}
            </span>
          }
        </span>
        <span
          class="material-symbols-rounded text-on-surface-muted transition-transform"
          [class.rotate-180]="expanded()"
        >expand_more</span>
      </button>

      @if (expanded()) {
        <div id="events-log-body" class="border-t border-outline/20">
          @if (display().length === 0) {
            <p class="px-4 py-6 text-center text-sm text-on-surface-muted">
              No actions yet.
            </p>
          } @else {
            <ul class="divide-y divide-outline/15 max-h-72 overflow-y-auto">
              @for (e of display(); track e.id) {
                <li
                  class="flex items-center gap-3 px-4 py-2.5 text-sm"
                  [class.bg-surface-variant]="e.isMeta"
                  [class.opacity-50]="e.isUndone"
                >
                  <span
                    class="material-symbols-rounded text-base shrink-0"
                    [class]="e.iconClass"
                  >{{ e.icon }}</span>
                  <span class="flex-1 min-w-0">
                    <span
                      class="block truncate"
                      [class.line-through]="e.isUndone"
                    >{{ e.label }}</span>
                    @if (e.scoreLine) {
                      <span class="block text-xs text-on-surface-muted font-mono">
                        Set {{ e.setNumber }} · {{ e.scoreLine }}
                      </span>
                    }
                  </span>
                  <time class="text-[11px] text-on-surface-muted shrink-0">
                    {{ e.createdAt | date: 'shortTime' }}
                  </time>
                </li>
              }
            </ul>
          }
        </div>
      }
    </div>
  `,
})
export class EventsLogComponent {
  readonly events = input<CounterEvent[]>([]);
  readonly teamAName = input<string>('Team A');
  readonly teamBName = input<string>('Team B');

  readonly expanded = signal(false);

  readonly count = computed(() => this.events().length);

  // Newest first, with undo/redo entries rewritten to reference the action
  // they affect (so the row says "Undo · Team A +1" instead of "undo").
  readonly display = computed<DisplayEvent[]>(() => {
    const all = this.events();
    const byId = new Map(all.map((e) => [e.id, e]));
    return [...all].reverse().map((e) => this.toDisplay(e, byId));
  });

  toggle(): void {
    this.expanded.update((v) => !v);
  }

  private teamName(team: 'A' | 'B'): string {
    return team === 'A' ? this.teamAName() : this.teamBName();
  }

  private actionLabel(e: CounterEvent): string {
    const team = this.teamName(e.team);
    switch (e.eventType) {
      case 'score_increment':
        return `${team} +1`;
      case 'score_decrement':
        return `${team} −1`;
      default:
        return `${team}`;
    }
  }

  private scoreLine(e: CounterEvent): string {
    return `${e.scoreABefore}–${e.scoreBBefore} → ${e.scoreAAfter}–${e.scoreBAfter}`;
  }

  private toDisplay(e: CounterEvent, byId: Map<string, CounterEvent>): DisplayEvent {
    if (e.eventType === 'undo' || e.eventType === 'redo') {
      const related = e.relatedEventId ? byId.get(e.relatedEventId) ?? null : null;
      const verb = e.eventType === 'undo' ? 'Undo' : 'Redo';
      const icon = e.eventType === 'undo' ? 'undo' : 'redo';
      const label = related
        ? `${verb} · ${this.actionLabel(related)}`
        : verb;
      return {
        id: e.id,
        setNumber: e.setNumber,
        createdAt: e.createdAt,
        icon,
        iconClass: e.eventType === 'undo' ? 'text-warning' : 'text-primary',
        label,
        scoreLine: this.scoreLine(e),
        isUndone: false,
        isMeta: true,
      };
    }

    const isInc = e.eventType === 'score_increment';
    return {
      id: e.id,
      setNumber: e.setNumber,
      createdAt: e.createdAt,
      icon: isInc ? 'add' : 'remove',
      iconClass: e.team === 'A' ? 'text-team-a' : 'text-team-b',
      label: this.actionLabel(e),
      scoreLine: this.scoreLine(e),
      isUndone: e.isUndone,
      isMeta: false,
    };
  }
}
