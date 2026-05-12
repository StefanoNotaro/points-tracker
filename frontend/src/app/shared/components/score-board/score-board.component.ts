import { Component, input } from '@angular/core';
import { SetIndicatorComponent } from '../set-indicator/set-indicator.component';

@Component({
  selector: 'pts-score-board',
  imports: [SetIndicatorComponent],
  template: `
    <div class="grid grid-cols-[1fr_auto_1fr] items-center gap-4 w-full">
      <!-- Team A -->
      <div class="flex flex-col items-center gap-2">
        <p class="text-sm font-medium text-on-surface-muted uppercase tracking-wide truncate max-w-full">
          {{ teamAName() }}
        </p>
        <span
          class="pts-score-display text-team-a"
          [attr.aria-label]="teamAName() + ' score: ' + scoreA()"
        >{{ scoreA() }}</span>
        <pts-set-indicator
          [setsWon]="setsWonA()"
          [totalSets]="totalSetsToWin()"
          [label]="teamAName()"
        />
      </div>

      <!-- Divider -->
      <div class="flex flex-col items-center gap-1 px-2">
        <span class="text-2xl font-light text-on-surface-muted">:</span>
        <span class="text-xs text-on-surface-muted">Set {{ currentSet() }}</span>
      </div>

      <!-- Team B -->
      <div class="flex flex-col items-center gap-2">
        <p class="text-sm font-medium text-on-surface-muted uppercase tracking-wide truncate max-w-full">
          {{ teamBName() }}
        </p>
        <span
          class="pts-score-display text-team-b"
          [attr.aria-label]="teamBName() + ' score: ' + scoreB()"
        >{{ scoreB() }}</span>
        <pts-set-indicator
          [setsWon]="setsWonB()"
          [totalSets]="totalSetsToWin()"
          [label]="teamBName()"
        />
      </div>
    </div>
  `,
})
export class ScoreBoardComponent {
  teamAName = input.required<string>();
  teamBName = input.required<string>();
  scoreA = input.required<number>();
  scoreB = input.required<number>();
  setsWonA = input.required<number>();
  setsWonB = input.required<number>();
  totalSetsToWin = input.required<number>();
  currentSet = input.required<number>();
}
