import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { MatMenuModule } from '@angular/material/menu';
import { MatButtonModule } from '@angular/material/button';
import { AuthService } from '../../core/auth/auth.service';
import { ThemeService } from '../../core/services/theme.service';

@Component({
  selector: 'pts-nav-bar',
  imports: [RouterLink, MatMenuModule, MatButtonModule],
  template: `
    <header class="sticky top-0 z-40 bg-surface border-b border-border">
      <div class="max-w-5xl mx-auto px-4 h-14 flex items-center justify-between gap-4">

        <a routerLink="/" class="flex items-center gap-2 font-bold text-primary text-lg">
          <span class="material-symbols-rounded">scoreboard</span>
          Points Tracker
        </a>

        <nav class="flex items-center gap-1">
          <!-- Theme toggle -->
          <button
            type="button"
            class="pts-btn-icon"
            (click)="theme.toggle()"
            [attr.aria-label]="theme.isDark() ? 'Switch to light mode' : 'Switch to dark mode'"
          >
            <span class="material-symbols-rounded">{{ theme.isDark() ? 'light_mode' : 'dark_mode' }}</span>
          </button>

          @if (auth.isAuthenticated()) {
            <button
              type="button"
              class="pts-btn-icon"
              [matMenuTriggerFor]="userMenu"
              aria-label="User menu"
            >
              <span class="material-symbols-rounded">account_circle</span>
            </button>

            <mat-menu #userMenu="matMenu">
              <div class="px-4 py-2 border-b border-border">
                <p class="text-sm font-medium text-on-surface">{{ auth.user()?.displayName }}</p>
                <p class="text-xs text-on-surface-muted">{{ auth.user()?.email }}</p>
              </div>
              <button mat-menu-item routerLink="/settings">
                <span class="material-symbols-rounded">settings</span>
                Settings
              </button>
              <button mat-menu-item (click)="auth.logout()">
                <span class="material-symbols-rounded">logout</span>
                Sign out
              </button>
            </mat-menu>
          } @else {
            <button class="pts-btn-primary" (click)="auth.login()">Sign in</button>
          }
        </nav>
      </div>
    </header>
  `,
})
export class NavBarComponent {
  readonly auth = inject(AuthService);
  readonly theme = inject(ThemeService);
}
