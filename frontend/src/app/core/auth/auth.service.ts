import { Injectable, signal, computed, inject } from '@angular/core';
import { Router } from '@angular/router';
import { OAuthService, AuthConfig } from 'angular-oauth2-oidc';
import { environment } from '../../../environments/environment';
import { User, GlobalRole } from '../../shared/models/user.model';

const authConfig: AuthConfig = {
  issuer: environment.oidc.issuer,
  clientId: environment.oidc.clientId,
  scope: environment.oidc.scope,
  redirectUri: environment.oidc.redirectUri,
  postLogoutRedirectUri: environment.oidc.postLogoutRedirectUri,
  responseType: environment.oidc.responseType,
  useSilentRefresh: environment.oidc.useSilentRefresh,
  showDebugInformation: !environment.production,
  clearHashAfterLogin: true,
  nonceStateSeparator: 'semicolon',
  // Authentik's shared endpoints (authorize, token, userinfo) don't share
  // the per-application issuer path — disable strict URL prefix validation.
  strictDiscoveryDocumentValidation: false,
};

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly oauthService = inject(OAuthService);
  private readonly router = inject(Router);

  private readonly _user = signal<User | null>(null);
  private readonly _isInitialized = signal(false);

  readonly user = this._user.asReadonly();
  readonly isAuthenticated = computed(() => this._user() !== null);
  readonly isInitialized = this._isInitialized.asReadonly();

  async initialize(): Promise<void> {
    this.oauthService.configure(authConfig);
    this.oauthService.setupAutomaticSilentRefresh();

    try {
      // 1) Discovery only — DON'T try login yet, because token validation
      //    needs JWKS and we have to redirect that through the dev proxy first
      //    to avoid Authentik's cross-origin CORS error.
      await this.oauthService.loadDiscoveryDocument();

      if (!environment.production) {
        const doc = (this.oauthService as any).discoveryDoc;
        if (doc?.jwks_uri) {
          const url = new URL(doc.jwks_uri);
          // Same-origin relative URL — Angular dev proxy forwards /application/o/* to Authentik.
          doc.jwks_uri = url.pathname + url.search;
        }
        // The library also caches jwks_uri on the service itself in some versions.
        (this.oauthService as any).jwksUri = doc?.jwks_uri ?? (this.oauthService as any).jwksUri;
      }

      // 2) Now we can safely validate any token from the redirect.
      await this.oauthService.tryLogin();

      this.syncUserFromToken();
    } catch {
      // Authentik unreachable or not configured — continue as anonymous
    } finally {
      this._isInitialized.set(true);
    }
  }

  login(): void {
    this.oauthService.initCodeFlow();
  }

  logout(): void {
    this.oauthService.logOut();
    this._user.set(null);
  }

  getAccessToken(): string | null {
    const token = this.oauthService.getAccessToken();
    return token || null;
  }

  hasRole(role: GlobalRole): boolean {
    const user = this._user();
    if (!user) return false;

    const hierarchy: GlobalRole[] = ['user', 'admin', 'super_admin'];
    return hierarchy.indexOf(user.role) >= hierarchy.indexOf(role);
  }

  private syncUserFromToken(): void {
    const claims = this.oauthService.getIdentityClaims() as Record<string, unknown> | null;
    if (!claims) {
      this._user.set(null);
      return;
    }

    this._user.set({
      id: claims['sub'] as string,
      email: claims['email'] as string,
      displayName: (claims['name'] as string) ?? (claims['email'] as string),
      role: (claims['pts_role'] as GlobalRole) ?? 'user',
    });
  }
}
