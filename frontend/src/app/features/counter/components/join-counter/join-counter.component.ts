import { Component, inject, input, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { CounterService } from '../../services/counter.service';
import { SessionTokenService } from '../../../../core/auth/session-token.service';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { NotificationService } from '../../../../core/services/notification.service';

@Component({
  selector: 'pts-join-counter',
  imports: [LoadingSpinnerComponent],
  template: `
    <div class="flex flex-col items-center justify-center min-h-[60vh] gap-4">
      @if (error()) {
        <span class="material-symbols-rounded text-6xl text-error">link_off</span>
        <p class="text-on-surface-muted">{{ error() }}</p>
      } @else {
        <pts-loading-spinner size="lg" />
        <p class="text-on-surface-muted">Joining counter…</p>
      }
    </div>
  `,
})
export class JoinCounterComponent implements OnInit {
  readonly token = input.required<string>();
  error = () => this._error;
  private _error: string | null = null;

  private readonly counterService = inject(CounterService);
  private readonly sessionTokens = inject(SessionTokenService);
  private readonly router = inject(Router);
  private readonly notifications = inject(NotificationService);

  async ngOnInit(): Promise<void> {
    try {
      const counter = await this.counterService.joinByShareToken(this.token());
      await this.router.navigate(['/counter', counter.id]);
    } catch {
      this._error = 'This share link is invalid or has expired.';
      this.notifications.error(this._error);
    }
  }
}
