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

export const TOURNAMENT_FORMATS: { value: TournamentFormat; label: string; description: string; icon: string }[] = [
  {
    value: 'singleelimination',
    label: 'Single elimination',
    description: 'Knockout bracket — lose once and you’re out.',
    icon: 'account_tree',
  },
  {
    value: 'doubleelimination',
    label: 'Double elimination',
    description: 'Two-loss elimination with a winners and losers bracket.',
    icon: 'fork_right',
  },
  {
    value: 'roundrobin',
    label: 'Round robin',
    description: 'Everyone plays everyone — ranked by wins.',
    icon: 'all_inclusive',
  },
  {
    value: 'groupstageelimination',
    label: 'Group stage + knockout',
    description: 'Round-robin in groups, then the top finishers fight in a bracket.',
    icon: 'workspaces',
  },
];
