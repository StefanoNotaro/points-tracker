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

  // Undo / redo / timeout-cancel events are not shown as separate rows —
  // the score (or timeout) row they affect already reflects the change via
  // the strike-through / faded style.
  private readonly visible = computed(() =>
    this.events().filter(
      (e) =>
        e.eventType !== 'undo'
        && e.eventType !== 'redo'
        && e.eventType !== 'timeout_canceled',
    ),
  );

  readonly count = computed(() => this.visible().length);

  readonly display = computed<DisplayEvent[]>(() =>
    [...this.visible()].reverse().map((e) => this.toDisplay(e)),
  );

  toggle(): void {
    this.expanded.update((v) => !v);
  }

  private teamName(team: 'A' | 'B'): string {
    return team === 'A' ? this.teamAName() : this.teamBName();
  }

  private scoreLine(e: CounterEvent): string {
    return `${e.scoreABefore}–${e.scoreBBefore} → ${e.scoreAAfter}–${e.scoreBAfter}`;
  }

  private toDisplay(e: CounterEvent): DisplayEvent {
    const team = this.teamName(e.team);
    const teamTint = e.team === 'A' ? 'text-team-a' : 'text-team-b';

    if (e.eventType === 'timeout') {
      return {
        id: e.id,
        setNumber: e.setNumber,
        createdAt: e.createdAt,
        icon: 'pause_circle',
        iconClass: teamTint,
        label: e.isUndone ? `${team} timeout (canceled)` : `${team} timeout`,
        // Timeouts don't change the score; the parenthetical clarifies that.
        scoreLine: `Score ${e.scoreAAfter}–${e.scoreBAfter}`,
        isUndone: e.isUndone,
        isMeta: true,
      };
    }

    const isInc = e.eventType === 'score_increment';
    return {
      id: e.id,
      setNumber: e.setNumber,
      createdAt: e.createdAt,
      icon: isInc ? 'add' : 'remove',
      iconClass: teamTint,
      label: isInc ? `${team} +1` : `${team} −1`,
      scoreLine: this.scoreLine(e),
      isUndone: e.isUndone,
      isMeta: false,
    };
  }
}
