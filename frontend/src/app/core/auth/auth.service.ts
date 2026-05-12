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
      await this.oauthService.loadDiscoveryDocumentAndTryLogin();
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
