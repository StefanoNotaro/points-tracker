import { Component, computed, inject, input, output } from '@angular/core';
import { NgClass, NgTemplateOutlet } from '@angular/common';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import {
  BracketSide,
  TournamentFormat,
  TournamentMatch,
} from '../../../../shared/models/tournament.model';

// ── Layout constants (px) ────────────────────────────────────────────────
const CARD_H = 72;
const CONN_W = 28;
const UNIT   = 80; // CARD_H + 8px gap between slots in round 1
const HEADER_H = 32; // round label row height

// ── Types ────────────────────────────────────────────────────────────────
interface LayoutMatch extends TournamentMatch { topPx: number; }

interface BracketColumn {
  side: BracketSide;
  round: number;
  label: string;
  isActive: boolean;  // has ready / inprogress match
  isFuture: boolean;  // all matches pending
  matches: LayoutMatch[];
  connectorPaths: string[]; // SVG path `d` strings to draw into the next column gap
}

export interface BracketSection {
  columns: BracketColumn[];
  heightPx: number;
}

interface GroupSection {
  groupNumber: number;
  label: string;
  matches: TournamentMatch[];
  isActive: boolean;
  isComplete: boolean;
}

interface RoundSection {
  label: string;
  matches: TournamentMatch[];
  isActive: boolean;
  isComplete: boolean;
}

// ── Bracket layout builder ───────────────────────────────────────────────
function buildBracketSection(
  matches: TournamentMatch[],
  getLabelFn: (round: number) => string,
): BracketSection {
  if (!matches.length) return { columns: [], heightPx: 0 };

  const roundMap = new Map<number, TournamentMatch[]>();
  for (const m of matches) {
    const arr = roundMap.get(m.roundNumber) ?? [];
    arr.push(m);
    roundMap.set(m.roundNumber, arr);
  }

  const sortedRounds = [...roundMap.keys()].sort((a, b) => a - b);
  const n1 = (roundMap.get(sortedRounds[0]) ?? []).length;
  const heightPx = n1 * UNIT;

  const columns: BracketColumn[] = sortedRounds.map(r => {
    const rMatches = (roundMap.get(r) ?? []).sort((a, b) => a.matchNumber - b.matchNumber);
    const n = rMatches.length;
    const slotH  = heightPx / n;
    const offset = (slotH - CARD_H) / 2;

    return {
      side: rMatches[0].bracketSide,
      round: r,
      label: getLabelFn(r),
      isActive: rMatches.some(m => m.status === 'ready' || m.status === 'inprogress'),
      isFuture: rMatches.every(m => m.status === 'pending'),
      matches: rMatches.map((m, i) => ({ ...m, topPx: i * slotH + offset })),
      connectorPaths: [],
    };
  });

  // Wire SVG connector paths between adjacent columns using nextMatchId links
  for (let ci = 0; ci < columns.length - 1; ci++) {
    const col     = columns[ci];
    const nextCol = columns[ci + 1];
    const paths: string[] = [];

    // Group feeders by their target match id
    const byTarget = new Map<string, LayoutMatch[]>();
    for (const m of col.matches) {
      if (!m.nextMatchId) continue;
      if (!nextCol.matches.some(t => t.id === m.nextMatchId)) continue;
      const list = byTarget.get(m.nextMatchId) ?? [];
      list.push(m);
      byTarget.set(m.nextMatchId, list);
    }

    for (const [targetId, feeders] of byTarget) {
      const target = nextCol.matches.find(t => t.id === targetId)!;
      const ty   = target.topPx + CARD_H / 2;
      const half = CONN_W / 2;

      if (feeders.length === 1) {
        const fy = feeders[0].topPx + CARD_H / 2;
        // Single feeder (bye): straight L-shape
        paths.push(`M 0 ${fy} H ${half} V ${ty} H ${CONN_W}`);
      } else {
        const [top, bot] = [...feeders].sort((a, b) => a.topPx - b.topPx);
        const y1 = top.topPx + CARD_H / 2;
        const y2 = bot.topPx + CARD_H / 2;
        const ym = (y1 + y2) / 2;
        // Classic bracket ⊢: horizontals to midpoint, vertical spine, then to target
        paths.push(
          `M 0 ${y1} H ${half}`,
          `M 0 ${y2} H ${half}`,
          `M ${half} ${y1} V ${y2}`,
          `M ${half} ${ym} V ${ty} H ${CONN_W}`,
        );
      }
    }

    col.connectorPaths = paths;
  }

  return { columns, heightPx };
}

// ── Component ────────────────────────────────────────────────────────────
@Component({
  selector: 'pts-bracket-view',
  imports: [NgClass, NgTemplateOutlet, TranslatePipe],
  templateUrl: './bracket-view.component.html',
})
export class BracketViewComponent {
  readonly matches   = input.required<TournamentMatch[]>();
  readonly format    = input.required<TournamentFormat>();
  readonly canEdit   = input<boolean>(false);
  readonly matchClicked       = output<TournamentMatch>();
  readonly scorerLinkClicked  = output<TournamentMatch>();

