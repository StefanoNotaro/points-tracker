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

@Injectable({ providedIn: 'root' })
export class CounterHubService implements OnDestroy {
  private readonly auth = inject(AuthService);
  private connection: HubConnection | null = null;
  private currentCounterId: string | null = null;

  readonly scoreUpdated$ = new Subject<Counter>();
  readonly connectionStateChanged$ = new Subject<HubConnectionState>();

  async connect(counterId: string): Promise<void> {
    // If a connection already exists, just switch groups instead of rebuilding.
    if (this.connection) {
      if (this.connection.state === HubConnectionState.Connected) {
        await this.switchGroup(counterId);
        return;
      }
      // Stale/failed connection — tear it down before rebuilding.
      try { await this.connection.stop(); } catch { /* ignore */ }
      this.connection = null;
    }

    this.connection = new HubConnectionBuilder()
      .withUrl(`${environment.hubUrl}/counter`, {
        // Returning undefined (not '') tells SignalR not to send an Authorization header.
        accessTokenFactory: () => this.auth.getAccessToken() ?? undefined as unknown as string,
      })
      .withAutomaticReconnect()
      .configureLogging(environment.production ? LogLevel.Error : LogLevel.Warning)
      .build();

    this.connection.on('ScoreUpdated', (counter: Counter) => {
      this.scoreUpdated$.next(counter);
    });

    this.connection.onreconnecting(() => {
      this.connectionStateChanged$.next(HubConnectionState.Reconnecting);
    });

    this.connection.onreconnected(async () => {
      this.connectionStateChanged$.next(HubConnectionState.Connected);
      // Rejoin whatever counter is currently active, not the one captured at connect time.
      if (this.currentCounterId) {
        await this.joinGroup(this.currentCounterId);
      }
    });

    this.connection.onclose(() => {
      this.connectionStateChanged$.next(HubConnectionState.Disconnected);
    });

    await this.connection.start();
    await this.switchGroup(counterId);
    this.connectionStateChanged$.next(HubConnectionState.Connected);
  }

  async disconnect(counterId: string): Promise<void> {
    if (!this.connection) return;

    if (this.connection.state === HubConnectionState.Connected) {
      try {
        await this.connection.invoke('LeaveCounter', counterId);
      } catch {
        /* ignore — we're tearing down anyway */
      }
    }

    try { await this.connection.stop(); } catch { /* ignore */ }
    this.connection = null;
    this.currentCounterId = null;
  }

  async ngOnDestroy(): Promise<void> {
    if (this.connection) {
      try { await this.connection.stop(); } catch { /* ignore */ }
      this.connection = null;
    }
    this.scoreUpdated$.complete();
    this.connectionStateChanged$.complete();
  }

  private async switchGroup(counterId: string): Promise<void> {
    if (this.currentCounterId && this.currentCounterId !== counterId) {
      try {
        await this.connection!.invoke('LeaveCounter', this.currentCounterId);
      } catch { /* ignore — best-effort */ }
    }
    await this.joinGroup(counterId);
    this.currentCounterId = counterId;
  }

  private async joinGroup(counterId: string): Promise<void> {
    await this.connection!.invoke('JoinCounter', counterId);
  }
}
