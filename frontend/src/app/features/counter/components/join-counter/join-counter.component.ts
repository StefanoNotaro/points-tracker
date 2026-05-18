import { Component, inject, input, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { CounterService } from '../../services/counter.service';
import { TournamentService } from '../../../tournament/services/tournament.service';
import { ShareTokenService } from '../../../../core/auth/share-token.service';
import { ScorerTokenService } from '../../../../core/auth/scorer-token.service';
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

  private readonly counterService     = inject(CounterService);
  private readonly tournamentService  = inject(TournamentService);
  private readonly shareTokens        = inject(ShareTokenService);
  private readonly scorerTokens       = inject(ScorerTokenService);
  private readonly router             = inject(Router);
  private readonly notifications      = inject(NotificationService);
  private readonly i18n               = inject(TranslateService);

  async ngOnInit(): Promise<void> {
    // Try share token first.
    try {
      const counter = await this.counterService.joinByShareToken(this.token());
      this.shareTokens.setToken(counter.id, this.token());
      await this.router.navigate(['/counter', counter.id]);
      return;
    } catch { /* fall through to scorer link resolve */ }

    // Try resolving as a scorer/referee link.
    try {
      const dto = await this.tournamentService.resolveMatchScorerLink(this.token());
      let counterId = dto.counterId;
      if (!counterId) {
        // Match counter not opened yet — open it now using the scorer token.
        const counter = await this.tournamentService.openMatchCounter(
          dto.tournamentId,
          dto.matchId,
          this.token(),
        );
        counterId = counter.id;
      }
      this.scorerTokens.setToken(counterId, this.token());
      await this.router.navigate(['/counter', counterId]);
      return;
    } catch { /* fall through to error */ }

    const msg = this.i18n.instant('counter.join.invalidMessage');
    this._error = msg;
    this.notifications.error(msg);
  }
}