  // Expose layout constants for inline-style bindings in the template
  readonly CONN_W   = CONN_W;
  readonly HEADER_H = HEADER_H;

  private readonly i18n = inject(TranslateService);

  // ── Computed sections ────────────────────────────────────────────────

  readonly knockoutMain = computed<BracketSection | null>(() => {
    const fmt  = this.format();
    const side: BracketSide = fmt === 'doubleelimination' ? 'winners' : 'main';
    const ms   = this.matches().filter(m => m.bracketSide === side);
    if (!ms.length) return null;
    const maxR = Math.max(...ms.map(m => m.roundNumber));
    return buildBracketSection(ms, r => this.mainRoundLabel(side, r, maxR));
  });

  readonly knockoutLosers = computed<BracketSection | null>(() => {
    if (this.format() !== 'doubleelimination') return null;
    const ms = this.matches().filter(m => m.bracketSide === 'losers');
    if (!ms.length) return null;
    return buildBracketSection(ms, r =>
      `${this.i18n.instant('tournament.bracket.losersPrefix')} · ${this.i18n.instant('tournament.bracket.round', { n: r })}`
    );
  });

  readonly grandFinal = computed<TournamentMatch | null>(() => {
    if (this.format() !== 'doubleelimination') return null;
    return this.matches().find(m => m.bracketSide === 'grandfinal') ?? null;
  });

  readonly groups = computed<GroupSection[]>(() => {
    if (this.format() !== 'groupstageelimination') return [];
    const ms = this.matches().filter(m => m.bracketSide === 'groupstage');
    const gmap = new Map<number, TournamentMatch[]>();
    for (const m of ms) {
      if (m.groupNumber == null) continue;
      const arr = gmap.get(m.groupNumber) ?? [];
      arr.push(m);
      gmap.set(m.groupNumber, arr);
    }
    return [...gmap.entries()]
      .sort(([a], [b]) => a - b)
      .map(([gn, gms]) => {
        const sorted = [...gms].sort((a, b) => a.matchNumber - b.matchNumber);
        return {
          groupNumber: gn,
          label: this.i18n.instant('tournament.bracket.group', { n: gn }),
          matches: sorted,
          isActive:   sorted.some(m => m.status === 'ready' || m.status === 'inprogress'),
          isComplete: sorted.every(m => m.status === 'completed' || m.status === 'walkover'),
        };
      });
  });

  readonly roundRobinRounds = computed<RoundSection[]>(() => {
    if (this.format() !== 'roundrobin') return [];
    const ms = this.matches().filter(m => m.bracketSide === 'main');
    const rmap = new Map<number, TournamentMatch[]>();
    for (const m of ms) {
      const arr = rmap.get(m.roundNumber) ?? [];
      arr.push(m);
      rmap.set(m.roundNumber, arr);
    }
    return [...rmap.entries()]
      .sort(([a], [b]) => a - b)
      .map(([r, rms]) => {
        const sorted = [...rms].sort((a, b) => a.matchNumber - b.matchNumber);
        return {
          label: this.i18n.instant('tournament.bracket.round', { n: r }),
          matches: sorted,
          isActive:   sorted.some(m => m.status === 'ready' || m.status === 'inprogress'),
          isComplete: sorted.every(m => m.status === 'completed' || m.status === 'walkover'),
        };
      });
  });

  // ── Helpers ──────────────────────────────────────────────────────────

  private mainRoundLabel(side: BracketSide, round: number, maxRound: number): string {
    const prefix = side === 'winners'
      ? `${this.i18n.instant('tournament.bracket.winnersPrefix')} · `
      : '';
    if (round === maxRound)     return `${prefix}${this.i18n.instant('tournament.bracket.final')}`;
    if (round === maxRound - 1) return `${prefix}${this.i18n.instant('tournament.bracket.semifinals')}`;
    if (round === maxRound - 2) return `${prefix}${this.i18n.instant('tournament.bracket.quarterfinals')}`;
    return `${prefix}${this.i18n.instant('tournament.bracket.round', { n: round })}`;
  }

  viewBox(section: BracketSection): string {
    return `0 0 ${CONN_W} ${section.heightPx}`;
  }

  winnerIs(m: TournamentMatch, slot: 'A' | 'B'): boolean {
    if (!m.winnerParticipantId) return false;
    return slot === 'A'
      ? m.winnerParticipantId === m.participantAId
      : m.winnerParticipantId === m.participantBId;
  }

  badgeClass(s: string): string {
    return s === 'inprogress' ? 'bg-success/15 text-success'
         : s === 'completed'  ? 'bg-surface-variant text-on-surface-muted'
         : s === 'ready'      ? 'bg-primary/15 text-primary'
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

  canManageScorerLinks(m: TournamentMatch): boolean {
    if (!this.canEdit()) return false;
    return m.status === 'ready' || m.status === 'inprogress';
  }

  openScorerLinks(m: TournamentMatch, event: Event): void {
    event.stopPropagation();
    this.scorerLinkClicked.emit(m);
  }
}
