import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  Counter,
  CounterSummary,
  CreateCounterRequest,
  CreateCounterResponse,
  ShareTokenResponse,
  ShareScope,
} from '../../../shared/models/counter.model';
import { SessionTokenService } from '../../../core/auth/session-token.service';

@Injectable({ providedIn: 'root' })
export class CounterService {
  private readonly http = inject(HttpClient);
  private readonly sessionTokens = inject(SessionTokenService);
  private readonly base = `${environment.apiUrl}/counters`;

  async create(request: CreateCounterRequest): Promise<CreateCounterResponse> {
    const response = await firstValueFrom(
      this.http.post<CreateCounterResponse>(this.base, request),
    );
    if (response.sessionToken) {
      this.sessionTokens.setToken(response.counter.id, response.sessionToken);
    }
    return response;
  }

  getById(id: string): Promise<Counter> {
    return firstValueFrom(this.http.get<Counter>(`${this.base}/${id}`));
  }

  incrementScore(id: string, team: 'A' | 'B'): Promise<Counter> {
    return firstValueFrom(
      this.http.post<Counter>(`${this.base}/${id}/score/increment`, { team }),
    );
  }

  decrementScore(id: string, team: 'A' | 'B'): Promise<Counter> {
    return firstValueFrom(
      this.http.post<Counter>(`${this.base}/${id}/score/decrement`, { team }),
    );
  }

  undo(id: string): Promise<Counter> {
    return firstValueFrom(this.http.post<Counter>(`${this.base}/${id}/undo`, {}));
  }

  resolveSideSwitch(id: string, confirm: boolean): Promise<Counter> {
    return firstValueFrom(
      this.http.post<Counter>(`${this.base}/${id}/side-switch`, { confirm }),
    );
  }

  switchSidesManually(id: string): Promise<Counter> {
    return firstValueFrom(
      this.http.post<Counter>(`${this.base}/${id}/switch-sides`, {}),
    );
  }

  updateTeamName(id: string, team: 'A' | 'B', name: string): Promise<Counter> {
    return firstValueFrom(
      this.http.patch<Counter>(`${this.base}/${id}/teams`, { team, name }),
    );
  }

  createShareToken(counterId: string, scope: ShareScope): Promise<ShareTokenResponse> {
    return firstValueFrom(
      this.http.post<ShareTokenResponse>(`${this.base}/${counterId}/share`, { scope }),
    );
  }

  joinByShareToken(token: string): Promise<Counter> {
    return firstValueFrom(this.http.get<Counter>(`${this.base}/join/${token}`));
  }

  listMine(): Promise<CounterSummary[]> {
    return firstValueFrom(this.http.get<CounterSummary[]>(`${this.base}/mine`));
  }

  async delete(id: string): Promise<void> {
    await firstValueFrom(this.http.delete<void>(`${this.base}/${id}`));
    this.sessionTokens.removeToken(id);
  }
}
