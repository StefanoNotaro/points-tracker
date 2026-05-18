import { Component, inject, signal } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { NgClass } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { ShareScope } from '../../models/counter.model';

export interface ShareDialogData {
  counterId: string;
  generate: (scope: ShareScope) => Promise<{ shareUrl: string }>;
}

@Component({
  selector: 'pts-share-dialog',
  imports: [MatDialogModule, NgClass, TranslatePipe],
  templateUrl: './share-dialog.component.html',
})
export class ShareDialogComponent {
  readonly data      = inject<ShareDialogData>(MAT_DIALOG_DATA);
  readonly dialogRef = inject(MatDialogRef<ShareDialogComponent>);

  readonly scopes: { value: ShareScope; labelKey: string; descriptionKey: string; icon: string }[] = [
    { value: 'read', labelKey: 'counter.share.scopeReadLabel', descriptionKey: 'counter.share.scopeReadDescription', icon: 'visibility' },
    { value: 'edit', labelKey: 'counter.share.scopeEditLabel', descriptionKey: 'counter.share.scopeEditDescription', icon: 'edit' },
  ];

  selectedScope = signal<ShareScope>('read');
  shareUrl      = signal<string | null>(null);
  loading       = signal(false);
  error         = signal<string | null>(null);
  copied        = signal(false);

  onScopeChange(scope: ShareScope): void {
    this.selectedScope.set(scope);
    this.shareUrl.set(null);
    this.error.set(null);
  }

  async generate(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      const response = await this.data.generate(this.selectedScope());
      this.shareUrl.set(response.shareUrl);
    } catch {
      this.error.set('counter.share.generateError');
    } finally {
      this.loading.set(false);
    }
  }

  copyLink(): void {
    const url = this.shareUrl();
    if (!url) return;
    void navigator.clipboard.writeText(url).then(() => {
      this.copied.set(true);
      setTimeout(() => this.copied.set(false), 2000);
    });
  }
}
