import { Component, inject } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';

export interface ConfirmDialogData {
  title: string;
  message: string;
  confirmLabel?: string;
  cancelLabel?: string;
  destructive?: boolean;
}

@Component({
  selector: 'pts-confirm-dialog',
  imports: [MatDialogModule, MatButtonModule],
  template: `
    <div class="p-6 max-w-sm">
      <h2 class="text-lg font-semibold text-on-surface mb-2">{{ data.title }}</h2>
      <p class="text-on-surface-muted text-sm mb-6">{{ data.message }}</p>
      <div class="flex justify-end gap-3">
        <button mat-button (click)="cancel()">
          {{ data.cancelLabel ?? 'Cancel' }}
        </button>
        <button
          mat-flat-button
          [color]="data.destructive ? 'warn' : 'primary'"
          (click)="confirm()"
        >
          {{ data.confirmLabel ?? 'Confirm' }}
        </button>
      </div>
    </div>
  `,
})
export class ConfirmDialogComponent {
  readonly data = inject<ConfirmDialogData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<ConfirmDialogComponent>);

  confirm(): void {
    this.dialogRef.close(true);
  }

  cancel(): void {
    this.dialogRef.close(false);
  }
}
