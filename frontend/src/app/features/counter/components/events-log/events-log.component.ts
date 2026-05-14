import { Component, computed, inject, input, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { CounterEvent } from '../../../../shared/models/counter.model';

interface DisplayEvent {
  id: string;
  setNumber: number;
  createdAt: string;
  icon: string;
  iconClass: string;
  label: string;
  scoreLine: string | null;
  isUndone: boolean;
  isMeta: boolean;
}

@Component({
  selector: 'pts-events-log',
  imports: [DatePipe, TranslatePipe],
  templateUrl: './events-log.component.html',
})
export class EventsLogComponent {
  readonly events = input<CounterEvent[]>([]);
  readonly teamAName = input<string>('Team A');
  readonly teamBName = input<string>('Team B');

  private readonly i18n = inject(TranslateService);

  readonly expanded = signal(false);

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
      const labelKey = e.isUndone ? 'counter.events.timeoutCanceled' : 'counter.events.timeoutLabel';
      return {
        id: e.id,
        setNumber: e.setNumber,
        createdAt: e.createdAt,
        icon: 'pause_circle',
        iconClass: teamTint,
        label: this.i18n.instant(labelKey, { team }),
        scoreLine: this.i18n.instant('counter.events.scoreBaseline', { a: e.scoreAAfter, b: e.scoreBAfter }),
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
      label: this.i18n.instant(isInc ? 'counter.events.scorePlus' : 'counter.events.scoreMinus', { team }),
      scoreLine: this.scoreLine(e),
      isUndone: e.isUndone,
      isMeta: false,
    };
  }
}
