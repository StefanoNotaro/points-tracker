import { Injectable } from '@angular/core';

const STORAGE_PREFIX = 'pts_session_';

/**
 * Manages anonymous session tokens for counter ownership.
 * Tokens are stored in localStorage keyed by counter ID.
 * They are sent via X-Session-Token header, never in URLs.
 */
@Injectable({ providedIn: 'root' })
export class SessionTokenService {
  getToken(counterId: string): string | null {
    return localStorage.getItem(`${STORAGE_PREFIX}${counterId}`);
  }

  setToken(counterId: string, token: string): void {
    localStorage.setItem(`${STORAGE_PREFIX}${counterId}`, token);
  }

  removeToken(counterId: string): void {
    localStorage.removeItem(`${STORAGE_PREFIX}${counterId}`);
  }

  hasToken(counterId: string): boolean {
    return this.getToken(counterId) !== null;
  }

  getAllCounterIds(): string[] {
    return Object.keys(localStorage)
      .filter((key) => key.startsWith(STORAGE_PREFIX))
      .map((key) => key.slice(STORAGE_PREFIX.length));
  }

  getAllTournamentTokens(): string[] {
    const prefix = `${STORAGE_PREFIX}tournament:`;
    return Object.keys(localStorage)
      .filter((key) => key.startsWith(prefix))
      .map((key) => localStorage.getItem(key))
      .filter((v): v is string => !!v);
  }
}
