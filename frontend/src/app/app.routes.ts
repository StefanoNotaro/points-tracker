import { Routes } from '@angular/router';
import { ShellComponent } from './layout/shell/shell.component';
import { authGuard } from './core/guards/auth.guard';
import { rootRedirectGuard } from './core/guards/root-redirect.guard';

export const routes: Routes = [
  {
    path: '',
    component: ShellComponent,
    children: [
      // Root: smart redirect — authed users go to the dashboard, anonymous
      // users go to the create/resume flow.
      {
        path: '',
        pathMatch: 'full',
        canMatch: [rootRedirectGuard],
        children: [],
      },
      {
        path: 'dashboard',
        canActivate: [authGuard],
        loadComponent: () =>
          import('./features/dashboard/dashboard.component').then((m) => m.DashboardComponent),
      },
      {
        path: 'new-counter',
        loadComponent: () =>
          import('./features/counter/components/create-counter/create-counter.component').then(
            (m) => m.CreateCounterComponent,
          ),
      },
      {
        path: 'my-counters',
        canActivate: [authGuard],
        loadComponent: () =>
          import('./features/counter/components/my-counters/my-counters.component').then(
            (m) => m.MyCountersComponent,
          ),
      },
      {
        path: 'counter/join/:token',
        loadComponent: () =>
          import('./features/counter/components/join-counter/join-counter.component').then(
            (m) => m.JoinCounterComponent,
          ),
      },
      {
        path: 'counter/:id',
        loadComponent: () =>
          import('./features/counter/components/counter-page/counter-page.component').then(
            (m) => m.CounterPageComponent,
          ),
      },
      {
        path: 'tournaments',
        loadComponent: () =>
          import('./features/tournament/components/my-tournaments/my-tournaments.component').then(
            (m) => m.MyTournamentsComponent,
          ),
      },
      {
        path: 'tournaments/new',
        loadComponent: () =>
          import(
            './features/tournament/components/create-tournament/create-tournament.component'
          ).then((m) => m.CreateTournamentComponent),
      },
      {
        path: 'tournaments/:id',
        loadComponent: () =>
          import(
            './features/tournament/components/tournament-detail/tournament-detail.component'
          ).then((m) => m.TournamentDetailComponent),
      },
      {
        path: 'settings',
        canActivate: [authGuard],
        loadComponent: () =>
          import('./features/settings/settings.component').then((m) => m.SettingsComponent),
      },
      {
        path: '403',
        loadComponent: () =>
          import('./shared/components/access-denied/access-denied.component').then(
            (m) => m.AccessDeniedComponent,
          ),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
