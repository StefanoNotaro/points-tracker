import { Component, input } from '@angular/core';
import { NgClass } from '@angular/common';

@Component({
  selector: 'pts-set-indicator',
  imports: [NgClass],
  template: `
    <div class="flex gap-1.5 items-center" [attr.aria-label]="label() + ' sets won: ' + setsWon()">
      @for (i of setArray(); track i) {
        <div
          class="w-2.5 h-2.5 rounded-full transition-all duration-300"
          [ngClass]="i < setsWon() ? 'bg-primary scale-110' : 'bg-border'"
          aria-hidden="true"
        ></div>
      }
    </div>
  `,
})
export class SetIndicatorComponent {
  setsWon = input.required<number>();
  totalSets = input.required<number>();
  label = input('Team');

  setArray() {
    return Array.from({ length: this.totalSets() }, (_, i) => i);
  }
}
