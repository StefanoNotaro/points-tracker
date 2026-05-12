import { Component, input } from '@angular/core';
import { NgClass } from '@angular/common';

@Component({
  selector: 'pts-set-indicator',
  imports: [NgClass],
  template: `
    <div class="flex gap-1.5" [attr.aria-label]="label() + ' sets won: ' + setsWon()">
      @for (i of setArray(); track i) {
        <div
          class="w-4 h-4 rounded-full border-2 transition-all"
          [ngClass]="
            i < setsWon()
              ? 'bg-primary border-primary'
              : 'bg-transparent border-border'
          "
          [attr.aria-hidden]="true"
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
