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

  return Object.keys(headers).length === 0
    ? next(req)
    : next(req.clone({ setHeaders: headers }));
};
