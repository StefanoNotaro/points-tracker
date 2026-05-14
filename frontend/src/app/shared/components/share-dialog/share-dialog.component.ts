import { Component, inject, signal } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { NgClass } from '@angular/common';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { ShareScope } from '../../models/counter.model';
import { CounterService } from '../../../features/counter/services/counter.service';
import { NotificationService } from '../../../core/services/notification.service';

export interface ShareDialogData {
  counterId: string;
}

@Component({
  selector: 'pts-share-dialog',
  imports: [MatDialogModule, NgClass, TranslatePipe],
  templateUrl: './share-dialog.component.html',
})
export class ShareDialogComponent {
  readonly data     = inject<ShareDialogData>(MAT_DIALOG_DATA);
  readonly dialogRef = inject(MatDialogRef<ShareDialogComponent>);
  private readonly counterService  = inject(CounterService);
  private readonly notifications   = inject(NotificationService);
  private readonly i18n            = inject(TranslateService);

  readonly scopes: { value: ShareScope; labelKey: string; descriptionKey: string; icon: string }[] = [
    { value: 'read', labelKey: 'counter.share.scopeReadLabel', descriptionKey: 'counter.share.scopeReadDescription', icon: 'visibility' },
    { value: 'edit', labelKey: 'counter.share.scopeEditLabel', descriptionKey: 'counter.share.scopeEditDescription', icon: 'edit' },
  ];

  selectedScope = signal<ShareScope>('read');
  shareUrl      = signal<string | null>(null);
  loading       = signal(false);

  onScopeChange(scope: ShareScope): void {
    this.selectedScope.set(scope);
    this.shareUrl.set(null);
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
      this.notifications.error(this.i18n.instant('counter.share.generateError'));
    } finally {
      this.loading.set(false);
    }
  }

  copyLink(): void {
    const url = this.shareUrl();
    if (!url) return;
    navigator.clipboard.writeText(url);
    this.notifications.success(this.i18n.instant('counter.share.linkCopied'));
  }
}
