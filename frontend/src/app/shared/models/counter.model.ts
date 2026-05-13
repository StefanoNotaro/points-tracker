import { SportType } from './sport.model';

export type CounterStatus = 'active' | 'finished' | 'abandoned';
export type ShareScope = 'read' | 'edit';
export type Team = 'A' | 'B';

export interface CounterSet {
  setNumber: number;
  scoreA: number;
  scoreB: number;
  winner: Team | null;
}

export type SideSwitchMode = 'none' | 'confirmeverysets' | 'autoeverypoints';

export interface SportRulesDto {
  pointsPerSet: number;
  lastSetPoints: number;
  setsToWin: number;
  totalSets: number;
  winByTwo: boolean;
  sideSwitchMode: SideSwitchMode;
  sideSwitchInterval: number;
  sideSwitchIntervalLastSet: number;
  timeoutsPerSet: number;
  timeoutDurationSeconds: number;
}

export interface Counter {
  id: string;
  sportType: SportType;
  ownerUserId: string | null;
  teamAName: string;
  teamBName: string;
  status: CounterStatus;
  sets: CounterSet[];
  currentSetNumber: number;
  setsWonA: number;
  setsWonB: number;
  currentScoreA: number;
  currentScoreB: number;
  isOwner: boolean;
  canEdit: boolean;
  createdAt: string;
  updatedAt: string;
  rules: SportRulesDto;
  sideSwitchCount: number;
  pendingSideSwitchConfirmation: boolean;
  indoorSwitchEverySets: number | null;
  beachAutoSwitchSides: boolean;
  canUndo: boolean;
  canRedo: boolean;
  timeoutsRemainingA: number;
  timeoutsRemainingB: number;
  activeTimeout: ActiveTimeout | null;
  events: CounterEvent[];
  linkedTournament: LinkedTournament | null;
}

export interface LinkedTournament {
  tournamentId: string;
  tournamentName: string;
  matchId: string;
}

export interface ActiveTimeout {
  team: Team;
  startedAt: string;
  durationSeconds: number;
}

export type CounterEventType =
  | 'score_increment'
  | 'score_decrement'
  | 'undo'
  | 'redo'
  | 'timeout'
  | 'timeout_canceled';

export interface CounterEvent {
  id: string;
  setNumber: number;
  eventType: CounterEventType;
  team: Team;
  scoreABefore: number;
  scoreBBefore: number;
  scoreAAfter: number;
  scoreBAfter: number;
  isUndone: boolean;
  relatedEventId: string | null;
  createdAt: string;
}

export interface CounterSummary {
  id: string;
  sportType: SportType;
  teamAName: string;
  teamBName: string;
  status: CounterStatus;
  setsWonA: number;
  setsWonB: number;
  currentScoreA: number;
  currentScoreB: number;
  createdAt: string;
  updatedAt: string;
}

export interface CreateCounterRequest {
  sportType: SportType;
  teamAName: string;
  teamBName: string;
  customRules?: Pick<SportRulesDto, 'pointsPerSet' | 'lastSetPoints' | 'setsToWin' | 'totalSets' | 'winByTwo'>;
  indoorSwitchEverySets?: number | null;
  beachAutoSwitchSides?: boolean;
  customTimeoutsPerSet?: number | null;
  customTimeoutDurationSeconds?: number | null;
}

export interface CreateCounterResponse {
  counter: Counter;
  sessionToken: string | null;
}

export interface ShareTokenResponse {
  token: string;
  shareUrl: string;
  scope: ShareScope;
  expiresAt: string;
}
