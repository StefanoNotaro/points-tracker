import { Component, inject, OnInit, signal, OnDestroy } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { TranslatePipe } from '@ngx-translate/core';

/**
 * Callers pass already-localized strings — this dialog is intentionally
 * locale-agnostic so it can render any wording chosen by the screen that
 * opened it. Only the default Confirm/Cancel button labels are localized
 * here, used as fallback when the caller doesn't supply explicit ones.
 */
export interface ConfirmDialogData {
  title:         string;
  message:       string;
  confirmLabel?: string;
  cancelLabel?:  string;
  destructive?:  boolean;
  hideCancel?:   boolean;
  autoDismissSeconds?: number;
}

@Component({
  selector: 'pts-confirm-dialog',
  imports: [MatDialogModule, TranslatePipe],
  templateUrl: './confirm-dialog.component.html',
})
export class ConfirmDialogComponent implements OnInit, OnDestroy {
  readonly data      = inject<ConfirmDialogData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<ConfirmDialogComponent>);

  readonly remainingSeconds = signal(0);
  private intervalId: ReturnType<typeof setInterval> | null = null;

  ngOnInit(): void {
    if (!this.data.autoDismissSeconds) return;
    this.remainingSeconds.set(this.data.autoDismissSeconds);
    this.intervalId = setInterval(() => {
      const next = this.remainingSeconds() - 1;
      this.remainingSeconds.set(next);
      if (next <= 0) {
        this.clearTimer();
        this.dialogRef.close(true);
      }
    }, 1000);
  }

  ngOnDestroy(): void {
    this.clearTimer();
  }

  confirm(): void { this.clearTimer(); this.dialogRef.close(true); }
  cancel():  void { this.clearTimer(); this.dialogRef.close(false); }

  private clearTimer(): void {
    if (this.intervalId !== null) {
      clearInterval(this.intervalId);
      this.intervalId = null;
    }
  }
}
