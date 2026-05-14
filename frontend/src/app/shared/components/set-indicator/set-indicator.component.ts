import { Component, inject, input } from '@angular/core';
import { NgClass } from '@angular/common';
import { TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'pts-set-indicator',
  imports: [NgClass],
  templateUrl: './set-indicator.component.html',
})
export class SetIndicatorComponent {
  setsWon = input.required<number>();
  totalSets = input.required<number>();
  label = input('Team');

  private readonly i18n = inject(TranslateService);

  ariaLabel(): string {
    return this.i18n.instant('counter.score.setsAria', { team: this.label(), won: this.setsWon() });
  }

  setArray() {
    return Array.from({ length: this.totalSets() }, (_, i) => i);
  }
}
