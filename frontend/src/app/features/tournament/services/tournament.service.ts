import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  CreateTournamentRequest,
  CreateTournamentResponse,
  Tournament,
  TournamentSummary,
  UpdateTournamentRequest,
} from '../../../shared/models/tournament.model';
import { Counter } from '../../../shared/models/counter.model';
import { SessionTokenService } from '../../../core/auth/session-token.service';

@Injectable({ providedIn: 'root' })
export class TournamentService {
  private readonly http = inject(HttpClient);
  private readonly sessionTokens = inject(SessionTokenService);
  private readonly base = `${environment.apiUrl}/tournaments`;

  async create(req: CreateTournamentRequest): Promise<CreateTournamentResponse> {
    const res = await firstValueFrom(this.http.post<CreateTournamentResponse>(this.base, req));
    if (res.sessionToken) {
      this.sessionTokens.setToken(`tournament:${res.tournament.id}`, res.sessionToken);
    }
    return res;
  }

  getById(id: string): Promise<Tournament> {
    return firstValueFrom(this.http.get<Tournament>(`${this.base}/${id}`));
  }

  listMine(): Promise<TournamentSummary[]> {
    return firstValueFrom(this.http.get<TournamentSummary[]>(`${this.base}/mine`));
  }

  addParticipant(id: string, teamName: string, seed: number | null): Promise<Tournament> {
    return firstValueFrom(
      this.http.post<Tournament>(`${this.base}/${id}/participants`, { teamName, seed, userId: null }),
    );
  }

  removeParticipant(id: string, participantId: string): Promise<Tournament> {
    return firstValueFrom(
      this.http.delete<Tournament>(`${this.base}/${id}/participants/${participantId}`),
    );
  }

  start(id: string): Promise<Tournament> {
    return firstValueFrom(this.http.post<Tournament>(`${this.base}/${id}/start`, {}));
  }

  update(id: string, payload: UpdateTournamentRequest): Promise<Tournament> {
    return firstValueFrom(this.http.patch<Tournament>(`${this.base}/${id}/rules`, payload));
  }

  openMatchCounter(id: string, matchId: string): Promise<Counter> {
    return firstValueFrom(
      this.http.post<Counter>(`${this.base}/${id}/matches/${matchId}/counter`, {}),
    );
  }

  recordResult(id: string, matchId: string, winnerParticipantId: string): Promise<Tournament> {
    return firstValueFrom(
      this.http.post<Tournament>(`${this.base}/${id}/matches/${matchId}/result`, {
        winnerParticipantId,
      }),
    );
  }

  async delete(id: string): Promise<void> {
    await firstValueFrom(this.http.delete<void>(`${this.base}/${id}`));
    this.sessionTokens.removeToken(`tournament:${id}`);
  }
}
