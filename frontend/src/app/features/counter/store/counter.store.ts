import { Injectable, signal, computed, inject, OnDestroy } from '@angular/core';
import { Subscription } from 'rxjs';
import { Counter, Team } from '../../../shared/models/counter.model';
import { SportConfig, SPORT_CONFIGS } from '../../../shared/models/sport.model';
import { CounterService } from '../services/counter.service';
import { CounterHubService } from '../services/counter-hub.service';
import { NotificationService } from '../../../core/services/notification.service';

type LoadState = 'idle' | 'loading' | 'loaded' | 'error';

@Injectable()
export class CounterStore implements OnDestroy {
  private readonly counterService = inject(CounterService);
  private readonly hubService = inject(CounterHubService);
  private readonly notifications = inject(NotificationService);
  private hubSub: Subscription | null = null;

  private readonly _counter = signal<Counter | null>(null);
  private readonly _loadState = signal<LoadState>('idle');
  private readonly _actionPending = signal(false);

  readonly counter = this._counter.asReadonly();
  readonly loadState = this._loadState.asReadonly();
  readonly actionPending = this._actionPending.asReadonly();
  readonly isLoading = computed(() => this._loadState() === 'loading');

  readonly sportConfig = computed((): SportConfig | null => {
    const c = this._counter();
    return c ? SPORT_CONFIGS[c.sportType] : null;
  });

  async load(counterId: string): Promise<void> {
    this._loadState.set('loading');
    try {
      const counter = await this.counterService.getById(counterId);
      this._counter.set(counter);
      this._loadState.set('loaded');
    } catch {
      this._loadState.set('error');
      return;
    }

    // SignalR is best-effort — a connection failure must not hide a loaded counter.
    try {
      await this.subscribeToHub(counterId);
    } catch (err) {
      console.warn('Real-time updates unavailable:', err);
      this.notifications.error('Live updates unavailable. Scores will not refresh automatically.');
    }
  }

  async incrementScore(team: Team): Promise<void> {
    const counter = this._counter();
    if (!counter || this._actionPending()) return;

    this._actionPending.set(true);
    try {
      const updated = await this.counterService.incrementScore(counter.id, team);
      this.applyUpdate(updated);
    } catch {
      this.notifications.error('Failed to update score.');
    } finally {
      this._actionPending.set(false);
    }
  }

  async resolveSideSwitch(confirm: boolean): Promise<void> {
    const counter = this._counter();
    if (!counter) return;
    try {
      const updated = await this.counterService.resolveSideSwitch(counter.id, confirm);
      this.applyUpdate(updated);
    } catch {
      this.notifications.error('Failed to record side switch.');
    }
  }

  async switchSidesManually(): Promise<void> {
    const counter = this._counter();
    if (!counter) return;
    try {
      const updated = await this.counterService.switchSidesManually(counter.id);
      this.applyUpdate(updated);
    } catch {
      this.notifications.error('Failed to switch sides.');
    }
  }

  async decrementScore(team: Team): Promise<void> {
    const counter = this._counter();
    if (!counter || this._actionPending()) return;

    this._actionPending.set(true);
    try {
      const updated = await this.counterService.decrementScore(counter.id, team);
      this.applyUpdate(updated);
    } catch {
      this.notifications.error('Failed to update score.');
    } finally {
      this._actionPending.set(false);
    }
  }

  async undo(): Promise<void> {
    const counter = this._counter();
    if (!counter || this._actionPending()) return;

    this._actionPending.set(true);
    try {
      const updated = await this.counterService.undo(counter.id);
      this.applyUpdate(updated);
    } catch {
      this.notifications.error('Failed to undo.');
    } finally {
      this._actionPending.set(false);
    }
  }

  async updateTeamName(team: Team, name: string): Promise<void> {
    const counter = this._counter();
    if (!counter) return;
    try {
      const updated = await this.counterService.updateTeamName(counter.id, team, name);
      this.applyUpdate(updated);
    } catch {
      this.notifications.error('Failed to update team name.');
    }
  }

  ngOnDestroy(): void {
    const id = this._counter()?.id;
    if (id) {
      this.hubService.disconnect(id);
    }
    this.hubSub?.unsubscribe();
  }

  private async subscribeToHub(counterId: string): Promise<void> {
    this.hubSub = this.hubService.scoreUpdated$.subscribe((updated) => {
      // The broadcast carries isOwner/canEdit computed for whoever made the change,
      // not for this client. Preserve our own access flags so a viewer doesn't
      // suddenly see edit buttons just because the owner moved the score.
      const current = this._counter();
      const merged = current
        ? { ...updated, isOwner: current.isOwner, canEdit: current.canEdit }
        : updated;
      this.applyUpdate(merged);
    });
    await this.hubService.connect(counterId);
  }

  private applyUpdate(next: Counter): void {
    this._counter.set(next);
    // The "side switched" notification is handled by the counter page so it can
    // render as a dialog (auto-dismissed after 5s) rather than a toast.
  }
}
