import { Injectable, inject, OnDestroy } from '@angular/core';
import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { Tournament } from '../../../shared/models/tournament.model';
import { AuthService } from '../../../core/auth/auth.service';

@Injectable({ providedIn: 'root' })
export class TournamentHubService implements OnDestroy {
  private readonly auth = inject(AuthService);
  private connection: HubConnection | null = null;
  private connecting: Promise<void> | null = null;
  private readonly groups = new Set<string>();

  readonly tournamentUpdated$ = new Subject<Tournament>();
  readonly tournamentDeleted$ = new Subject<string>();

  async joinTournament(id: string): Promise<void> {
    await this.ensureConnected();
    if (this.groups.has(id)) return;
    this.groups.add(id);
    await this.connection!.invoke('JoinTournament', id);
  }

  async leaveTournament(id: string): Promise<void> {
    if (!this.groups.delete(id)) return;
    if (this.connection?.state === HubConnectionState.Connected) {
      try { await this.connection.invoke('LeaveTournament', id); } catch { /* best-effort */ }
    }
  }

  async ngOnDestroy(): Promise<void> {
    if (this.connection) {
      try { await this.connection.stop(); } catch { /* ignore */ }
      this.connection = null;
    }
    this.tournamentUpdated$.complete();
    this.tournamentDeleted$.complete();
  }

  private async ensureConnected(): Promise<void> {
    if (this.connection?.state === HubConnectionState.Connected) return;
    if (this.connecting) return this.connecting;

    this.connecting = (async () => {
      this.connection = new HubConnectionBuilder()
        .withUrl(`${environment.hubUrl}/tournament`, {
          accessTokenFactory: () =>
            this.auth.getAccessToken() ?? (undefined as unknown as string),
        })
        .withAutomaticReconnect()
        .configureLogging(environment.production ? LogLevel.Error : LogLevel.Warning)
        .build();

      this.connection.on('TournamentUpdated', (t: Tournament) => this.tournamentUpdated$.next(t));
      this.connection.on('TournamentDeleted', (id: string) => this.tournamentDeleted$.next(id));

      this.connection.onreconnected(async () => {
        for (const id of this.groups) {
          try { await this.connection!.invoke('JoinTournament', id); } catch { /* ignore */ }
        }
      });

      await this.connection.start();
    })();

    try { await this.connecting; } finally { this.connecting = null; }
  }
}
