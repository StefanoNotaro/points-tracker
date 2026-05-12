import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../auth/auth.service';
import { SessionTokenService } from '../auth/session-token.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const sessionTokens = inject(SessionTokenService);

  const accessToken = auth.getAccessToken();
  if (accessToken) {
    return next(req.clone({ setHeaders: { Authorization: `Bearer ${accessToken}` } }));
  }

  // For anonymous requests targeting a specific counter, attach session token
  const counterIdMatch = req.url.match(/\/counters\/([0-9a-f-]{36})/i);
  if (counterIdMatch) {
    const sessionToken = sessionTokens.getToken(counterIdMatch[1]);
    if (sessionToken) {
      return next(req.clone({ setHeaders: { 'X-Session-Token': sessionToken } }));
    }
  }

  return next(req);
};
