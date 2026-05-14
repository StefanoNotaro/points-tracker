import { Component, inject, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatDialog } from '@angular/material/dialog';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { TournamentService } from '../../services/tournament.service';
import { TournamentSummary } from '../../../../shared/models/tournament.model';
import { SPORT_CONFIGS } from '../../../../shared/models/sport.model';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { NotificationService } from '../../../../core/services/notification.service';
import { AuthService } from '../../../../core/auth/auth.service';
import {
  ConfirmDialogComponent,
  ConfirmDialogData,
} from '../../../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'pts-my-tournaments',
  imports: [RouterLink, LoadingSpinnerComponent, DatePipe, TranslatePipe],
  templateUrl: './my-tournaments.component.html',
})
export class MyTournamentsComponent implements OnInit {
  private readonly service = inject(TournamentService);
  private readonly notifications = inject(NotificationService);
  private readonly dialog = inject(MatDialog);
  private readonly auth = inject(AuthService);
  private readonly i18n = inject(TranslateService);

  readonly loading = signal(true);
  readonly items = signal<TournamentSummary[]>([]);
  readonly isAnonymous = signal(false);

  async ngOnInit(): Promise<void> {
    const anonymous = !this.auth.isAuthenticated();
    this.isAnonymous.set(anonymous);
    try {
      const list = anonymous
        ? await this.service.listMineAnonymous()
        : await this.service.listMine();
      this.items.set(list);
    } catch {
      this.notifications.error(this.i18n.instant('tournament.list.loadError'));
    } finally {
      this.loading.set(false);
    }
  }

  sportIcon(s: string): string {
    return SPORT_CONFIGS[s as keyof typeof SPORT_CONFIGS]?.icon ?? 'emoji_events';
  }

  async confirmDelete(t: TournamentSummary): Promise<void> {
    const ok = await this.dialog.open<ConfirmDialogComponent, ConfirmDialogData, boolean>(
      ConfirmDialogComponent,
      {
        data: {
          title: this.i18n.instant('tournament.list.deleteConfirm.title'),
          message: this.i18n.instant('tournament.list.deleteConfirm.message', { name: t.name }),
          confirmLabel: this.i18n.instant('tournament.list.deleteConfirm.confirm'),
        },
      },
    ).afterClosed().toPromise();
    if (!ok) return;
    try {
      await this.service.delete(t.id);
      this.items.update((l) => l.filter((x) => x.id !== t.id));
      this.notifications.success(this.i18n.instant('tournament.list.deleteSuccess'));
    } catch {
      this.notifications.error(this.i18n.instant('tournament.list.deleteError'));
    }
  }
}
