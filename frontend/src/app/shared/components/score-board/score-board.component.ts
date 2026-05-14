import { Component, inject, input } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { SetIndicatorComponent } from '../set-indicator/set-indicator.component';

@Component({
  selector: 'pts-score-board',
  imports: [SetIndicatorComponent, TranslatePipe],
  templateUrl: './score-board.component.html',
})
export class ScoreBoardComponent {
  teamAName    = input.required<string>();
  teamBName    = input.required<string>();
  scoreA       = input.required<number>();
  scoreB       = input.required<number>();
  setsWonA     = input.required<number>();
  setsWonB     = input.required<number>();
  totalSetsToWin = input.required<number>();
  currentSet   = input.required<number>();
  swap         = input(false);

  private readonly i18n = inject(TranslateService);

  ariaScore(team: string, value: number): string {
    return this.i18n.instant('counter.score.ariaScore', { team, value });
  }
}
