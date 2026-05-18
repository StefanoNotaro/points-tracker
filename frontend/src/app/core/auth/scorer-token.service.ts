import { Injectable } from '@angular/core';

const STORAGE_PREFIX = 'pts_scorer_';

/**
 * Stores the raw scorer invite token a referee used to join a match counter,
 * keyed by counter ID. Sent via the X-Scorer-Token header on all write
 * operations so the backend can authorise the scoring actions.
 *
 * Tokens are kept in sessionStorage (not localStorage) — they expire with
 * the browser tab, which aligns with the match-lifetime semantics of scorer links.
 */
@Injectable({ providedIn: 'root' })
export class ScorerTokenService {
  getToken(counterId: string): string | null {
    return sessionStorage.getItem(`${STORAGE_PREFIX}${counterId}`);
  }

  setToken(counterId: string, token: string): void {
    sessionStorage.setItem(`${STORAGE_PREFIX}${counterId}`, token);
  }

  removeToken(counterId: string): void {
    sessionStorage.removeItem(`${STORAGE_PREFIX}${counterId}`);
  }

  hasToken(counterId: string): boolean {
    return this.getToken(counterId) !== null;
  }
}
