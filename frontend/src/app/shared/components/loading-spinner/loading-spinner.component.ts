import { Component, input } from '@angular/core';

@Component({
  selector: 'pts-loading-spinner',
  template: `
    <div
      class="flex items-center justify-center"
      [class.fixed]="overlay()"
      [class.inset-0]="overlay()"
      [class.bg-surface/80]="overlay()"
      [class.z-50]="overlay()"
      role="status"
      aria-label="Loading"
    >
      <span
        class="inline-block rounded-full border-4 border-primary/20 border-t-primary animate-spin"
        [class]="sizeClass()"
      ></span>
    </div>
  `,
})
export class LoadingSpinnerComponent {
  size = input<'sm' | 'md' | 'lg'>('md');
  overlay = input(false);

  sizeClass() {
    const map = { sm: 'w-6 h-6', md: 'w-10 h-10', lg: 'w-16 h-16' };
    return map[this.size()];
  }
}
