import { Component, inject, input, OnInit, signal } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { TournamentService } from '../../services/tournament.service';
import { NotificationService } from '../../../../core/services/notification.service';
import { IssuedMatchScorerLink, MatchScorerLink } from '../../../../shared/models/tournament.model';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';

@Component({
  selector: 'pts-scorer-link-panel',
  imports: [TranslatePipe, LoadingSpinnerComponent],
  templateUrl: './scorer-link-panel.component.html',
})
export class ScorerLinkPanelComponent implements OnInit {
  readonly tournamentId = input.required<string>();
  readonly matchId = input.required<string>();

  private readonly service = inject(TournamentService);
  private readonly notifications = inject(NotificationService);
  private readonly i18n = inject(TranslateService);

  readonly links = signal<MatchScorerLink[]>([]);
  readonly loading = signal(true);
  readonly issuing = signal(false);
  readonly newToken = signal<string | null>(null);
  readonly copied = signal(false);
  readonly labelInput = signal('');

  async ngOnInit(): Promise<void> {
    await this.loadLinks();
  }

  async loadLinks(): Promise<void> {
    this.loading.set(true);
    try {
      const all = await this.service.listMatchScorerLinks(this.tournamentId(), this.matchId());
      this.links.set(all.filter((l) => l.isActive));
    } catch {
      this.notifications.error(this.i18n.instant('tournament.scorerLink.loadError'));
    } finally {
      this.loading.set(false);
    }
  }

  async issue(): Promise<void> {
    this.issuing.set(true);
    this.newToken.set(null);
    try {
      const issued: IssuedMatchScorerLink = await this.service.issueMatchScorerLink(
        this.tournamentId(),
        this.matchId(),
        this.labelInput().trim() || null,
      );
      this.newToken.set(this.buildScorerUrl(issued.token));
      this.labelInput.set('');
      await this.loadLinks();
    } catch {
      this.notifications.error(this.i18n.instant('tournament.scorerLink.issueError'));
    } finally {
      this.issuing.set(false);
    }
  }

  async revoke(linkId: string): Promise<void> {
    try {
      await this.service.revokeMatchScorerLink(this.tournamentId(), linkId);
      this.links.update((ls) => ls.filter((l) => l.id !== linkId));
    } catch {
      this.notifications.error(this.i18n.instant('tournament.scorerLink.revokeError'));
    }
  }

  copyToken(): void {
    const url = this.newToken();
    if (!url) return;
    void navigator.clipboard.writeText(url).then(() => {
      this.copied.set(true);
      setTimeout(() => this.copied.set(false), 2000);
    });
  }

  dismissToken(): void {
    this.newToken.set(null);
  }

  private buildScorerUrl(token: string): string {
    return `${window.location.origin}/counter/join/${encodeURIComponent(token)}`;
  }
}
