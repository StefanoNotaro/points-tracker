import { Component, inject } from '@angular/core';
import { AuthService } from '../../core/auth/auth.service';
import { ThemeService } from '../../core/services/theme.service';

@Component({
  selector: 'pts-settings',
  template: `
    <div class="flex flex-col gap-5 pb-8">

      <!-- Page header -->
      <div class="pt-2">
        <h1 class="text-2xl font-bold text-on-surface">Settings</h1>
        <p class="text-on-surface-muted text-sm mt-1">Manage your account and preferences.</p>
      </div>

      <!-- Account card -->
      <div class="pts-card flex flex-col gap-4">
        <p class="pts-label">Account</p>
        <div class="flex items-center gap-3">
          <div class="w-12 h-12 rounded-2xl bg-primary/10 flex items-center justify-center flex-shrink-0">
            <span class="material-symbols-rounded text-2xl text-primary">account_circle</span>
          </div>
          <div class="min-w-0">
            <p class="font-semibold text-on-surface truncate">{{ auth.user()?.displayName }}</p>
            <p class="text-sm text-on-surface-muted truncate">{{ auth.user()?.email }}</p>
          </div>
        </div>
      </div>

      <!-- Appearance card -->
      <div class="pts-card flex flex-col gap-4">
        <p class="pts-label">Appearance</p>
        <div class="flex items-center justify-between gap-4">
          <div class="flex items-center gap-3">
            <span class="material-symbols-rounded text-on-surface-muted">
              {{ theme.isDark() ? 'dark_mode' : 'light_mode' }}
            </span>
            <div>
              <p class="text-sm font-medium text-on-surface">Theme</p>
              <p class="text-xs text-on-surface-muted">{{ theme.isDark() ? 'Dark' : 'Light' }} mode active</p>
            </div>
          </div>
          <button
            class="relative inline-flex h-7 w-12 items-center rounded-full transition-colors duration-200
                   focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
            [class]="theme.isDark() ? 'bg-primary' : 'bg-border'"
            (click)="theme.toggle()"
            role="switch"
            [attr.aria-checked]="theme.isDark()"
            aria-label="Toggle dark mode"
          >
            <span
              class="inline-block h-5 w-5 rounded-full bg-white shadow-elevated transition-transform duration-200"
              [class]="theme.isDark() ? 'translate-x-6' : 'translate-x-1'"
            ></span>
          </button>
        </div>
      </div>

      <!-- Danger zone -->
      <div class="pts-card flex flex-col gap-4">
        <p class="pts-label">Session</p>
        <button
          class="flex items-center gap-3 text-error hover:bg-error/8 rounded-xl p-3 -mx-1
                 transition-colors text-sm font-medium w-full text-left"
          (click)="auth.logout()"
        >
          <span class="material-symbols-rounded text-xl">logout</span>
          Sign out
        </button>
      </div>

    </div>
  `,
})
export class SettingsComponent {
  readonly auth  = inject(AuthService);
  readonly theme = inject(ThemeService);
}
