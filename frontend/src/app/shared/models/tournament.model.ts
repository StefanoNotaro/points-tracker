import { SportType } from './sport.model';

export type TournamentFormat = 'singleelimination' | 'doubleelimination' | 'roundrobin' | 'groupstageelimination';
export type TournamentStatus = 'draft' | 'registration' | 'active' | 'completed' | 'abandoned';
export type BracketSide = 'main' | 'winners' | 'losers' | 'grandfinal' | 'groupstage';
export type TournamentMatchStatus = 'pending' | 'ready' | 'inprogress' | 'completed' | 'walkover';

export interface StageRulesDto {
  pointsPerSet: number | null;
  lastSetPoints: number | null;
  setsToWin: number | null;
  totalSets: number | null;
  winByTwo: boolean | null;
  timeoutsPerSet: number | null;
  timeoutDurationSeconds: number | null;
}

export interface TournamentRulesDto {
  customPointsPerSet: number | null;
  customLastSetPoints: number | null;
  customSetsToWin: number | null;
  customTotalSets: number | null;
  customWinByTwo: boolean | null;
  indoorSwitchEverySets: number | null;
  beachAutoSwitchSides: boolean;
  customTimeoutsPerSet: number | null;
  customTimeoutDurationSeconds: number | null;
  groupCount: number | null;
  advancePerGroup: number | null;
  finalRules: StageRulesDto | null;
  semifinalRules: StageRulesDto | null;
}

export interface TournamentParticipant {
  id: string;
  teamName: string;
  seed: number | null;
  userId: string | null;
}

export interface TournamentMatch {
  id: string;
  bracketSide: BracketSide;
  roundNumber: number;
  matchNumber: number;
  groupNumber: number | null;
  participantAId: string | null;
  participantAName: string | null;
  participantBId: string | null;
  participantBName: string | null;
  counterId: string | null;
  winnerParticipantId: string | null;
  status: TournamentMatchStatus;
  nextMatchId: string | null;
  nextLoserMatchId: string | null;
  scheduledAt: string | null;
}

export interface TournamentStanding {
  participantId: string;
  teamName: string;
  matchesPlayed: number;
  wins: number;
  losses: number;
}

export interface Tournament {
  id: string;
  name: string;
  sportType: SportType;
  format: TournamentFormat;
  status: TournamentStatus;
  ownerUserId: string | null;
  isOwner: boolean;
  canEdit: boolean;
  createdAt: string;
  updatedAt: string;
  startsAt: string | null;
  endsAt: string | null;
  rules: TournamentRulesDto;
  participants: TournamentParticipant[];
  matches: TournamentMatch[];
  standings: TournamentStanding[];
}

export interface TournamentSummary {
  id: string;
  name: string;
  sportType: SportType;
  format: TournamentFormat;
  status: TournamentStatus;
  participantCount: number;
  createdAt: string;
  updatedAt: string;
}

export interface CustomRulesPayload {
  pointsPerSet: number;
  lastSetPoints: number;
  setsToWin: number;
  totalSets: number;
  winByTwo: boolean;
}

export interface CreateTournamentRequest {
  name: string;
  sportType: SportType;
  format: TournamentFormat;
  customRules?: CustomRulesPayload;
  indoorSwitchEverySets?: number | null;
  beachAutoSwitchSides?: boolean;
  customTimeoutsPerSet?: number | null;
  customTimeoutDurationSeconds?: number | null;
  groupCount?: number | null;
  advancePerGroup?: number | null;
}

export interface UpdateTournamentRequest {
  name?: string;
  startsAt?: string | null;
  endsAt?: string | null;
  clearStartsAt?: boolean;
  clearEndsAt?: boolean;
  customRules?: CustomRulesPayload | null;
  clearCustomRules?: boolean;
  indoorSwitchEverySets?: number | null;
  beachAutoSwitchSides?: boolean;
  customTimeoutsPerSet?: number | null;
  customTimeoutDurationSeconds?: number | null;
  finalRules?: CustomRulesPayload | null;
  finalTimeoutsPerSet?: number | null;
  finalTimeoutDurationSeconds?: number | null;
  clearFinalRules?: boolean;
  semifinalRules?: CustomRulesPayload | null;
  semifinalTimeoutsPerSet?: number | null;
  semifinalTimeoutDurationSeconds?: number | null;
  clearSemifinalRules?: boolean;
}

export interface CreateTournamentResponse {
  tournament: Tournament;
  sessionToken: string | null;
}

export interface MatchScorerLink {
  id: string;
  tournamentId: string;
  matchId: string;
  label: string | null;
  grantedToUserId: string | null;
  isActive: boolean;
  createdAt: string;
}

export interface IssuedMatchScorerLink extends MatchScorerLink {
  token: string;
}

export interface ScorerJoinDto {
  counterId: string | null;
  tournamentId: string;
  matchId: string;
}

/**
 * Minimum number of participants for a tournament to be startable.
 * For group-stage, the minimum scales with the group count (each group needs ≥ 2 teams).
 */
export function minTeamsForFormat(
  format: TournamentFormat,
  groupCount: number | null | undefined = null,
): number {
  switch (format) {
    case 'singleelimination': return 2;
    case 'doubleelimination': return 4;
    case 'roundrobin': return 3;
    case 'groupstageelimination': return Math.max(4, (groupCount ?? 2) * 2);
  }
}

/**
 * (Groups × advance) must be a power of two and at least 2 — the resulting
 * bracket needs to be a clean knockout.
 */
export function isValidGroupConfig(groupCount: number, advancePerGroup: number): boolean {
  if (groupCount < 2 || advancePerGroup < 1) return false;
  const slots = groupCount * advancePerGroup;
  if (slots < 2) return false;
  return (slots & (slots - 1)) === 0;
}

/**
 * Tournament format metadata. `labelKey` / `descriptionKey` are
 * ngx-translate keys, not display strings — pipe them through `| translate`.
 */
export const TOURNAMENT_FORMATS: {
  value: TournamentFormat;
  labelKey: string;
  descriptionKey: string;
  icon: string;
}[] = [
  {
    value: 'singleelimination',
    labelKey: 'tournament.format.singleelimination.label',
    descriptionKey: 'tournament.format.singleelimination.description',
    icon: 'account_tree',
  },
  {
    value: 'doubleelimination',
    labelKey: 'tournament.format.doubleelimination.label',
    descriptionKey: 'tournament.format.doubleelimination.description',
    icon: 'fork_right',
  },
  {
    value: 'roundrobin',
    labelKey: 'tournament.format.roundrobin.label',
    descriptionKey: 'tournament.format.roundrobin.description',
    icon: 'all_inclusive',
  },
  {
    value: 'groupstageelimination',
    labelKey: 'tournament.format.groupstageelimination.label',
    descriptionKey: 'tournament.format.groupstageelimination.description',
    icon: 'workspaces',
  },
];
