import { Component, inject } from '@angular/core';
import { NgClass } from '@angular/common';
import { NotificationService, Toast } from '../../../core/services/notification.service';

@Component({
  selector: 'pts-toast-container',
  imports: [NgClass],
  template: `
    <div class="fixed bottom-4 right-4 z-50 flex flex-col gap-2 max-w-sm w-full" aria-live="polite">
      @for (toast of notifications.toasts(); track toast.id) {
        <div
          class="flex items-start gap-3 rounded-lg px-4 py-3 shadow-elevated text-sm font-medium transition-all"
          [ngClass]="toastClass(toast)"
          role="alert"
        >
          <span class="material-symbols-rounded text-base leading-none mt-px">{{ toastIcon(toast) }}</span>
          <span class="flex-1">{{ toast.message }}</span>
          <button
            class="pts-btn-icon !w-6 !h-6"
            (click)="notifications.dismiss(toast.id)"
            aria-label="Dismiss notification"
          >
            <span class="material-symbols-rounded text-base">close</span>
          </button>
        </div>
      }
    </div>
  `,
})
export class ToastContainerComponent {
  readonly notifications = inject(NotificationService);

  toastClass(toast: Toast): Record<string, boolean> {
    return {
      'bg-success text-on-success': toast.type === 'success',
      'bg-error text-on-error': toast.type === 'error',
      'bg-primary text-on-primary': toast.type === 'info',
      'bg-warning text-on-warning': toast.type === 'warning',
    };
  }

  toastIcon(toast: Toast): string {
    const icons = { success: 'check_circle', error: 'error', info: 'info', warning: 'warning' };
    return icons[toast.type];
  }
}
