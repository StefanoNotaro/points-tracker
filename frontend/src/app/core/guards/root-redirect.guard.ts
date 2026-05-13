import { inject } from '@angular/core';
import { CanMatchFn, Router } from '@angular/router';
import { AuthService } from '../auth/auth.service';

/**
 * Decides where '/' should land:
 *  - authenticated users → /dashboard
 *  - anonymous users     → /new-counter (the existing create-or-resume page)
 *
 * Implemented as CanMatch so the redirect happens before any component loads,
 * which keeps the URL bar in sync and avoids a flash of the wrong page.
 */
export const rootRedirectGuard: CanMatchFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  return router.createUrlTree([auth.isAuthenticated() ? '/dashboard' : '/new-counter']);
};
