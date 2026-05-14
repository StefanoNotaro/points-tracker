import { Component, inject, input, output } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'pts-score-button',
  templateUrl: './score-button.component.html',
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
