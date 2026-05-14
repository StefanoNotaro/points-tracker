import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { NavBarComponent } from '../nav-bar/nav-bar.component';
import { ToastContainerComponent } from '../../shared/components/toast/toast.component';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner.component';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'pts-shell',
  imports: [RouterOutlet, NavBarComponent, ToastContainerComponent, LoadingSpinnerComponent],
  templateUrl: './shell.component.html',
})
export class ShellComponent {
  // AuthService.initialize() is wired into provideAppInitializer so it has
  // already resolved by the time this component renders. The isInitialized
  // gate above is kept as a defensive fallback (e.g. if the initializer is
  // ever removed during a refactor).
  readonly auth = inject(AuthService);
}
