import { Component, input } from '@angular/core';
import { SetIndicatorComponent } from '../set-indicator/set-indicator.component';

@Component({
  selector: 'pts-score-board',
  imports: [SetIndicatorComponent],
  template: `
    <div class="w-full">
      <!-- Set badge -->
      <div class="flex justify-center mb-4 sm:mb-5">
        <span class="pts-badge bg-surface-variant text-on-surface-muted">
          <span class="material-symbols-rounded text-sm">sports_score</span>
          Set {{ currentSet() }}
        </span>
      </div>

      <!-- Scores row — Team A column always uses team-a colour; physical
           order is flipped via flex-direction when sides have switched. -->
      <div
        class="flex items-center gap-2 sm:gap-4"
        [class.flex-row-reverse]="swap()"
      >
        <!-- Team A column -->
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
            [attr.aria-label]="teamAName() + ' score: ' + scoreA()"
          >{{ scoreA() }}</span>
        </div>

        <!-- Divider -->
        <div class="flex items-center justify-center pb-2 shrink-0 w-6 sm:w-8">
          <span class="text-2xl font-thin text-on-surface-muted select-none">:</span>
        </div>

        <!-- Team B column -->
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
            [attr.aria-label]="teamBName() + ' score: ' + scoreB()"
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
  // True when sides have been switched an odd number of times: render team B on the left.
  swap         = input(false);
}
