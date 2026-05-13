import { Component, computed, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { CounterService } from '../counter/services/counter.service';
import { CounterHubService } from '../counter/services/counter-hub.service';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner.component';
import { Counter, CounterSummary } from '../../shared/models/counter.model';
import { SPORT_CONFIGS } from '../../shared/models/sport.model';
import { NotificationService } from '../../core/services/notification.service';

@Component({
  selector: 'pts-dashboard',
  imports: [RouterLink, LoadingSpinnerComponent, DatePipe],
  template: `
    <div class="flex flex-col gap-5 pb-8">

      <!-- Greeting -->
      <header class="flex flex-col gap-1">
        <p class="text-xs uppercase tracking-wide text-on-surface-muted">Welcome back</p>
        <h1 class="text-2xl font-bold text-on-surface truncate">
          {{ auth.user()?.displayName }}
        </h1>
      </header>

      <!-- Quick actions -->
      <div class="grid grid-cols-2 gap-3">
        <a routerLink="/new-counter"
           class="pts-card flex flex-col gap-1 hover:border-primary transition-colors">
          <span class="material-symbols-rounded text-primary text-2xl">add_circle</span>
          <p class="font-semibold text-on-surface text-sm">New counter</p>
          <p class="text-xs text-on-surface-muted">Start tracking a match.</p>
        </a>
        <a routerLink="/my-counters"
           class="pts-card flex flex-col gap-1 hover:border-primary transition-colors">
          <span class="material-symbols-rounded text-primary text-2xl">list_alt</span>
          <p class="font-semibold text-on-surface text-sm">All counters</p>
          <p class="text-xs text-on-surface-muted">Resume or review past matches.</p>
        </a>
      </div>

      <!-- Stats -->
      <div class="grid grid-cols-3 gap-3">
        <div class="pts-card !p-3 text-center">
          <p class="text-2xl font-bold text-on-surface font-mono">{{ stats().total }}</p>
          <p class="text-[11px] uppercase tracking-wide text-on-surface-muted mt-0.5">Total</p>
        </div>
        <div class="pts-card !p-3 text-center">
          <p class="text-2xl font-bold text-success font-mono">{{ stats().active }}</p>
          <p class="text-[11px] uppercase tracking-wide text-on-surface-muted mt-0.5">Active</p>
        </div>
        <div class="pts-card !p-3 text-center">
          <p class="text-2xl font-bold text-on-surface font-mono">{{ stats().finished }}</p>
          <p class="text-[11px] uppercase tracking-wide text-on-surface-muted mt-0.5">Finished</p>
        </div>
      </div>

      <!-- Active matches -->
      <section class="flex flex-col gap-2">
        <div class="flex items-center justify-between">
          <h2 class="pts-label">Active</h2>
          @if (active().length > 0) {
            <a routerLink="/my-counters" class="text-xs text-primary hover:underline">
              View all
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
            <p class="text-sm text-on-surface-muted">No matches in progress.</p>
            <a routerLink="/new-counter" class="pts-btn-primary mt-1">
              <span class="material-symbols-rounded text-lg">add</span>
              <span>Start one</span>
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
                      <span class="text-on-surface-muted font-normal">vs</span>
                      {{ c.teamBName }}
                    </span>
                    <span class="block text-xs text-on-surface-muted mt-0.5">
                      <span class="font-mono text-on-surface">{{ c.setsWonA }}–{{ c.setsWonB }}</span>
                      · current {{ c.currentScoreA }}–{{ c.currentScoreB }}
                    </span>
                  </span>
                  <span class="inline-flex items-center gap-1 text-success text-xs shrink-0">
                    <span class="w-1.5 h-1.5 rounded-full bg-success"></span>Live
                  </span>
                </a>
              </li>
            }
          </ul>
        }
      </section>

      <!-- Recent finished -->
      @if (!loading() && recent().length > 0) {
        <section class="flex flex-col gap-2">
          <h2 class="pts-label">Recent</h2>
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
                      <span class="text-on-surface-muted">vs</span>
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
  readonly auth                   = inject(AuthService);
  private readonly counterService = inject(CounterService);
  private readonly hub            = inject(CounterHubService);
  private readonly notifications  = inject(NotificationService);

  readonly loading  = signal(true);
  readonly counters = signal<CounterSummary[]>([]);
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
      this.notifications.error('Could not load your counters.');
    } finally {
      this.loading.set(false);
    }

    // Subscribe to the per-user SignalR group. The server broadcasts every
    // owned counter's updates to user-{id}, so this single subscription
    // keeps the dashboard live no matter how many counters are running.
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
    // Map the broadcast Counter onto our lighter CounterSummary shape.
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
