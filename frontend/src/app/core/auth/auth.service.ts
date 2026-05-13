import { Injectable, computed, inject, signal } from '@angular/core';
import { AuthConfig, OAuthService } from 'angular-oauth2-oidc';
import { environment } from '../../../environments/environment';
import { GlobalRole, User } from '../../shared/models/user.model';

const authConfig: AuthConfig = {
  issuer: environment.oidc.issuer,
  clientId: environment.oidc.clientId,
  scope: environment.oidc.scope,
  redirectUri: environment.oidc.redirectUri,
  postLogoutRedirectUri: environment.oidc.postLogoutRedirectUri,
  responseType: 'code',
  showDebugInformation: !environment.production,
  // Authentik's OIDC endpoints (authorize, token, jwks, etc.) live at
  // /application/o/<action>/ while the issuer is /application/o/<slug>/.
  // The endpoint URLs therefore don't share the issuer's path prefix, which
  // makes the library's default strict discovery check reject the document.
  strictDiscoveryDocumentValidation: false,
};

const ROLE_HIERARCHY: readonly GlobalRole[] = ['user', 'admin', 'super_admin'];

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly oauthService = inject(OAuthService);

  private readonly _user = signal<User | null>(null);
  private readonly _isInitialized = signal(false);

  readonly user = this._user.asReadonly();
  readonly isAuthenticated = computed(() => this._user() !== null);
  readonly isInitialized = this._isInitialized.asReadonly();

  async initialize(): Promise<void> {
    this.oauthService.configure(authConfig);
    this.oauthService.setupAutomaticSilentRefresh();

    this.oauthService.events.subscribe(() => this.refreshUserFromClaims());

    try {
      await this.oauthService.loadDiscoveryDocumentAndTryLogin();
      this.refreshUserFromClaims();
    } finally {
      this._isInitialized.set(true);
    }
  }

  login(): void {
    this.oauthService.initCodeFlow();
  }

  logout(): void {
    this.oauthService.logOut();
  }

  getAccessToken(): string | null {
    return this.oauthService.getAccessToken() || null;
  }

  hasRole(role: GlobalRole): boolean {
    const user = this._user();
    if (!user) return false;
    return ROLE_HIERARCHY.indexOf(user.role) >= ROLE_HIERARCHY.indexOf(role);
  }

  private refreshUserFromClaims(): void {
    if (!this.oauthService.hasValidIdToken()) {
      this._user.set(null);
      return;
    }

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
