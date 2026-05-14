import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'pts-access-denied',
  imports: [RouterLink, TranslatePipe],
  template: `
    <div class="flex flex-col items-center justify-center min-h-[60vh] gap-6 text-center px-4">
      <span class="material-symbols-rounded text-8xl text-error">lock</span>
      <div>
        <h1 class="text-2xl font-bold text-on-surface mb-2">{{ 'accessDenied.title' | translate }}</h1>
        <p class="text-on-surface-muted">{{ 'accessDenied.message' | translate }}</p>
      </div>
      <a routerLink="/" class="pts-btn-primary">{{ 'common.goHome' | translate }}</a>
    </div>
  `,
})
export class AccessDeniedComponent {}
