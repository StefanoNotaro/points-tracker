import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatDialog } from '@angular/material/dialog';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { CounterService } from '../../services/counter.service';
import { NotificationService } from '../../../../core/services/notification.service';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import {
  ConfirmDialogComponent,
  ConfirmDialogData,
} from '../../../../shared/components/confirm-dialog/confirm-dialog.component';
import { CounterSummary } from '../../../../shared/models/counter.model';
import { SPORT_CONFIGS } from '../../../../shared/models/sport.model';

@Component({
  selector: 'pts-my-counters',
  imports: [RouterLink, LoadingSpinnerComponent, TranslatePipe],
  templateUrl: './my-counters.component.html',
})
export class MyCountersComponent implements OnInit {
  private readonly counterService = inject(CounterService);
  private readonly notifications  = inject(NotificationService);
  private readonly dialog         = inject(MatDialog);
  private readonly i18n           = inject(TranslateService);

  readonly loading  = signal(true);
  readonly counters = signal<CounterSummary[]>([]);

  async ngOnInit(): Promise<void> {
    await this.refresh();
  }

  async refresh(): Promise<void> {
    this.loading.set(true);
    try {
      this.counters.set(await this.counterService.listMine());
    } catch {
      this.notifications.error(this.i18n.instant('counter.list.loadError'));
    } finally {
      this.loading.set(false);
    }
  }

  iconFor(sport: string): string {
    return SPORT_CONFIGS[sport as keyof typeof SPORT_CONFIGS]?.icon ?? 'sports_score';
  }

  async confirmDelete(c: CounterSummary): Promise<void> {
    const confirmed = await this.dialog
      .open<ConfirmDialogComponent, ConfirmDialogData, boolean>(ConfirmDialogComponent, {
        data: {
          title: this.i18n.instant('counter.list.deleteTitle'),
          message: this.i18n.instant('counter.list.deleteMessage', { a: c.teamAName, b: c.teamBName }),
          confirmLabel: this.i18n.instant('common.delete'),
        },
      })
      .afterClosed()
      .toPromise();

    if (!confirmed) return;

    try {
      await this.counterService.delete(c.id);
      this.counters.update((list) => list.filter((x) => x.id !== c.id));
      this.notifications.success(this.i18n.instant('counter.list.deleteSuccess'));
    } catch {
      this.notifications.error(this.i18n.instant('counter.list.deleteError'));
    }
  }
}
