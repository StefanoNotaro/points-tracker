import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { MatMenuModule } from '@angular/material/menu';
import { MatButtonModule } from '@angular/material/button';
import { AuthService } from '../../core/auth/auth.service';
import { ThemeService } from '../../core/services/theme.service';

@Component({
  selector: 'pts-nav-bar',
  imports: [RouterLink, MatMenuModule, MatButtonModule],
  styles: [`
    :host { display: contents; }
    header { backdrop-filter: blur(12px); -webkit-backdrop-filter: blur(12px); }
  `],
  template: `
    <header class="sticky top-0 z-40 border-b border-border pt-[env(safe-area-inset-top)]"
            style="background-color: color-mix(in oklch, var(--color-surface) 85%, transparent)">
      <div class="max-w-2xl mx-auto px-3 sm:px-4 h-14 flex items-center justify-between gap-2">

        <a routerLink="/" class="flex items-center gap-2 font-bold text-primary text-base select-none min-w-0">
          <span class="material-symbols-rounded text-2xl shrink-0">scoreboard</span>
          <span class="hidden sm:inline truncate">Points Tracker</span>
        </a>

        <nav class="flex items-center gap-1">
          <button
            type="button"
            class="pts-btn-icon"
            (click)="theme.toggle()"
            [attr.aria-label]="theme.isDark() ? 'Switch to light mode' : 'Switch to dark mode'"
          >
            <span class="material-symbols-rounded text-xl">
              {{ theme.isDark() ? 'light_mode' : 'dark_mode' }}
            </span>
          </button>

          @if (auth.isAuthenticated()) {
            <a
              routerLink="/dashboard"
              class="pts-btn-icon"
              aria-label="Dashboard"
              title="Dashboard"
            >
              <span class="material-symbols-rounded text-xl">space_dashboard</span>
            </a>
            <a
              routerLink="/my-counters"
              class="pts-btn-icon"
              aria-label="My counters"
              title="My counters"
            >
              <span class="material-symbols-rounded text-xl">list_alt</span>
            </a>

            <button
              type="button"
              class="pts-btn-icon"
              [matMenuTriggerFor]="userMenu"
              aria-label="User menu"
            >
              <span class="material-symbols-rounded text-2xl">account_circle</span>
            </button>

            <mat-menu #userMenu="matMenu">
              <div class="px-4 py-3 border-b border-border min-w-48">
                <p class="text-sm font-semibold text-on-surface">{{ auth.user()?.displayName }}</p>
                <p class="text-xs text-on-surface-muted mt-0.5">{{ auth.user()?.email }}</p>
              </div>
              <button mat-menu-item routerLink="/dashboard">
                <span class="material-symbols-rounded mr-2 text-base align-middle">space_dashboard</span>
                Dashboard
              </button>
              <button mat-menu-item routerLink="/my-counters">
                <span class="material-symbols-rounded mr-2 text-base align-middle">list_alt</span>
                My Counters
              </button>
              <button mat-menu-item routerLink="/settings">
                <span class="material-symbols-rounded mr-2 text-base align-middle">settings</span>
                Settings
              </button>
              <button mat-menu-item (click)="auth.logout()">
                <span class="material-symbols-rounded mr-2 text-base align-middle">logout</span>
                Sign out
              </button>
            </mat-menu>
          } @else {
            <button class="pts-btn-primary text-sm py-1.5 px-4" (click)="auth.login()">
              Sign in
            </button>
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
