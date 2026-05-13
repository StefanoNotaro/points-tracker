import { Component, inject, input, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { CounterService } from '../../services/counter.service';
import { ShareTokenService } from '../../../../core/auth/share-token.service';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { NotificationService } from '../../../../core/services/notification.service';

@Component({
  selector: 'pts-join-counter',
  imports: [LoadingSpinnerComponent],
  template: `
    <div class="flex flex-col items-center justify-center min-h-[60vh] gap-4 text-center">
      @if (_error) {
        <div class="w-16 h-16 rounded-2xl bg-error/10 flex items-center justify-center">
          <span class="material-symbols-rounded text-4xl text-error">link_off</span>
        </div>
        <div>
          <h2 class="text-lg font-bold text-on-surface">Link invalid or expired</h2>
          <p class="text-on-surface-muted text-sm mt-1">{{ _error }}</p>
        </div>
      } @else {
        <pts-loading-spinner size="lg" />
        <p class="text-on-surface-muted text-sm font-medium">Joining counter…</p>
      }
    </div>
  `,
})
export class JoinCounterComponent implements OnInit {
  readonly token = input.required<string>();
  _error: string | null = null;

  private readonly counterService  = inject(CounterService);
  private readonly shareTokens     = inject(ShareTokenService);
  private readonly router          = inject(Router);
  private readonly notifications   = inject(NotificationService);

  async ngOnInit(): Promise<void> {
    try {
      const counter = await this.counterService.joinByShareToken(this.token());
      // Remember the share token so subsequent requests keep the same access level.
      this.shareTokens.setToken(counter.id, this.token());
      await this.router.navigate(['/counter', counter.id]);
    } catch {
      this._error = 'This share link is invalid or has expired.';
      this.notifications.error(this._error);
    }
  }
}
