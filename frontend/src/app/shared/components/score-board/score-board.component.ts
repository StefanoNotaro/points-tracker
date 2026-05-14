import { Component, inject, input } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { SetIndicatorComponent } from '../set-indicator/set-indicator.component';

@Component({
  selector: 'pts-score-board',
  imports: [SetIndicatorComponent, TranslatePipe],
  template: `
    <div class="w-full">
      <div class="flex justify-center mb-4 sm:mb-5">
        <span class="pts-badge bg-surface-variant text-on-surface-muted">
          <span class="material-symbols-rounded text-sm">sports_score</span>
          {{ 'counter.score.setBadge' | translate: { n: currentSet() } }}
        </span>
      </div>

      <div
        class="flex items-center gap-2 sm:gap-4"
        [class.flex-row-reverse]="swap()"
      >
        <div class="flex-1 min-w-0 flex flex-col items-center gap-1.5">
          <p class="text-sm font-semibold text-on-surface truncate max-w-full px-1 text-center w-full">
            {{ teamAName() }}
          </p>
          <pts-set-indicator
            [setsWon]="setsWonA()"
            [totalSets]="totalSetsToWin()"
            [label]="teamAName()"
          />
          <span
            class="pts-score text-team-a"
            [attr.aria-label]="ariaScore(teamAName(), scoreA())"
          >{{ scoreA() }}</span>
        </div>

        <div class="flex items-center justify-center pb-2 shrink-0 w-6 sm:w-8">
          <span class="text-2xl font-thin text-on-surface-muted select-none">:</span>
        </div>

        <div class="flex-1 min-w-0 flex flex-col items-center gap-1.5">
          <p class="text-sm font-semibold text-on-surface truncate max-w-full px-1 text-center w-full">
            {{ teamBName() }}
          </p>
          <pts-set-indicator
            [setsWon]="setsWonB()"
            [totalSets]="totalSetsToWin()"
            [label]="teamBName()"
          />
          <span
            class="pts-score text-team-b"
            [attr.aria-label]="ariaScore(teamBName(), scoreB())"
          >{{ scoreB() }}</span>
        </div>
      </div>
    </div>
  `,
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
