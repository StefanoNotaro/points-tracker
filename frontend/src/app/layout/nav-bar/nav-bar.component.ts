import { Component, computed, inject, signal } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterLinkActive } from '@angular/router';
import { filter } from 'rxjs';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '../../core/auth/auth.service';
import { ThemeService } from '../../core/services/theme.service';

interface DrawerItem {
  labelKey: string;
  icon: string;
  routerLink: string;
  authedOnly?: boolean;
  adminOnly?: boolean;
}

@Component({
  selector: 'pts-nav-bar',
  imports: [RouterLink, RouterLinkActive, TranslatePipe],
  styleUrl: './nav-bar.component.css',
  templateUrl: './nav-bar.component.html',
})
export class NavBarComponent {
  readonly auth = inject(AuthService);
  readonly theme = inject(ThemeService);
  private readonly router = inject(Router);

  readonly drawerOpen = signal(false);

  private readonly items: DrawerItem[] = [
    { labelKey: 'nav.items.home',         icon: 'home',            routerLink: '/' },
    { labelKey: 'nav.items.newCounter',   icon: 'add_circle',      routerLink: '/new-counter' },
    { labelKey: 'nav.items.dashboard',    icon: 'space_dashboard', routerLink: '/dashboard', authedOnly: true },
    { labelKey: 'nav.items.counters',     icon: 'list_alt',        routerLink: '/my-counters', authedOnly: true },
    { labelKey: 'nav.items.tournaments',  icon: 'emoji_events',    routerLink: '/tournaments' },
    { labelKey: 'nav.items.settings',     icon: 'settings',        routerLink: '/settings', authedOnly: true },
    { labelKey: 'nav.items.adminCleanup', icon: 'cleaning_services', routerLink: '/admin/cleanup', authedOnly: true, adminOnly: true },
  ];

  readonly visibleItems = computed(() =>
    this.items.filter((i) => {
      if (i.authedOnly && !this.auth.isAuthenticated()) return false;
      if (i.adminOnly && !this.auth.hasRole('admin')) return false;
      return true;
    }),
  );

  constructor() {
    this.router.events
      .pipe(filter((e) => e instanceof NavigationEnd))
      .subscribe(() => this.drawerOpen.set(false));
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
