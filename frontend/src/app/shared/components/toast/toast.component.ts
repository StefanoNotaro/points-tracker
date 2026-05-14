import { Component, inject } from '@angular/core';
import { NgClass } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { NotificationService, Toast } from '../../../core/services/notification.service';

@Component({
  selector: 'pts-toast-container',
  imports: [NgClass, TranslatePipe],
  templateUrl: './toast.component.html',
})
export class ToastContainerComponent {
  readonly notifications = inject(NotificationService);

  toastClass(toast: Toast): Record<string, boolean> {
    return {
      'bg-success text-on-success': toast.type === 'success',
      'bg-error text-on-error':     toast.type === 'error',
      'bg-primary text-on-primary': toast.type === 'info',
      'bg-warning text-on-warning': toast.type === 'warning',
    };
  }

  toastIcon(toast: Toast): string {
    const map: Record<Toast['type'], string> = {
      success: 'check_circle',
      error:   'error',
      info:    'info',
      warning: 'warning',
    };
    return map[toast.type];
  }
}
