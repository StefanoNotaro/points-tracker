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

export interface Counter {
  id: string;
  sportType: SportType;
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
}

export interface CreateCounterRequest {
  sportType: SportType;
  teamAName: string;
  teamBName: string;
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
