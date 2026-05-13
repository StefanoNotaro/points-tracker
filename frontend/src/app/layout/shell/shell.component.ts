import { Component, inject, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { NavBarComponent } from '../nav-bar/nav-bar.component';
import { ToastContainerComponent } from '../../shared/components/toast/toast.component';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner.component';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'pts-shell',
  imports: [RouterOutlet, NavBarComponent, ToastContainerComponent, LoadingSpinnerComponent],
  template: `
    @if (!auth.isInitialized()) {
      <div class="fixed inset-0 flex items-center justify-center bg-bg">
        <pts-loading-spinner size="lg" />
      </div>
    } @else {
      <div class="min-h-[100dvh] flex flex-col bg-bg text-on-surface">
        <pts-nav-bar />
        <main class="flex-1 w-full max-w-2xl mx-auto px-3 sm:px-4 py-4 sm:py-6 pb-[env(safe-area-inset-bottom)]">
          <router-outlet />
        </main>
      </div>
      <pts-toast-container />
    }
  `,
})
export class ShellComponent implements OnInit {
  readonly auth = inject(AuthService);

  async ngOnInit(): Promise<void> {
    await this.auth.initialize();
  }
}
