import { Injectable, inject, OnDestroy } from '@angular/core';
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { Counter } from '../../../shared/models/counter.model';
import { AuthService } from '../../../core/auth/auth.service';
import { SessionTokenService } from '../../../core/auth/session-token.service';
import { ShareTokenService } from '../../../core/auth/share-token.service';

/** Group bookkeeping — group key, server method names, ref count. */
type GroupKind = 'counter' | 'user';
interface GroupSubscription {
  kind: GroupKind;
  id: string;
  refs: number;
}

/**
 * Singleton SignalR connection shared by the counter page and the dashboard.
 *
 * Each subscriber asks for a group (counter-id or user-id); the service
 * ref-counts joins so the connection is established once, groups are joined
 * lazily, and a "leave" only fires when the last subscriber drops out.
 */
@Injectable({ providedIn: 'root' })
export class CounterHubService implements OnDestroy {
  private readonly auth = inject(AuthService);
  private readonly sessionTokens = inject(SessionTokenService);
  private readonly shareTokens = inject(ShareTokenService);
  private connection: HubConnection | null = null;
  private connecting: Promise<void> | null = null;
  private readonly groups = new Map<string, GroupSubscription>();

  readonly scoreUpdated$ = new Subject<Counter>();
  readonly counterDeleted$ = new Subject<string>();
  readonly liveAccessDenied$ = new Subject<string>();
  readonly connectionStateChanged$ = new Subject<HubConnectionState>();

  // ── Counter group ────────────────────────────────────────────────────
  joinCounter(counterId: string): Promise<void> {
    return this.join('counter', counterId);
  }

  leaveCounter(counterId: string): Promise<void> {
    return this.leave('counter', counterId);
  }

  // ── User group (dashboard) ───────────────────────────────────────────
  // No userId param: the server picks the authenticated identity. Internally
  // we tag the subscription with a sentinel so the ref-count logic still
  // works (only one user-group per connection makes sense anyway).
  private static readonly USER_KEY = '__self__';

  joinUser(): Promise<void> {
    return this.join('user', CounterHubService.USER_KEY);
  }

  leaveUser(): Promise<void> {
    return this.leave('user', CounterHubService.USER_KEY);
  }

  async ngOnDestroy(): Promise<void> {
    if (this.connection) {
      try { await this.connection.stop(); } catch { /* ignore */ }
      this.connection = null;
    }
    this.scoreUpdated$.complete();
    this.counterDeleted$.complete();
    this.liveAccessDenied$.complete();
    this.connectionStateChanged$.complete();
  }

  private key(kind: GroupKind, id: string): string {
    return `${kind}:${id}`;
  }

  private async join(kind: GroupKind, id: string): Promise<void> {
    await this.ensureConnected();

    const key = this.key(kind, id);
    const existing = this.groups.get(key);
    if (existing) {
      existing.refs += 1;
      return;
    }

    try {
      await this.invokeJoin(kind, id);
      this.groups.set(key, { kind, id, refs: 1 });
    } catch (error) {
      if (kind === 'counter' && isLiveAccessDenied(error)) {
        throw new CounterHubAccessDeniedError(id);
      }

      throw error;
    }
  }

  private async leave(kind: GroupKind, id: string): Promise<void> {
    const key = this.key(kind, id);
    const existing = this.groups.get(key);
    if (!existing) return;

    existing.refs -= 1;
    if (existing.refs > 0) return;

    this.groups.delete(key);
    if (this.connection?.state === HubConnectionState.Connected) {
      try { await this.invokeLeave(kind, id); } catch { /* best-effort */ }
    }
  }

  private invokeJoin(kind: GroupKind, id: string): Promise<void> {
    return kind === 'counter'
      ? this.connection!.invoke('JoinCounter', id, this.sessionTokens.getToken(id), this.shareTokens.getToken(id))
      : this.connection!.invoke('JoinMyUpdates');
  }

  private invokeLeave(kind: GroupKind, id: string): Promise<void> {
    return kind === 'counter'
      ? this.connection!.invoke('LeaveCounter', id)
      : this.connection!.invoke('LeaveMyUpdates');
  }

  private async ensureConnected(): Promise<void> {
    if (this.connection?.state === HubConnectionState.Connected) return;
    if (this.connecting) return this.connecting;

    this.connecting = (async () => {
      this.connection = new HubConnectionBuilder()
        .withUrl(`${environment.hubUrl}/counter`, {
          accessTokenFactory: () =>
            this.auth.getAccessToken() ?? (undefined as unknown as string),
        })
        .withAutomaticReconnect()
        .configureLogging(environment.production ? LogLevel.Error : LogLevel.Warning)
        .build();

      this.connection.on('ScoreUpdated', (counter: Counter) => {
        this.scoreUpdated$.next(counter);
      });
      this.connection.on('CounterDeleted', (counterId: string) => {
        this.counterDeleted$.next(counterId);
      });

      this.connection.onreconnecting(() => {
        this.connectionStateChanged$.next(HubConnectionState.Reconnecting);
      });
      this.connection.onreconnected(async () => {
        // Rejoin every group from scratch — the server forgot us during the
        // disconnect.
        for (const g of [...this.groups.values()]) {
          try {
            await this.invokeJoin(g.kind, g.id);
          } catch (error) {
            if (g.kind === 'counter' && isLiveAccessDenied(error)) {
              this.groups.delete(this.key(g.kind, g.id));
              this.liveAccessDenied$.next(g.id);
            }
          }
        }
        this.connectionStateChanged$.next(HubConnectionState.Connected);
      });
      this.connection.onclose(() => {
        this.connectionStateChanged$.next(HubConnectionState.Disconnected);
      });

      await this.connection.start();
      this.connectionStateChanged$.next(HubConnectionState.Connected);
    })();

    try { await this.connecting; }
    finally { this.connecting = null; }
  }
}

export class CounterHubAccessDeniedError extends Error {
  constructor(readonly counterId: string) {
    super('counter.liveAccessDenied');
  }
}

function isLiveAccessDenied(error: unknown): boolean {
  if (error instanceof Error) {
    return error.message.includes('counter.liveAccessDenied');
  }

  return typeof error === 'string' && error.includes('counter.liveAccessDenied');
}

