import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  CreateTournamentRequest,
  CreateTournamentResponse,
  IssuedMatchScorerLink,
  MatchScorerLink,
  ScorerJoinDto,
  Tournament,
  TournamentSummary,
  UpdateTournamentRequest,
} from '../../../shared/models/tournament.model';
import { Counter } from '../../../shared/models/counter.model';
import { SessionTokenService } from '../../../core/auth/session-token.service';
import { ScorerTokenService } from '../../../core/auth/scorer-token.service';

@Injectable({ providedIn: 'root' })
export class TournamentService {
  private readonly http = inject(HttpClient);
  private readonly sessionTokens = inject(SessionTokenService);
  private readonly scorerTokens = inject(ScorerTokenService);
  private readonly base = `${environment.apiUrl}/tournaments`;
  private readonly scorerLinksBase = `${environment.apiUrl}/scorer-links`;

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

  listMineAnonymous(): Promise<TournamentSummary[]> {
    const sessionTokens = this.sessionTokens.getAllTournamentTokens();
    if (sessionTokens.length === 0) return Promise.resolve([]);
    return firstValueFrom(
      this.http.post<TournamentSummary[]>(`${this.base}/mine-anonymous`, { sessionTokens }),
    );
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

  start(id: string, options?: { randomizeUnseeded?: boolean }): Promise<Tournament> {
    return firstValueFrom(
      this.http.post<Tournament>(`${this.base}/${id}/start`, {
        randomizeUnseeded: !!options?.randomizeUnseeded,
      }),
    );
  }

  update(id: string, payload: UpdateTournamentRequest): Promise<Tournament> {
    return firstValueFrom(this.http.patch<Tournament>(`${this.base}/${id}/rules`, payload));
  }

  openMatchCounter(id: string, matchId: string, scorerToken?: string | null): Promise<Counter> {
    const options = scorerToken
      ? { headers: new HttpHeaders({ 'X-Scorer-Token': scorerToken }) }
      : undefined;
    return firstValueFrom(
      this.http.post<Counter>(`${this.base}/${id}/matches/${matchId}/counter`, {}, options),
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

  resolveMatchScorerLink(token: string): Promise<ScorerJoinDto> {
    return firstValueFrom(
      this.http.get<ScorerJoinDto>(`${this.scorerLinksBase}/resolve/${encodeURIComponent(token)}`),
    );
  }

  issueMatchScorerLink(tournamentId: string, matchId: string, label: string | null): Promise<IssuedMatchScorerLink> {
    return firstValueFrom(
      this.http.post<IssuedMatchScorerLink>(
        `${this.base}/${tournamentId}/matches/${matchId}/scorer-links`,
        { label, grantToUserId: null },
      ),
    );
  }

  revokeMatchScorerLink(tournamentId: string, linkId: string): Promise<void> {
    return firstValueFrom(
      this.http.delete<void>(`${this.base}/${tournamentId}/scorer-links/${linkId}`),
    );
  }

  listMatchScorerLinks(tournamentId: string, matchId: string): Promise<MatchScorerLink[]> {
    return firstValueFrom(
      this.http.get<MatchScorerLink[]>(`${this.base}/${tournamentId}/matches/${matchId}/scorer-links`),
    );
  }

  storeScorerToken(counterId: string, token: string): void {
    this.scorerTokens.setToken(counterId, token);
  }
}
