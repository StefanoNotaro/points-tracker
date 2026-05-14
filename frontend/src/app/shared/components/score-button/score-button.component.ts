import { Component, inject, input, output } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'pts-score-button',
  template: `
    <div class="flex flex-col items-center gap-2 w-full">
      <button
        type="button"
        class="w-full h-24 sm:h-28 rounded-2xl flex flex-col items-center justify-center gap-1
               font-bold text-white transition-all duration-150
               active:scale-[0.97] focus-visible:outline-2 focus-visible:outline-offset-2
               disabled:opacity-40 disabled:cursor-not-allowed select-none"
        [class]="teamBtnClass()"
        [disabled]="disabled()"
        (click)="increment.emit()"
        [attr.aria-label]="pointLabel()"
      >
        <span class="material-symbols-rounded text-4xl leading-none">add</span>
        <span class="text-xs font-semibold opacity-90 truncate max-w-full px-3">
          {{ label() }}
        </span>
      </button>

      <button
        type="button"
        class="w-11 h-11 rounded-full flex items-center justify-center
               text-on-surface-muted border border-border bg-surface
               transition-all duration-150 hover:border-on-surface-muted hover:text-on-surface
               active:scale-90 focus-visible:outline-primary focus-visible:outline-1 focus-visible:outline-offset-1
               disabled:opacity-40 disabled:cursor-not-allowed select-none"
        [disabled]="disabled()"
        (click)="decrement.emit()"
        [attr.aria-label]="removePointLabel()"
        [attr.title]="removePointLabel()"
      >
        <span class="material-symbols-rounded text-xl leading-none">remove</span>
      </button>
    </div>
  `,
})
export class ScoreButtonComponent {
  label    = input.required<string>();
  team     = input.required<'A' | 'B'>();
  disabled = input(false);

  increment = output<void>();
  decrement = output<void>();

  private readonly i18n = inject(TranslateService);

  pointLabel(): string {
    return this.i18n.instant('counter.page.pointAria', { label: this.label() });
  }
  removePointLabel(): string {
    return this.i18n.instant('counter.page.removePointAria', { label: this.label() });
  }

  teamBtnClass(): string {
    const shadow = 'shadow-elevated';
    return this.team() === 'A'
      ? `bg-team-a hover:opacity-90 focus-visible:outline-team-a ${shadow}`
      : `bg-team-b hover:opacity-90 focus-visible:outline-team-b ${shadow}`;
  }
}
