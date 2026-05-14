import { Component, input } from '@angular/core';

@Component({
  selector: 'pts-loading-spinner',
  templateUrl: './loading-spinner.component.html',
})
export class LoadingSpinnerComponent {
  size = input<'sm' | 'md' | 'lg'>('md');
  overlay = input(false);

  sizeClass() {
    const map = { sm: 'w-6 h-6', md: 'w-10 h-10', lg: 'w-16 h-16' };
    return map[this.size()];
  }
}
