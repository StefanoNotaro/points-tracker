import { Component, inject, OnInit, signal, OnDestroy } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';

export interface ConfirmDialogData {
  title:         string;
  message:       string;
  confirmLabel?: string;
  cancelLabel?:  string;
  destructive?:  boolean;
  /** When true, only the confirm button is rendered (use for info-style dialogs). */
  hideCancel?:   boolean;
  /**
   * When set, the dialog auto-dismisses after this many seconds and shows a
   * countdown to the user. Used for transient info dialogs.
   */
  autoDismissSeconds?: number;
}

@Component({
  selector: 'pts-confirm-dialog',
  imports: [MatDialogModule],
  template: `
    <div class="p-6 w-full max-w-xs">

      <!-- Icon -->
      <div class="flex justify-center mb-4">
        <div
          class="w-12 h-12 rounded-2xl flex items-center justify-center"
          [class]="data.destructive ? 'bg-error/10' : 'bg-primary/10'"
        >
          <span
            class="material-symbols-rounded text-2xl"
            [class]="data.destructive ? 'text-error' : 'text-primary'"
          >{{ data.destructive ? 'warning' : 'help' }}</span>
        </div>
      </div>

      <!-- Text -->
      <h2 class="text-base font-bold text-on-surface text-center mb-2">{{ data.title }}</h2>
      <p class="text-sm text-on-surface-muted text-center mb-4 leading-relaxed">{{ data.message }}</p>

      <!-- Countdown -->
      @if (data.autoDismissSeconds && remainingSeconds() > 0) {
        <p class="text-xs text-on-surface-muted text-center mb-4">
          Closing in {{ remainingSeconds() }}s
        </p>
      }

      <!-- Actions -->
      <div class="flex flex-col gap-2">
        <button
          class="w-full py-2.5 rounded-xl font-semibold text-sm transition-all active:scale-[0.97]"
          [class]="data.destructive
            ? 'bg-error text-on-error hover:opacity-90'
            : 'pts-btn-primary'"
          (click)="confirm()"
        >
          {{ data.confirmLabel ?? 'Confirm' }}
        </button>
        @if (!data.hideCancel) {
          <button
            class="w-full py-2.5 rounded-xl font-medium text-sm text-on-surface-muted
                   hover:bg-surface-variant transition-colors"
            (click)="cancel()"
          >
            {{ data.cancelLabel ?? 'Cancel' }}
          </button>
        }
      </div>
    </div>
  `,
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
