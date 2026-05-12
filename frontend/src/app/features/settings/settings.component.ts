import { Component, inject } from '@angular/core';
import { AuthService } from '../../core/auth/auth.service';
import { ThemeService } from '../../core/services/theme.service';

@Component({
  selector: 'pts-settings',
  template: `
    <div class="max-w-md mx-auto py-8">
      <h1 class="text-2xl font-bold text-on-surface mb-6">Settings</h1>

      <div class="pts-card flex flex-col gap-6">
        <section>
          <h2 class="text-sm font-semibold text-on-surface-muted uppercase tracking-wide mb-3">Account</h2>
          <p class="text-on-surface font-medium">{{ auth.user()?.displayName }}</p>
          <p class="text-on-surface-muted text-sm">{{ auth.user()?.email }}</p>
        </section>

        <section>
          <h2 class="text-sm font-semibold text-on-surface-muted uppercase tracking-wide mb-3">Appearance</h2>
          <button class="pts-btn-secondary" (click)="theme.toggle()">
            Toggle {{ theme.isDark() ? 'light' : 'dark' }} mode
          </button>
        </section>

        <section>
          <button class="pts-btn-secondary text-error border-error hover:bg-error/10" (click)="auth.logout()">
            Sign out
          </button>
        </section>
      </div>
    </div>
  `,
})
export class SettingsComponent {
  readonly auth = inject(AuthService);
  readonly theme = inject(ThemeService);
}
