import { Component, output, input } from '@angular/core';
import { NgClass } from '@angular/common';
import { SportConfig, SportType } from '../../models/sport.model';

@Component({
  selector: 'pts-sport-selector',
  imports: [NgClass],
  template: `
    <div class="grid grid-cols-1 gap-2" role="radiogroup" aria-label="Select sport">
      @for (sport of sports(); track sport.type) {
        <button
          type="button"
          role="radio"
          [attr.aria-checked]="selected() === sport.type"
          class="flex items-center gap-4 p-4 rounded-xl border-2 transition-all duration-150 text-left w-full"
          [ngClass]="selected() === sport.type
            ? 'border-primary bg-primary/8 text-on-surface'
            : 'border-border bg-surface-raised text-on-surface hover:border-primary/30 hover:bg-surface'"
          (click)="sportSelected.emit(sport.type)"
        >
          <!-- Icon -->
          <div
            class="w-12 h-12 rounded-xl flex items-center justify-center flex-shrink-0 transition-colors"
            [ngClass]="selected() === sport.type ? 'bg-primary/15' : 'bg-surface-variant'"
          >
            <span
              class="material-symbols-rounded text-3xl transition-colors"
              [ngClass]="selected() === sport.type ? 'text-primary' : 'text-on-surface-muted'"
            >{{ sport.icon }}</span>
          </div>

          <!-- Info -->
          <div class="flex-1 min-w-0">
            <p class="font-semibold text-sm">{{ sport.label }}</p>
            <p class="text-xs text-on-surface-muted mt-0.5">
              Best of {{ sport.totalSets }} sets · {{ sport.pointsPerSet }} pts to win
            </p>
          </div>

          <!-- Selected check -->
          @if (selected() === sport.type) {
            <span class="material-symbols-rounded text-primary text-xl flex-shrink-0">check_circle</span>
          }
        </button>
      }
    </div>
  `,
})
export class SportSelectorComponent {
  sports       = input.required<SportConfig[]>();
  selected     = input<SportType | null>(null);
  sportSelected = output<SportType>();
}
