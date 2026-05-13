import { Component, computed, input, output } from '@angular/core';
import { NgClass } from '@angular/common';
import { TournamentMatch, BracketSide } from '../../../../shared/models/tournament.model';

interface Round {
  side: BracketSide;
  roundNumber: number;
  matches: TournamentMatch[];
}

@Component({
  selector: 'pts-bracket-view',
  imports: [NgClass],
  template: `
    <div class="flex flex-col gap-6">
      @for (group of grouped(); track group.side + ':' + group.roundNumber) {
        <section class="flex flex-col gap-2">
          <h3 class="pts-label flex items-center gap-2">
            <span>{{ labelFor(group.side, group.roundNumber) }}</span>
          </h3>
          <ul class="flex flex-col gap-2">
            @for (m of group.matches; track m.id) {
              <li>
                <button type="button"
                        class="w-full pts-card !p-3 text-left flex items-center gap-3 transition-colors"
                        [class.hover:border-primary]="canOpen(m)"
                        [class.opacity-60]="!canOpen(m) && !m.winnerParticipantId"
                        (click)="openMatch(m)">
                  <span class="flex-1 min-w-0 flex flex-col gap-1">
                    <span class="flex items-center justify-between gap-2">
                      <span class="text-sm truncate"
                            [ngClass]="winnerIs(m, 'A') ? 'font-bold text-on-surface' : 'text-on-surface'">
                        {{ m.participantAName ?? 'TBD' }}
                      </span>
                      <span class="text-xs text-on-surface-muted">{{ winnerIs(m, 'A') ? 'W' : '' }}</span>
                    </span>
                    <span class="flex items-center justify-between gap-2">
                      <span class="text-sm truncate"
                            [ngClass]="winnerIs(m, 'B') ? 'font-bold text-on-surface' : 'text-on-surface'">
                        {{ m.participantBName ?? 'TBD' }}
                      </span>
                      <span class="text-xs text-on-surface-muted">{{ winnerIs(m, 'B') ? 'W' : '' }}</span>
                    </span>
                  </span>
                  <span class="pts-badge shrink-0"
                        [ngClass]="badgeClass(m.status)">{{ statusLabel(m.status) }}</span>
                </button>
              </li>
            }
          </ul>
        </section>
      }
    </div>
  `,
})
export class BracketViewComponent {
  readonly matches = input.required<TournamentMatch[]>();
  readonly canEdit = input<boolean>(false);
  readonly matchClicked = output<TournamentMatch>();

  readonly grouped = computed<Round[]>(() => {
    const map = new Map<string, Round>();
    for (const m of this.matches()) {
      const key = `${m.bracketSide}:${m.roundNumber}`;
      if (!map.has(key)) map.set(key, { side: m.bracketSide, roundNumber: m.roundNumber, matches: [] });
      map.get(key)!.matches.push(m);
    }
    return Array.from(map.values())
      .sort((a, b) => sideOrder(a.side) - sideOrder(b.side) || a.roundNumber - b.roundNumber)
      .map((r) => ({ ...r, matches: r.matches.sort((a, b) => a.matchNumber - b.matchNumber) }));
  });

  labelFor(side: BracketSide, round: number): string {
    const prefix = side === 'winners' ? 'Winners · '
      : side === 'losers' ? 'Losers · '
      : side === 'grandfinal' ? 'Grand final'
      : '';
    if (side === 'grandfinal') return prefix;
    return `${prefix}Round ${round}`;
  }

  winnerIs(m: TournamentMatch, slot: 'A' | 'B'): boolean {
    if (!m.winnerParticipantId) return false;
    return slot === 'A' ? m.winnerParticipantId === m.participantAId
                        : m.winnerParticipantId === m.participantBId;
  }

  statusLabel(s: string): string {
    return s === 'inprogress' ? 'Live'
         : s === 'completed' ? 'Done'
         : s === 'ready' ? 'Ready'
         : s === 'walkover' ? 'Bye'
         : '—';
  }

  badgeClass(s: string): string {
    return s === 'inprogress' ? 'bg-success/15 text-success'
         : s === 'completed' ? 'bg-surface-variant text-on-surface-muted'
         : s === 'ready' ? 'bg-primary/15 text-primary'
         : 'bg-surface-variant text-on-surface-muted';
  }

  /**
   * Organisers can open a Ready match (spawns counter) or any match that
   * already has a linked counter. Viewers can only navigate to existing
   * linked counters (they aren't allowed to spawn new ones).
   */
  canOpen(m: TournamentMatch): boolean {
    if (m.counterId) return true;
    if (!this.canEdit()) return false;
    return !!m.participantAId && !!m.participantBId
           && m.status !== 'completed' && m.status !== 'walkover';
  }

  openMatch(m: TournamentMatch): void {
    if (!this.canOpen(m)) return;
    this.matchClicked.emit(m);
  }
}

function sideOrder(s: BracketSide): number {
  return s === 'main' ? 0 : s === 'winners' ? 1 : s === 'losers' ? 2 : 3;
}
