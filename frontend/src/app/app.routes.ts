import { Routes } from '@angular/router';
import { ShellComponent } from './layout/shell/shell.component';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  {
    path: '',
    component: ShellComponent,
    children: [
      {
        path: '',
        loadChildren: () =>
          import('./features/counter/counter.routes').then((m) => m.COUNTER_ROUTES),
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
