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
  template: `
    <div class="flex flex-col gap-5 pb-8">

      <header class="flex flex-col gap-1">
        <p class="text-xs uppercase tracking-wide text-on-surface-muted">{{ 'dashboard.kicker' | translate }}</p>
        <h1 class="text-2xl font-bold text-on-surface truncate">
          {{ auth.user()?.displayName }}
        </h1>
      </header>

      <div class="grid grid-cols-2 gap-3">
        <a routerLink="/new-counter"
           class="pts-card flex flex-col gap-1 hover:border-primary transition-colors">
          <span class="material-symbols-rounded text-primary text-2xl">add_circle</span>
          <p class="font-semibold text-on-surface text-sm">{{ 'dashboard.actions.newCounter' | translate }}</p>
          <p class="text-xs text-on-surface-muted">{{ 'dashboard.actions.newCounterHelp' | translate }}</p>
        </a>
        <a routerLink="/tournaments/new"
           class="pts-card flex flex-col gap-1 hover:border-primary transition-colors">
          <span class="material-symbols-rounded text-primary text-2xl">emoji_events</span>
          <p class="font-semibold text-on-surface text-sm">{{ 'dashboard.actions.newTournament' | translate }}</p>
          <p class="text-xs text-on-surface-muted">{{ 'dashboard.actions.newTournamentHelp' | translate }}</p>
        </a>
      </div>

      <div class="grid grid-cols-3 gap-3">
        <div class="pts-card !p-3 text-center">
          <p class="text-2xl font-bold text-on-surface font-mono">{{ stats().total }}</p>
          <p class="text-[11px] uppercase tracking-wide text-on-surface-muted mt-0.5">{{ 'dashboard.stats.total' | translate }}</p>
        </div>
        <div class="pts-card !p-3 text-center">
          <p class="text-2xl font-bold text-success font-mono">{{ stats().active }}</p>
          <p class="text-[11px] uppercase tracking-wide text-on-surface-muted mt-0.5">{{ 'dashboard.stats.active' | translate }}</p>
        </div>
        <div class="pts-card !p-3 text-center">
          <p class="text-2xl font-bold text-on-surface font-mono">{{ stats().finished }}</p>
          <p class="text-[11px] uppercase tracking-wide text-on-surface-muted mt-0.5">{{ 'dashboard.stats.finished' | translate }}</p>
        </div>
      </div>

      <section class="flex flex-col gap-2">
        <div class="flex items-center justify-between">
          <h2 class="pts-label">{{ 'dashboard.active' | translate }}</h2>
          @if (active().length > 0) {
            <a routerLink="/my-counters" class="text-xs text-primary hover:underline">
              {{ 'common.viewAll' | translate }}
            </a>
          }
        </div>

        @if (loading()) {
          <div class="flex items-center justify-center py-8">
            <pts-loading-spinner size="md" />
          </div>
        } @else if (active().length === 0) {
          <div class="pts-card flex flex-col items-center text-center gap-2 py-6">
            <span class="material-symbols-rounded text-3xl text-on-surface-muted">scoreboard</span>
            <p class="text-sm text-on-surface-muted">{{ 'dashboard.noActive' | translate }}</p>
            <a routerLink="/new-counter" class="pts-btn-primary mt-1">
              <span class="material-symbols-rounded text-lg">add</span>
              <span>{{ 'dashboard.startOne' | translate }}</span>
            </a>
          </div>
        } @else {
          <ul class="flex flex-col gap-2">
            @for (c of active(); track c.id) {
              <li>
                <a [routerLink]="['/counter', c.id]"
                   class="pts-card !p-3 flex items-center gap-3 hover:border-primary transition-colors">
                  <span class="material-symbols-rounded text-2xl text-primary shrink-0">
                    {{ iconFor(c.sportType) }}
                  </span>
                  <span class="flex-1 min-w-0">
                    <span class="block font-semibold text-on-surface truncate">
                      {{ c.teamAName }}
                      <span class="text-on-surface-muted font-normal">{{ 'dashboard.vs' | translate }}</span>
                      {{ c.teamBName }}
                    </span>
                    <span class="block text-xs text-on-surface-muted mt-0.5">
                      <span class="font-mono text-on-surface">{{ c.setsWonA }}–{{ c.setsWonB }}</span>
                      · {{ 'dashboard.currentScore' | translate: { a: c.currentScoreA, b: c.currentScoreB } }}
                    </span>
                  </span>
                  <span class="inline-flex items-center gap-1 text-success text-xs shrink-0">
                    <span class="w-1.5 h-1.5 rounded-full bg-success"></span>{{ 'common.live' | translate }}
                  </span>
                </a>
              </li>
            }
          </ul>
        }
      </section>

      <section class="flex flex-col gap-2">
        <div class="flex items-center justify-between">
          <h2 class="pts-label">{{ 'dashboard.tournaments' | translate }}</h2>
          @if (tournaments().length > 0) {
            <a routerLink="/tournaments" class="text-xs text-primary hover:underline">{{ 'common.viewAll' | translate }}</a>
          }
        </div>
        @if (loadingTournaments()) {
          <div class="flex items-center justify-center py-6"><pts-loading-spinner size="sm" /></div>
        } @else if (activeTournaments().length === 0) {
          <div class="pts-card flex flex-col items-center text-center gap-2 py-6">
            <span class="material-symbols-rounded text-3xl text-on-surface-muted">emoji_events</span>
            <p class="text-sm text-on-surface-muted">{{ 'dashboard.noTournaments' | translate }}</p>
            <a routerLink="/tournaments/new" class="pts-btn-primary mt-1">
              <span class="material-symbols-rounded text-lg">add</span>
              <span>{{ 'dashboard.createOne' | translate }}</span>
            </a>
          </div>
        } @else {
          <ul class="flex flex-col gap-2">
            @for (t of activeTournaments(); track t.id) {
              <li>
                <a [routerLink]="['/tournaments', t.id]"
                   class="pts-card !p-3 flex items-center gap-3 hover:border-primary transition-colors">
                  <span class="material-symbols-rounded text-2xl text-primary shrink-0">emoji_events</span>
                  <span class="flex-1 min-w-0">
                    <span class="block font-semibold text-on-surface truncate">{{ t.name }}</span>
                    <span class="block text-xs text-on-surface-muted truncate">
                      {{ 'tournament.format.' + t.format + '.label' | translate }}
                      · {{ 'tournament.list.teamsCount' | translate: { count: t.participantCount } }}
                    </span>
                  </span>
                  <span class="inline-flex items-center gap-1 text-success text-xs shrink-0">
                    <span class="w-1.5 h-1.5 rounded-full bg-success"></span>{{ 'common.live' | translate }}
                  </span>
                </a>
              </li>
            }
          </ul>
        }
      </section>

      @if (!loading() && recent().length > 0) {
        <section class="flex flex-col gap-2">
          <h2 class="pts-label">{{ 'dashboard.recent' | translate }}</h2>
          <ul class="flex flex-col gap-2">
            @for (c of recent(); track c.id) {
              <li>
                <a [routerLink]="['/counter', c.id]"
                   class="pts-card !p-3 flex items-center gap-3 hover:border-primary transition-colors">
                  <span class="material-symbols-rounded text-xl text-on-surface-muted shrink-0">
                    {{ iconFor(c.sportType) }}
                  </span>
                  <span class="flex-1 min-w-0">
                    <span class="block text-sm text-on-surface truncate">
                      {{ c.teamAName }}
                      <span class="text-on-surface-muted">{{ 'dashboard.vs' | translate }}</span>
                      {{ c.teamBName }}
                    </span>
                    <span class="block text-xs text-on-surface-muted mt-0.5 font-mono">
                      {{ c.setsWonA }}–{{ c.setsWonB }}
                    </span>
                  </span>
                  <time class="text-[11px] text-on-surface-muted shrink-0">
                    {{ c.updatedAt | date: 'shortDate' }}
                  </time>
                </a>
              </li>
            }
          </ul>
        </section>
      }
    </div>
  `,
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
