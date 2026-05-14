import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../../../core/auth/auth.service';
import { NotificationService } from '../../../../core/services/notification.service';
import {
  AdminCleanupService,
  CleanupAuditEntry,
  CleanupPreview,
  CleanupRunResult,
} from '../../services/admin-cleanup.service';

@Component({
  selector: 'pts-admin-cleanup',
  imports: [CommonModule, FormsModule, TranslatePipe],
  templateUrl: './admin-cleanup.component.html',
})
export class AdminCleanupComponent {
  private readonly api = inject(AdminCleanupService);
  private readonly notifications = inject(NotificationService);
  private readonly i18n = inject(TranslateService);
  readonly auth = inject(AuthService);

  readonly preview = signal<CleanupPreview | null>(null);
  readonly audit = signal<CleanupAuditEntry[]>([]);
  readonly loading = signal(false);
  readonly lastRun = signal<CleanupRunResult | null>(null);

  // Selection state for the by-id flows. The preview returns small samples;
  // the dashboard ticks them by default so an admin can act with one click.
  readonly selectedCounterIds = signal<Set<string>>(new Set());
  readonly selectedTournamentIds = signal<Set<string>>(new Set());

  readonly reason = signal('');
  readonly confirmingPolicy = signal(false);
  readonly confirmingHardPurge = signal<'counters' | 'tournaments' | null>(null);

  constructor() {
    void this.refresh();
  }

  async refresh(): Promise<void> {
    this.loading.set(true);
    try {
      const [p, a] = await Promise.all([this.api.preview(), this.api.auditLog(50)]);
      this.preview.set(p);
      this.audit.set(a);
      this.selectedCounterIds.set(new Set(p.sampleCounterIds));
      this.selectedTournamentIds.set(new Set(p.sampleTournamentIds));
    } catch {
      // The error interceptor toasts a generic message — nothing extra to add here.
    } finally {
      this.loading.set(false);
    }
  }

  toggleCounter(id: string): void {
    const next = new Set(this.selectedCounterIds());
    if (next.has(id)) next.delete(id); else next.add(id);
    this.selectedCounterIds.set(next);
  }

  toggleTournament(id: string): void {
    const next = new Set(this.selectedTournamentIds());
    if (next.has(id)) next.delete(id); else next.add(id);
    this.selectedTournamentIds.set(next);
  }

  async runPolicy(): Promise<void> {
    this.confirmingPolicy.set(false);
    this.loading.set(true);
    try {
      const result = await this.api.runPolicy(this.reason() || undefined);
      this.lastRun.set(result);
      this.notifications.success(this.i18n.instant('admin.cleanup.toasts.policyRun'));
      await this.refresh();
    } finally {
      this.loading.set(false);
    }
  }

  async softDeleteCounters(): Promise<void> {
    const ids = Array.from(this.selectedCounterIds());
    if (ids.length === 0) return;
    this.loading.set(true);
    try {
      const { affected } = await this.api.softDeleteCounters(ids, this.reason() || undefined);
      this.notifications.success(this.i18n.instant('admin.cleanup.toasts.softDeleted', { count: affected }));
      await this.refresh();
    } finally {
      this.loading.set(false);
    }
  }

  async softDeleteTournaments(): Promise<void> {
    const ids = Array.from(this.selectedTournamentIds());
    if (ids.length === 0) return;
    this.loading.set(true);
    try {
      const { affected } = await this.api.softDeleteTournaments(ids, this.reason() || undefined);
      this.notifications.success(this.i18n.instant('admin.cleanup.toasts.softDeleted', { count: affected }));
      await this.refresh();
    } finally {
      this.loading.set(false);
    }
  }

  async hardPurgeCounters(): Promise<void> {
    this.confirmingHardPurge.set(null);
    const ids = Array.from(this.selectedCounterIds());
    if (ids.length === 0 || !this.reason().trim()) return;
    this.loading.set(true);
    try {
      const { affected } = await this.api.hardPurgeCounters(ids, this.reason());
      this.notifications.success(this.i18n.instant('admin.cleanup.toasts.hardPurged', { count: affected }));
      await this.refresh();
    } finally {
      this.loading.set(false);
    }
  }

  async hardPurgeTournaments(): Promise<void> {
    this.confirmingHardPurge.set(null);
    const ids = Array.from(this.selectedTournamentIds());
    if (ids.length === 0 || !this.reason().trim()) return;
    this.loading.set(true);
    try {
      const { affected } = await this.api.hardPurgeTournaments(ids, this.reason());
      this.notifications.success(this.i18n.instant('admin.cleanup.toasts.hardPurged', { count: affected }));
      await this.refresh();
    } finally {
      this.loading.set(false);
    }
  }

  async purgeExpiredTokens(): Promise<void> {
    this.loading.set(true);
    try {
      const { affected } = await this.api.purgeExpiredShareTokens();
      this.notifications.success(this.i18n.instant('admin.cleanup.toasts.tokensPurged', { count: affected }));
      await this.refresh();
    } finally {
      this.loading.set(false);
    }
  }
}
