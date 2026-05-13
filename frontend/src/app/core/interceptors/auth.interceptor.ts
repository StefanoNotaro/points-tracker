import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../auth/auth.service';
import { SessionTokenService } from '../auth/session-token.service';
import { ShareTokenService } from '../auth/share-token.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const sessionTokens = inject(SessionTokenService);
  const shareTokens = inject(ShareTokenService);

  const headers: Record<string, string> = {};

  const accessToken = auth.getAccessToken();
  if (accessToken) {
    headers['Authorization'] = `Bearer ${accessToken}`;
  }

  // For requests targeting a specific counter, attach session and/or share token.
  // Both can apply: a user may own one counter (session token) and have edit-level
  // share access to another. The backend uses whichever grants access.
  const counterIdMatch = req.url.match(/\/counters\/([0-9a-f-]{36})/i);
  if (counterIdMatch) {
    const counterId = counterIdMatch[1];

    if (!accessToken) {
      const sessionToken = sessionTokens.getToken(counterId);
      if (sessionToken) headers['X-Session-Token'] = sessionToken;
    }

    const shareToken = shareTokens.getToken(counterId);
    if (shareToken) headers['X-Share-Token'] = shareToken;
  }

  // Tournaments use the same X-Session-Token scheme for anonymous ownership.
  // Storage key is namespaced ("tournament:{id}") so it doesn't collide with
  // a counter of the same UUID. For tournament create POST the token isn't
  // known yet — the response carries it, then TournamentService.create stores it.
  const tournamentIdMatch = req.url.match(/\/tournaments\/([0-9a-f-]{36})/i);
  if (tournamentIdMatch && !accessToken) {
    const tournamentId = tournamentIdMatch[1];
    const sessionToken = sessionTokens.getToken(`tournament:${tournamentId}`);
    if (sessionToken) headers['X-Session-Token'] = sessionToken;
  }

  return Object.keys(headers).length === 0
    ? next(req)
    : next(req.clone({ setHeaders: headers }));
};
