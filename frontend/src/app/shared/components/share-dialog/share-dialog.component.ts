import { Component, inject, signal } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatTabsModule } from '@angular/material/tabs';
import { NgClass } from '@angular/common';
import { ShareScope, ShareTokenResponse } from '../../models/counter.model';
import { CounterService } from '../../../features/counter/services/counter.service';
import { NotificationService } from '../../../core/services/notification.service';

export interface ShareDialogData {
  counterId: string;
}

@Component({
  selector: 'pts-share-dialog',
  imports: [MatDialogModule, MatButtonModule, MatTabsModule, NgClass],
  template: `
    <div class="p-6 min-w-[320px] max-w-sm">
      <h2 class="text-lg font-semibold text-on-surface mb-4">Share Counter</h2>

      <div class="flex gap-2 mb-4">
        @for (scope of scopes; track scope.value) {
          <button
            type="button"
            class="flex-1 py-2 rounded-md text-sm font-medium border-2 transition-all"
            [ngClass]="
              selectedScope() === scope.value
                ? 'border-primary bg-primary/8 text-primary'
                : 'border-border text-on-surface-muted hover:border-primary/40'
            "
            (click)="selectedScope.set(scope.value)"
          >
            {{ scope.label }}
          </button>
        }
      </div>

      @if (shareUrl()) {
        <div class="bg-surface-variant rounded-md p-3 mb-4">
          <p class="text-xs text-on-surface-muted mb-1">Share link</p>
          <p class="text-sm break-all font-mono text-on-surface select-all">{{ shareUrl() }}</p>
        </div>
        <div class="flex gap-2">
          <button class="pts-btn-primary flex-1" (click)="copyLink()">
            <span class="material-symbols-rounded text-base mr-1">content_copy</span>
            Copy Link
          </button>
          <button class="pts-btn-secondary" (click)="generate()" [disabled]="loading()">
            Regenerate
          </button>
        </div>
      } @else {
        <button
          class="pts-btn-primary w-full"
          (click)="generate()"
          [disabled]="loading()"
        >
          {{ loading() ? 'Generating...' : 'Generate Link' }}
        </button>
      }

      <button
        mat-button
        class="mt-4 w-full text-on-surface-muted"
        (click)="dialogRef.close()"
      >
        Close
      </button>
    </div>
  `,
})
export class ShareDialogComponent {
  readonly data = inject<ShareDialogData>(MAT_DIALOG_DATA);
  readonly dialogRef = inject(MatDialogRef<ShareDialogComponent>);
  private readonly counterService = inject(CounterService);
  private readonly notifications = inject(NotificationService);

  readonly scopes: { value: ShareScope; label: string }[] = [
    { value: 'read', label: 'View only' },
    { value: 'edit', label: 'Can edit' },
  ];

  selectedScope = signal<ShareScope>('read');
  shareUrl = signal<string | null>(null);
  loading = signal(false);

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
