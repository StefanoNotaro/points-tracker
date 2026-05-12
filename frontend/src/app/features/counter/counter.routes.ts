import { Routes } from '@angular/router';

export const COUNTER_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./components/create-counter/create-counter.component').then(
        (m) => m.CreateCounterComponent,
      ),
  },
  {
    path: 'counter/new',
    loadComponent: () =>
      import('./components/create-counter/create-counter.component').then(
        (m) => m.CreateCounterComponent,
      ),
  },
  {
    path: 'counter/:id',
    loadComponent: () =>
      import('./components/counter-page/counter-page.component').then(
        (m) => m.CounterPageComponent,
      ),
  },
  {
    path: 'counter/join/:token',
    loadComponent: () =>
      import('./components/join-counter/join-counter.component').then(
        (m) => m.JoinCounterComponent,
      ),
  },
];
