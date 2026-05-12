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
      <pts-loading-spinner [overlay]="true" size="lg" />
    } @else {
      <pts-nav-bar />
      <main class="max-w-5xl mx-auto px-4 py-6 min-h-[calc(100vh-3.5rem)]">
        <router-outlet />
      </main>
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
