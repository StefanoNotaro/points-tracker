import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatDialog } from '@angular/material/dialog';
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
  imports: [RouterLink, LoadingSpinnerComponent],
  template: `
    <div class="flex flex-col gap-4 pb-8">

      <div class="flex items-center justify-between gap-2">
        <h1 class="text-xl sm:text-2xl font-bold text-on-surface">My Counters</h1>
        <a routerLink="/new-counter" class="pts-btn-primary" aria-label="New counter">
          <span class="material-symbols-rounded text-lg">add</span>
          <span class="hidden sm:inline">New</span>
        </a>
      </div>

      @if (loading()) {
        <div class="flex items-center justify-center py-20">
          <pts-loading-spinner size="lg" />
        </div>
      } @else if (counters().length === 0) {
        <div class="pts-card flex flex-col items-center text-center gap-3 py-10">
          <span class="material-symbols-rounded text-5xl text-on-surface-muted">scoreboard</span>
          <p class="text-on-surface font-semibold">No counters yet</p>
          <p class="text-sm text-on-surface-muted">Create your first counter to track a match.</p>
          <a routerLink="/new-counter" class="pts-btn-primary mt-2">
            <span class="material-symbols-rounded">add</span>
            <span>Start a counter</span>
          </a>
        </div>
      } @else {
        <ul class="flex flex-col gap-2">
          @for (c of counters(); track c.id) {
            <li class="pts-card !p-3 flex items-center gap-3">
              <span class="material-symbols-rounded text-2xl text-primary shrink-0">
                {{ iconFor(c.sportType) }}
              </span>
              <a
                [routerLink]="['/counter', c.id]"
                class="flex-1 min-w-0 active:opacity-70 transition-opacity"
              >
                <p class="font-semibold text-on-surface truncate">
                  {{ c.teamAName }} <span class="text-on-surface-muted font-normal">vs</span> {{ c.teamBName }}
                </p>
                <p class="text-xs text-on-surface-muted truncate flex items-center gap-1.5 mt-0.5">
                  <span class="font-mono font-medium text-on-surface">
                    {{ c.setsWonA }}–{{ c.setsWonB }}
                  </span>
                  @if (c.status === 'active') {
                    <span class="inline-flex items-center gap-1 text-success">
                      <span class="w-1.5 h-1.5 rounded-full bg-success"></span>Live
                    </span>
                  } @else {
                    <span class="inline-flex items-center gap-1">
                      <span class="material-symbols-rounded text-sm">flag</span>
                      {{ statusLabel(c.status) }}
                    </span>
                  }
                </p>
              </a>
              <button
                type="button"
                class="pts-btn-icon shrink-0"
                (click)="confirmDelete(c)"
                aria-label="Delete counter"
                title="Delete"
              >
                <span class="material-symbols-rounded text-on-surface-muted hover:text-error">
                  delete_outline
                </span>
              </button>
            </li>
          }
        </ul>
      }
    </div>
  `,
})
export class MyCountersComponent implements OnInit {
  private readonly counterService = inject(CounterService);
  private readonly notifications  = inject(NotificationService);
  private readonly dialog         = inject(MatDialog);

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
      this.notifications.error('Could not load your counters.');
    } finally {
      this.loading.set(false);
    }
  }

  iconFor(sport: string): string {
    return SPORT_CONFIGS[sport as keyof typeof SPORT_CONFIGS]?.icon ?? 'sports_score';
  }

  sportLabel(sport: string): string {
    return SPORT_CONFIGS[sport as keyof typeof SPORT_CONFIGS]?.label ?? sport;
  }

  statusLabel(status: string): string {
    return status === 'active' ? 'In progress'
         : status === 'finished' ? 'Finished'
         : 'Abandoned';
  }

  async confirmDelete(c: CounterSummary): Promise<void> {
    const confirmed = await this.dialog
      .open<ConfirmDialogComponent, ConfirmDialogData, boolean>(ConfirmDialogComponent, {
        data: {
          title: 'Delete counter?',
          message: `"${c.teamAName} vs ${c.teamBName}" will be removed. This can't be undone from here.`,
          confirmLabel: 'Delete',
        },
      })
      .afterClosed()
      .toPromise();

    if (!confirmed) return;

    try {
      await this.counterService.delete(c.id);
      this.counters.update((list) => list.filter((x) => x.id !== c.id));
      this.notifications.success('Counter deleted.');
    } catch {
      this.notifications.error('Failed to delete counter.');
    }
  }
}
