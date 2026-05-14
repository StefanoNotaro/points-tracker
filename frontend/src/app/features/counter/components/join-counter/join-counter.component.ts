import { Component, inject, input, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { CounterService } from '../../services/counter.service';
import { ShareTokenService } from '../../../../core/auth/share-token.service';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { NotificationService } from '../../../../core/services/notification.service';

@Component({
  selector: 'pts-join-counter',
  imports: [LoadingSpinnerComponent, TranslatePipe],
  templateUrl: './join-counter.component.html',
})
export class JoinCounterComponent implements OnInit {
  readonly token = input.required<string>();
  _error: string | null = null;

  private readonly counterService  = inject(CounterService);
  private readonly shareTokens     = inject(ShareTokenService);
  private readonly router          = inject(Router);
  private readonly notifications   = inject(NotificationService);
  private readonly i18n            = inject(TranslateService);

  async ngOnInit(): Promise<void> {
    try {
      const counter = await this.counterService.joinByShareToken(this.token());
      this.shareTokens.setToken(counter.id, this.token());
      await this.router.navigate(['/counter', counter.id]);
    } catch {
      const msg = this.i18n.instant('counter.join.invalidMessage');
      this._error = msg;
      this.notifications.error(msg);
    }
  }
}
