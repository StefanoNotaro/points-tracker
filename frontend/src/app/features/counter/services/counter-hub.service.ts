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

  readonly scoreUpdated$ = new Subject<Counter>();
  readonly connectionStateChanged$ = new Subject<HubConnectionState>();

  async connect(counterId: string): Promise<void> {
    if (this.connection?.state === HubConnectionState.Connected) {
      await this.joinGroup(counterId);
      return;
    }

    this.connection = new HubConnectionBuilder()
      .withUrl(`${environment.hubUrl}/counter`, {
        accessTokenFactory: () => this.auth.getAccessToken() ?? '',
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
      await this.joinGroup(counterId);
    });

    await this.connection.start();
    await this.joinGroup(counterId);
    this.connectionStateChanged$.next(HubConnectionState.Connected);
  }

  async disconnect(counterId: string): Promise<void> {
    if (this.connection?.state === HubConnectionState.Connected) {
      await this.connection.invoke('LeaveCounter', counterId);
    }
  }

  async ngOnDestroy(): Promise<void> {
    await this.connection?.stop();
    this.scoreUpdated$.complete();
    this.connectionStateChanged$.complete();
  }

  private async joinGroup(counterId: string): Promise<void> {
    await this.connection!.invoke('JoinCounter', counterId);
  }
}
