import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../auth/auth.service';
import { NotificationService } from '../services/notification.service';

let lastRateLimitToastAt = 0;
const RATE_LIMIT_TOAST_COOLDOWN_MS = 3000;

// Guards against multiple concurrent 401s racing into parallel initCodeFlow()
// redirects. Reset on the next navigation when a fresh interceptor instance runs.
let loginRedirectInFlight = false;

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const auth = inject(AuthService);
  const notifications = inject(NotificationService);
  const i18n = inject(TranslateService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      switch (error.status) {
        case 401:
          // Anonymous flows (share-token reads, public counter joins) can
          // legitimately receive 401 — the caller surfaces the failure in its
          // own UI. Only force re-auth when the user actually had a session.
          if (auth.isAuthenticated() && !loginRedirectInFlight) {
            loginRedirectInFlight = true;
            notifications.warning(i18n.instant('apiErrors.sessionExpired'));
            auth.login();
          }
          break;
        case 403:
          router.navigate(['/403']);
          break;
        case 404:
          break;
        case 429: {
          const now = Date.now();
          if (now - lastRateLimitToastAt >= RATE_LIMIT_TOAST_COOLDOWN_MS) {
            lastRateLimitToastAt = now;
            const retryAfter = parseRetryAfterSeconds(error);
            if (retryAfter !== null) {
              notifications.warning(i18n.instant('apiErrors.rateLimitedRetry', { seconds: retryAfter }));
            } else {
              notifications.warning(i18n.instant('apiErrors.rateLimited'));
            }
          }
          break;
        }
        default:
          notifications.error(i18n.instant('apiErrors.generic'));
          break;
      }
      return throwError(() => error);
    }),
  );
};

function parseRetryAfterSeconds(error: HttpErrorResponse): number | null {
  const raw = error.headers.get('Retry-After');
  if (!raw) return null;

  const asInt = Number.parseInt(raw, 10);
  if (Number.isFinite(asInt) && asInt >= 0) return asInt;

  const asDate = Date.parse(raw);
  if (Number.isNaN(asDate)) return null;

  const diffMs = asDate - Date.now();
  if (diffMs <= 0) return 0;
  return Math.ceil(diffMs / 1000);
}

