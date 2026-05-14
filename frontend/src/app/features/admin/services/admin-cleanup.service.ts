import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface CleanupPreview {
  staleAnonymousCounters: number;
  staleAnonymousTournaments: number;
  expiredShareTokens: number;
  countersPastGrace: number;
  tournamentsPastGrace: number;
  sampleCounterIds: string[];
  sampleTournamentIds: string[];
}

export interface CleanupRunResult {
  countersSoftDeleted: number;
  tournamentsSoftDeleted: number;
  countersHardPurged: number;
  tournamentsHardPurged: number;
  shareTokensPurged: number;
}

export interface CleanupAuditEntry {
  id: string;
  action: string;
  actor: string;
  targetCount: number;
  targetIdsJson: string | null;
  reason: string | null;
  occurredAt: string;
}

@Injectable({ providedIn: 'root' })
export class AdminCleanupService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/admin/cleanup`;

  preview(): Promise<CleanupPreview> {
    return firstValueFrom(this.http.get<CleanupPreview>(`${this.base}/preview`));
  }

  auditLog(take = 100): Promise<CleanupAuditEntry[]> {
    return firstValueFrom(
      this.http.get<CleanupAuditEntry[]>(`${this.base}/audit?take=${take}`),
    );
  }

  runPolicy(reason?: string): Promise<CleanupRunResult> {
    return firstValueFrom(
      this.http.post<CleanupRunResult>(`${this.base}/run-policy`, {
        confirm: true,
        reason: reason ?? null,
      }),
    );
  }

  softDeleteCounters(ids: string[], reason?: string): Promise<{ affected: number }> {
    return firstValueFrom(
      this.http.post<{ affected: number }>(`${this.base}/soft-delete/counters`, {
        ids,
        confirm: true,
        reason: reason ?? null,
      }),
    );
  }

  softDeleteTournaments(ids: string[], reason?: string): Promise<{ affected: number }> {
    return firstValueFrom(
      this.http.post<{ affected: number }>(`${this.base}/soft-delete/tournaments`, {
        ids,
        confirm: true,
        reason: reason ?? null,
      }),
    );
  }

  hardPurgeCounters(ids: string[], reason: string): Promise<{ affected: number }> {
    return firstValueFrom(
      this.http.post<{ affected: number }>(`${this.base}/hard-purge/counters`, {
        ids,
        confirm: true,
        reason,
      }),
    );
  }

  hardPurgeTournaments(ids: string[], reason: string): Promise<{ affected: number }> {
    return firstValueFrom(
      this.http.post<{ affected: number }>(`${this.base}/hard-purge/tournaments`, {
        ids,
        confirm: true,
        reason,
      }),
    );
  }

  purgeExpiredShareTokens(): Promise<{ affected: number }> {
    return firstValueFrom(
      this.http.post<{ affected: number }>(`${this.base}/share-tokens/purge-expired`, {
        confirm: true,
      }),
    );
  }
}
