import { Component, inject, signal } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { NgClass } from '@angular/common';
import { ShareScope } from '../../models/counter.model';
import { CounterService } from '../../../features/counter/services/counter.service';
import { NotificationService } from '../../../core/services/notification.service';

export interface ShareDialogData {
  counterId: string;
}

@Component({
  selector: 'pts-share-dialog',
  imports: [MatDialogModule, NgClass],
  template: `
    <div class="p-6 w-full max-w-sm">

      <!-- Header -->
      <div class="flex items-center gap-3 mb-5">
        <div class="w-10 h-10 rounded-xl bg-primary/10 flex items-center justify-center">
          <span class="material-symbols-rounded text-primary text-xl">share</span>
        </div>
        <h2 class="text-lg font-bold text-on-surface">Share Counter</h2>
      </div>

      <!-- Scope selector -->
      <div class="grid grid-cols-2 gap-2 mb-4">
        @for (scope of scopes; track scope.value) {
          <button
            type="button"
            class="flex flex-col items-start gap-1 p-3 rounded-xl border-2 transition-all duration-150 text-left"
            [ngClass]="selectedScope() === scope.value
              ? 'border-primary bg-primary/8'
              : 'border-border bg-surface-raised hover:border-primary/30'"
            (click)="onScopeChange(scope.value)"
          >
            <span
              class="material-symbols-rounded text-lg"
              [ngClass]="selectedScope() === scope.value ? 'text-primary' : 'text-on-surface-muted'"
            >{{ scope.icon }}</span>
            <span
              class="text-sm font-semibold"
              [ngClass]="selectedScope() === scope.value ? 'text-primary' : 'text-on-surface'"
            >{{ scope.label }}</span>
            <span class="text-xs text-on-surface-muted">{{ scope.description }}</span>
          </button>
        }
      </div>

      <!-- Generated link -->
      @if (shareUrl()) {
        <div class="bg-surface-variant rounded-xl p-3 mb-4 border border-border">
          <p class="pts-label mb-1.5">Share link</p>
          <p class="text-xs font-mono text-on-surface break-all select-all leading-relaxed">
            {{ shareUrl() }}
          </p>
        </div>

        <div class="flex gap-2 mb-3">
          <button class="pts-btn-primary flex-1" (click)="copyLink()">
            <span class="material-symbols-rounded text-base">content_copy</span>
            Copy link
          </button>
          <button
            class="pts-btn-secondary px-3"
            (click)="generate()"
            [disabled]="loading()"
            title="Regenerate link"
          >
            <span class="material-symbols-rounded text-base">refresh</span>
          </button>
        </div>
      } @else {
        <button
          class="pts-btn-primary w-full mb-3"
          (click)="generate()"
          [disabled]="loading()"
        >
          @if (loading()) {
            <span class="material-symbols-rounded animate-spin text-base">progress_activity</span>
            Generating…
          } @else {
            <span class="material-symbols-rounded text-base">link</span>
            Generate link
          }
        </button>
      }

      <!-- Close -->
      <button
        class="w-full text-sm text-on-surface-muted hover:text-on-surface transition-colors py-1"
        (click)="dialogRef.close()"
      >
        Close
      </button>
    </div>
  `,
})
export class ShareDialogComponent {
  readonly data     = inject<ShareDialogData>(MAT_DIALOG_DATA);
  readonly dialogRef = inject(MatDialogRef<ShareDialogComponent>);
  private readonly counterService  = inject(CounterService);
  private readonly notifications   = inject(NotificationService);

  readonly scopes: { value: ShareScope; label: string; description: string; icon: string }[] = [
    { value: 'read', label: 'View only',  description: 'Can watch live score', icon: 'visibility' },
    { value: 'edit', label: 'Can score',  description: 'Can add/remove points', icon: 'edit' },
  ];

  selectedScope = signal<ShareScope>('read');
  shareUrl      = signal<string | null>(null);
  loading       = signal(false);

  onScopeChange(scope: ShareScope): void {
    this.selectedScope.set(scope);
    this.shareUrl.set(null); // reset link when scope changes
  }

  async generate(): Promise<void> {
    this.loading.set(true);
    try {
      const response = await this.counterService.createShareToken(
        this.data.counterId,
        this.selectedScope(),
      );
      this.shareUrl.set(response.shareUrl);
    } catch {
      this.notifications.error('Failed to generate share link.');
    } finally {
      this.loading.set(false);
    }
  }

  copyLink(): void {
    const url = this.shareUrl();
    if (!url) return;
    navigator.clipboard.writeText(url);
    this.notifications.success('Link copied to clipboard!');
  }
}
