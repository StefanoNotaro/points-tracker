import { Component, computed, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { AuthService } from '../../core/auth/auth.service';
import { CounterService } from '../counter/services/counter.service';
import { CounterHubService } from '../counter/services/counter-hub.service';
import { TournamentService } from '../tournament/services/tournament.service';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner.component';
import { Counter, CounterSummary } from '../../shared/models/counter.model';
import { TournamentSummary } from '../../shared/models/tournament.model';
import { SPORT_CONFIGS } from '../../shared/models/sport.model';
import { NotificationService } from '../../core/services/notification.service';

@Component({
  selector: 'pts-dashboard',
  imports: [RouterLink, LoadingSpinnerComponent, DatePipe, TranslatePipe],
  templateUrl: './dashboard.component.html',
})
export class DashboardComponent implements OnInit, OnDestroy {
  readonly auth                      = inject(AuthService);
  private readonly counterService    = inject(CounterService);
  private readonly tournamentService = inject(TournamentService);
  private readonly hub               = inject(CounterHubService);
  private readonly notifications     = inject(NotificationService);
  private readonly i18n              = inject(TranslateService);

  readonly loading            = signal(true);
  readonly loadingTournaments = signal(true);
  readonly counters           = signal<CounterSummary[]>([]);
  readonly tournaments        = signal<TournamentSummary[]>([]);

  readonly activeTournaments = computed(() =>
    this.tournaments().filter((t) => t.status === 'active' || t.status === 'draft' || t.status === 'registration'),
  );

  private subscribedToUser = false;
  private readonly subs: Subscription[] = [];

  readonly active = computed(() =>
    this.counters().filter((c) => c.status === 'active'),
  );

  readonly recent = computed(() =>
    this.counters()
      .filter((c) => c.status !== 'active')
      .slice(0, 3),
  );

  readonly stats = computed(() => {
    const list = this.counters();
    return {
      total:    list.length,
      active:   list.filter((c) => c.status === 'active').length,
      finished: list.filter((c) => c.status === 'finished').length,
    };
  });

  async ngOnInit(): Promise<void> {
    try {
      const list = await this.counterService.listMine();
      list.sort((a, b) => +new Date(b.updatedAt) - +new Date(a.updatedAt));
      this.counters.set(list);
    } catch {
      this.notifications.error(this.i18n.instant('dashboard.loadCountersError'));
    } finally {
      this.loading.set(false);
    }

    try {
      const ts = await this.tournamentService.listMine();
      ts.sort((a, b) => +new Date(b.updatedAt) - +new Date(a.updatedAt));
      this.tournaments.set(ts);
    } catch {
      // Tournament list is best-effort; don't block the dashboard.
    } finally {
      this.loadingTournaments.set(false);
    }

    if (!this.auth.isAuthenticated()) return;
    this.subs.push(
      this.hub.scoreUpdated$.subscribe((c) => this.applyUpdate(c)),
      this.hub.counterDeleted$.subscribe((id) => this.removeCounter(id)),
    );
    try {
      await this.hub.joinUser();
      this.subscribedToUser = true;
    } catch (err) {
      console.warn('Live dashboard updates unavailable:', err);
    }
  }

  async ngOnDestroy(): Promise<void> {
    for (const s of this.subs) s.unsubscribe();
    if (this.subscribedToUser) {
      try { await this.hub.leaveUser(); } catch { /* ignore */ }
    }
  }

  private applyUpdate(updated: Counter): void {
    this.counters.update((list) => {
      const idx = list.findIndex((c) => c.id === updated.id);
      const summary: CounterSummary = {
        id:            updated.id,
        sportType:     updated.sportType,
        teamAName:     updated.teamAName,
        teamBName:     updated.teamBName,
        status:        updated.status,
        setsWonA:      updated.setsWonA,
        setsWonB:      updated.setsWonB,
        currentScoreA: updated.currentScoreA,
        currentScoreB: updated.currentScoreB,
        createdAt:     updated.createdAt,
        updatedAt:     updated.updatedAt,
      };
      const next = idx === -1 ? [summary, ...list] : list.map((c, i) => (i === idx ? summary : c));
      next.sort((a, b) => +new Date(b.updatedAt) - +new Date(a.updatedAt));
      return next;
    });
  }

  private removeCounter(id: string): void {
    this.counters.update((list) => list.filter((c) => c.id !== id));
  }

  iconFor(sport: string): string {
    return SPORT_CONFIGS[sport as keyof typeof SPORT_CONFIGS]?.icon ?? 'sports_score';
  }
}
