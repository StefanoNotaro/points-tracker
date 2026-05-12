import { Component, input, output } from '@angular/core';
import { NgClass } from '@angular/common';

@Component({
  selector: 'pts-score-button',
  imports: [NgClass],
  template: `
    <div class="flex flex-col items-center gap-1">
      <button
        type="button"
        class="w-16 h-16 rounded-full flex items-center justify-center text-2xl font-bold transition-all
               active:scale-95 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary
               disabled:opacity-40 disabled:cursor-not-allowed"
        [ngClass]="teamClass()"
        [disabled]="disabled()"
        (click)="increment.emit()"
        [attr.aria-label]="'Increment ' + label() + ' score'"
      >
        <span class="material-symbols-rounded text-3xl">add</span>
      </button>

      <button
        type="button"
        class="w-10 h-10 rounded-full flex items-center justify-center transition-all
               hover:bg-on-surface/8 active:scale-95 focus-visible:outline-2 focus-visible:outline-primary
               disabled:opacity-40 disabled:cursor-not-allowed text-on-surface-muted"
        [disabled]="disabled()"
        (click)="decrement.emit()"
        [attr.aria-label]="'Decrement ' + label() + ' score'"
      >
        <span class="material-symbols-rounded text-xl">remove</span>
      </button>
    </div>
  `,
})
export class ScoreButtonComponent {
  label = input.required<string>();
  team = input.required<'A' | 'B'>();
  disabled = input(false);

  increment = output<void>();
  decrement = output<void>();

  teamClass() {
    return this.team() === 'A'
      ? 'bg-team-a text-on-primary hover:opacity-90 shadow-elevated'
      : 'bg-team-b text-on-primary hover:opacity-90 shadow-elevated';
  }
}
