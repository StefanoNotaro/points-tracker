import { Component, inject, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatDialog } from '@angular/material/dialog';
import { TournamentService } from '../../services/tournament.service';
import { TournamentSummary, TOURNAMENT_FORMATS } from '../../../../shared/models/tournament.model';
import { SPORT_CONFIGS } from '../../../../shared/models/sport.model';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { NotificationService } from '../../../../core/services/notification.service';
import {
  ConfirmDialogComponent,
  ConfirmDialogData,
} from '../../../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'pts-my-tournaments',
  imports: [RouterLink, LoadingSpinnerComponent, DatePipe],
  template: `
    <div class="flex flex-col gap-4 pb-8">
      <div class="flex items-center justify-between gap-2">
        <h1 class="text-xl sm:text-2xl font-bold text-on-surface">My Tournaments</h1>
        <a routerLink="/tournaments/new" class="pts-btn-primary" aria-label="New tournament">
          <span class="material-symbols-rounded text-lg">add</span>
          <span class="hidden sm:inline">New</span>
        </a>
      </div>

      @if (loading()) {
        <div class="flex items-center justify-center py-20"><pts-loading-spinner size="lg" /></div>
      } @else if (items().length === 0) {
        <div class="pts-card flex flex-col items-center text-center gap-3 py-10">
          <span class="material-symbols-rounded text-5xl text-on-surface-muted">emoji_events</span>
          <p class="text-on-surface font-semibold">No tournaments yet</p>
          <p class="text-sm text-on-surface-muted">Create your first tournament to bracket your teams.</p>
          <a routerLink="/tournaments/new" class="pts-btn-primary mt-2">
            <span class="material-symbols-rounded">add</span><span>Create tournament</span>
          </a>
        </div>
      } @else {
        <ul class="flex flex-col gap-2">
          @for (t of items(); track t.id) {
            <li class="pts-card !p-3 flex items-center gap-3">
              <span class="material-symbols-rounded text-2xl text-primary shrink-0">
                {{ sportIcon(t.sportType) }}
              </span>
              <a [routerLink]="['/tournaments', t.id]" class="flex-1 min-w-0 active:opacity-70">
                <p class="font-semibold text-on-surface truncate">{{ t.name }}</p>
                <p class="text-xs text-on-surface-muted truncate flex items-center gap-1.5 mt-0.5">
                  <span>{{ formatLabel(t.format) }}</span>
                  <span>·</span>
                  <span>{{ t.participantCount }} teams</span>
                  <span>·</span>
                  <span class="inline-flex items-center gap-1">
                    <span class="w-1.5 h-1.5 rounded-full"
                      [class.bg-success]="t.status === 'active'"
                      [class.bg-on-surface-muted]="t.status !== 'active'"></span>
                    {{ statusLabel(t.status) }}
                  </span>
                </p>
                <p class="text-[11px] text-on-surface-muted mt-0.5">
                  Updated {{ t.updatedAt | date: 'shortDate' }}
                </p>
              </a>
              <button type="button" class="pts-btn-icon shrink-0" (click)="confirmDelete(t)" aria-label="Delete">
                <span class="material-symbols-rounded text-on-surface-muted hover:text-error">delete_outline</span>
              </button>
            </li>
          }
        </ul>
      }
    </div>
  `,
})
export class MyTournamentsComponent implements OnInit {
  private readonly service = inject(TournamentService);
  private readonly notifications = inject(NotificationService);
  private readonly dialog = inject(MatDialog);

  readonly loading = signal(true);
  readonly items = signal<TournamentSummary[]>([]);

  async ngOnInit(): Promise<void> {
    try { this.items.set(await this.service.listMine()); }
    catch { this.notifications.error('Could not load your tournaments.'); }
    finally { this.loading.set(false); }
  }

  sportIcon(s: string): string {
    return SPORT_CONFIGS[s as keyof typeof SPORT_CONFIGS]?.icon ?? 'emoji_events';
  }
  formatLabel(f: string): string {
    return TOURNAMENT_FORMATS.find((x) => x.value === f)?.label ?? f;
  }
  statusLabel(s: string): string {
    return s.charAt(0).toUpperCase() + s.slice(1);
  }

  async confirmDelete(t: TournamentSummary): Promise<void> {
    const ok = await this.dialog.open<ConfirmDialogComponent, ConfirmDialogData, boolean>(
      ConfirmDialogComponent,
      { data: { title: 'Delete tournament?', message: `"${t.name}" will be removed.`, confirmLabel: 'Delete' } },
    ).afterClosed().toPromise();
    if (!ok) return;
    try {
      await this.service.delete(t.id);
      this.items.update((l) => l.filter((x) => x.id !== t.id));
      this.notifications.success('Tournament deleted.');
    } catch {
      this.notifications.error('Failed to delete tournament.');
    }
  }
}
