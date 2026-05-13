import { Component, inject, signal } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterLinkActive } from '@angular/router';
import { filter } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { ThemeService } from '../../core/services/theme.service';

interface DrawerItem {
  label: string;
  icon: string;
  routerLink: string;
  authedOnly?: boolean;
}

@Component({
  selector: 'pts-nav-bar',
  imports: [RouterLink, RouterLinkActive],
  styles: [`
    :host { display: contents; }
    header { backdrop-filter: blur(12px); -webkit-backdrop-filter: blur(12px); }
    .drawer { transform: translateX(-100%); transition: transform 220ms ease; }
    .drawer.open { transform: translateX(0); }
    .scrim { opacity: 0; pointer-events: none; transition: opacity 220ms ease; }
    .scrim.open { opacity: 1; pointer-events: auto; }
  `],
  template: `
    <!-- Top bar -->
    <header class="sticky top-0 z-40 border-b border-border pt-[env(safe-area-inset-top)]"
            style="background-color: color-mix(in oklch, var(--color-surface) 85%, transparent)">
      <div class="max-w-2xl mx-auto px-3 sm:px-4 h-14 flex items-center justify-between gap-2">
        <div class="flex items-center gap-1 min-w-0">
          <button type="button" class="pts-btn-icon" aria-label="Open menu"
                  (click)="drawerOpen.set(true)">
            <span class="material-symbols-rounded text-2xl">menu</span>
          </button>
          <a routerLink="/" class="flex items-center gap-2 font-bold text-primary text-base select-none min-w-0">
            <span class="material-symbols-rounded text-2xl shrink-0">scoreboard</span>
            <span class="hidden sm:inline truncate">Points Tracker</span>
          </a>
        </div>

        <nav class="flex items-center gap-1">
          <button type="button" class="pts-btn-icon" (click)="theme.toggle()"
                  [attr.aria-label]="theme.isDark() ? 'Switch to light mode' : 'Switch to dark mode'">
            <span class="material-symbols-rounded text-xl">
              {{ theme.isDark() ? 'light_mode' : 'dark_mode' }}
            </span>
          </button>

          @if (auth.isAuthenticated()) {
            <button type="button" class="pts-btn-icon" (click)="drawerOpen.set(true)"
                    aria-label="Account">
              <span class="material-symbols-rounded text-2xl">account_circle</span>
            </button>
          } @else {
            <button type="button" class="pts-btn-primary text-sm py-1.5 px-4"
                    (click)="auth.login()">Sign in</button>
          }
        </nav>
      </div>
    </header>

    <!-- Scrim -->
    <div class="scrim fixed inset-0 z-40 bg-black/40"
         [class.open]="drawerOpen()"
         (click)="drawerOpen.set(false)"
         aria-hidden="true"></div>

    <!-- Slide-out drawer -->
    <aside class="drawer fixed top-0 left-0 z-50 h-[100dvh] w-72 max-w-[85vw]
                  border-r border-border bg-surface flex flex-col pt-[env(safe-area-inset-top)]
                  pb-[env(safe-area-inset-bottom)]"
           [class.open]="drawerOpen()"
           aria-label="Primary navigation">

      <div class="flex items-center justify-between px-4 h-14 border-b border-border">
        <a routerLink="/" (click)="drawerOpen.set(false)"
           class="flex items-center gap-2 font-bold text-primary text-base">
          <span class="material-symbols-rounded text-2xl">scoreboard</span>
          <span>Points Tracker</span>
        </a>
        <button type="button" class="pts-btn-icon" (click)="drawerOpen.set(false)" aria-label="Close menu">
          <span class="material-symbols-rounded text-xl">close</span>
        </button>
      </div>

      @if (auth.isAuthenticated()) {
        <div class="px-4 py-3 border-b border-border">
          <p class="text-sm font-semibold text-on-surface truncate">{{ auth.user()?.displayName }}</p>
          <p class="text-xs text-on-surface-muted truncate mt-0.5">{{ auth.user()?.email }}</p>
        </div>
      }

      <ul class="flex flex-col p-2 gap-0.5 flex-1 overflow-y-auto">
        @for (item of visibleItems(); track item.routerLink) {
          <li>
            <a [routerLink]="item.routerLink"
               routerLinkActive="bg-primary/10 text-primary"
               [routerLinkActiveOptions]="{ exact: item.routerLink === '/' }"
               (click)="drawerOpen.set(false)"
               class="flex items-center gap-3 px-3 py-3 rounded-lg text-sm font-medium
                      text-on-surface hover:bg-surface-variant transition-colors">
              <span class="material-symbols-rounded text-xl">{{ item.icon }}</span>
              <span>{{ item.label }}</span>
            </a>
          </li>
        }
      </ul>

      <div class="p-2 border-t border-border">
        @if (auth.isAuthenticated()) {
          <button type="button"
                  class="w-full flex items-center gap-3 px-3 py-3 rounded-lg text-sm font-medium
                         text-on-surface hover:bg-surface-variant transition-colors"
                  (click)="signOut()">
            <span class="material-symbols-rounded text-xl">logout</span>
            <span>Sign out</span>
          </button>
        } @else {
          <button type="button" class="pts-btn-primary w-full" (click)="signIn()">
            <span class="material-symbols-rounded text-lg">login</span>
            <span>Sign in</span>
          </button>
        }
      </div>
    </aside>
  `,
})
export class NavBarComponent {
  readonly auth = inject(AuthService);
  readonly theme = inject(ThemeService);
  private readonly router = inject(Router);

  readonly drawerOpen = signal(false);

  private readonly items: DrawerItem[] = [
    { label: 'Home',        icon: 'home',          routerLink: '/' },
    { label: 'New counter', icon: 'add_circle',    routerLink: '/new-counter' },
    { label: 'Dashboard',   icon: 'space_dashboard', routerLink: '/dashboard', authedOnly: true },
    { label: 'Counters',    icon: 'list_alt',      routerLink: '/my-counters', authedOnly: true },
    { label: 'Tournaments', icon: 'emoji_events',  routerLink: '/tournaments' },
    { label: 'Settings',    icon: 'settings',      routerLink: '/settings', authedOnly: true },
  ];

  constructor() {
    // Close the drawer on every successful navigation.
    this.router.events
      .pipe(filter((e) => e instanceof NavigationEnd))
      .subscribe(() => this.drawerOpen.set(false));
  }

  visibleItems(): DrawerItem[] {
    return this.items.filter((i) => !i.authedOnly || this.auth.isAuthenticated());
  }

  signIn(): void {
    this.drawerOpen.set(false);
    this.auth.login();
  }
  signOut(): void {
    this.drawerOpen.set(false);
    this.auth.logout();
  }
}
