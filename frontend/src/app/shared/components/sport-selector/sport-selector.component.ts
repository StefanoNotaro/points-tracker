import { Component, output, input } from '@angular/core';
import { NgClass } from '@angular/common';
import { SportConfig, SportType } from '../../models/sport.model';

@Component({
  selector: 'pts-sport-selector',
  imports: [NgClass],
  template: `
    <div class="flex flex-col gap-2" role="radiogroup" aria-label="Select sport">
      @for (sport of sports(); track sport.type) {
        <button
          type="button"
          role="radio"
          [attr.aria-checked]="selected() === sport.type"
          class="flex items-center gap-4 p-4 rounded-lg border-2 transition-all text-left"
          [ngClass]="
            selected() === sport.type
              ? 'border-primary bg-primary/8 text-primary'
              : 'border-border bg-surface text-on-surface hover:border-primary/40'
          "
          (click)="sportSelected.emit(sport.type)"
        >
          <span class="material-symbols-rounded text-3xl">{{ sport.icon }}</span>
          <div>
            <p class="font-semibold">{{ sport.label }}</p>
            <p class="text-sm text-on-surface-muted">
              Best of {{ sport.totalSets }} · {{ sport.pointsPerSet }} pts/set
            </p>
          </div>
        </button>
      }
    </div>
  `,
})
export class SportSelectorComponent {
  sports = input.required<SportConfig[]>();
  selected = input<SportType | null>(null);
  sportSelected = output<SportType>();
}
