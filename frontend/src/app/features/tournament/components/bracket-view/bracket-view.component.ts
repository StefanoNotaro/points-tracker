import { Component, computed, inject, input, output } from '@angular/core';
import { NgClass } from '@angular/common';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { TournamentMatch, BracketSide } from '../../../../shared/models/tournament.model';

interface Round {
  side: BracketSide;
  roundNumber: number;
  matches: TournamentMatch[];
}

@Component({
  selector: 'pts-bracket-view',
  imports: [NgClass, TranslatePipe],
  templateUrl: './bracket-view.component.html',
})
export class BracketViewComponent {
  readonly matches = input.required<TournamentMatch[]>();
  readonly canEdit = input<boolean>(false);
  readonly matchClicked = output<TournamentMatch>();

  private readonly i18n = inject(TranslateService);

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
    if (side === 'grandfinal') return this.i18n.instant('tournament.bracket.grandFinal');
    const roundLabel = this.i18n.instant('tournament.bracket.round', { n: round });
    if (side === 'winners') return `${this.i18n.instant('tournament.bracket.winnersPrefix')} · ${roundLabel}`;
    if (side === 'losers')  return `${this.i18n.instant('tournament.bracket.losersPrefix')} · ${roundLabel}`;
    return roundLabel;
  }

  winnerIs(m: TournamentMatch, slot: 'A' | 'B'): boolean {
    if (!m.winnerParticipantId) return false;
    return slot === 'A' ? m.winnerParticipantId === m.participantAId
                        : m.winnerParticipantId === m.participantBId;
  }

  badgeClass(s: string): string {
    return s === 'inprogress' ? 'bg-success/15 text-success'
         : s === 'completed' ? 'bg-surface-variant text-on-surface-muted'
         : s === 'ready' ? 'bg-primary/15 text-primary'
         : 'bg-surface-variant text-on-surface-muted';
  }

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
