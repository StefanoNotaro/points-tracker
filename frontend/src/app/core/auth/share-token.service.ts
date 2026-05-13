import { Injectable } from '@angular/core';

const STORAGE_PREFIX = 'pts_share_';

/**
 * Remembers the share token a visitor used to join a counter, keyed by counter id.
 * Sent on subsequent requests via the X-Share-Token header so the visitor keeps
 * the access level (read/edit) the original link granted them — without ever
 * putting the token back into the URL.
 */
@Injectable({ providedIn: 'root' })
export class ShareTokenService {
  getToken(counterId: string): string | null {
    return localStorage.getItem(`${STORAGE_PREFIX}${counterId}`);
  }

  setToken(counterId: string, token: string): void {
    localStorage.setItem(`${STORAGE_PREFIX}${counterId}`, token);
  }

  removeToken(counterId: string): void {
    localStorage.removeItem(`${STORAGE_PREFIX}${counterId}`);
  }
}
