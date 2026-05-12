import { Component, inject, input, OnInit, OnDestroy } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { ScoreBoardComponent } from '../../../../shared/components/score-board/score-board.component';
import { ScoreButtonComponent } from '../../../../shared/components/score-button/score-button.component';
import { TeamNameEditorComponent } from '../../../../shared/components/team-name-editor/team-name-editor.component';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import {
  ShareDialogComponent,
  ShareDialogData,
} from '../../../../shared/components/share-dialog/share-dialog.component';
import {
  ConfirmDialogComponent,
  ConfirmDialogData,
} from '../../../../shared/components/confirm-dialog/confirm-dialog.component';
import { CounterStore } from '../../store/counter.store';

@Component({
  selector: 'pts-counter-page',
  imports: [
    ScoreBoardComponent,
    ScoreButtonComponent,
    TeamNameEditorComponent,
    LoadingSpinnerComponent,
    MatButtonModule,
  ],
  providers: [CounterStore],
  template: `
    @if (store.isLoading()) {
      <pts-loading-spinner size="lg" [overlay]="true" />
    } @else if (store.loadState() === 'error') {
      <div class="flex flex-col items-center justify-center min-h-[60vh] gap-4 text-center">
        <span class="material-symbols-rounded text-6xl text-error">error</span>
        <p class="text-on-surface-muted">Counter not found or unavailable.</p>
      </div>
    } @else if (store.counter(); as counter) {
      <div class="flex flex-col gap-8">

        <!-- Header -->
        <div class="flex items-center justify-between">
          <div class="flex items-center gap-2">
            <span class="material-symbols-rounded text-primary">
              {{ store.sportConfig()?.icon }}
            </span>
            <span class="text-sm text-on-surface-muted">{{ store.sportConfig()?.label }}</span>
          </div>
          <div class="flex gap-1">
            <button
              type="button"
              class="pts-btn-icon"
              (click)="undo()"
              [disabled]="!counter.canEdit || store.actionPending()"
              aria-label="Undo last action"
            >
              <span class="material-symbols-rounded">undo</span>
            </button>
            <button
              type="button"
              class="pts-btn-icon"
              (click)="openShare(counter.id)"
              aria-label="Share counter"
            >
              <span class="material-symbols-rounded">share</span>
            </button>
          </div>
        </div>

        <!-- Score board -->
        <div class="pts-card">
          <pts-score-board
            [teamAName]="counter.teamAName"
            [teamBName]="counter.teamBName"
            [scoreA]="counter.currentScoreA"
            [scoreB]="counter.currentScoreB"
            [setsWonA]="counter.setsWonA"
            [setsWonB]="counter.setsWonB"
            [totalSetsToWin]="store.sportConfig()?.setsToWin ?? 3"
            [currentSet]="counter.currentSetNumber"
          />
        </div>

        <!-- Match status banner -->
        @if (counter.status !== 'active') {
          <div class="flex justify-center">
            <span class="text-sm font-semibold text-success bg-success/10 px-4 py-2 rounded-full">
              Match finished
            </span>
          </div>
        }

        <!-- Score buttons (active + editable only) -->
        @if (counter.canEdit && counter.status === 'active') {
          <div class="grid grid-cols-3 items-center gap-4">
            <div class="flex justify-center">
              <pts-score-button
                [label]="counter.teamAName"
                team="A"
                [disabled]="store.actionPending()"
                (increment)="store.incrementScore('A')"
                (decrement)="store.decrementScore('A')"
              />
            </div>

            <div class="flex justify-center">
              <span class="text-xs text-on-surface-muted">vs</span>
            </div>

            <div class="flex justify-center">
              <pts-score-button
                [label]="counter.teamBName"
                team="B"
                [disabled]="store.actionPending()"
                (increment)="store.incrementScore('B')"
                (decrement)="store.decrementScore('B')"
              />
            </div>
          </div>
        }

        <!-- Team name editors -->
        @if (counter.canEdit) {
          <div class="grid grid-cols-2 gap-4 text-center">
            <div class="flex justify-center">
              <pts-team-name-editor
                [teamName]="counter.teamAName"
                [canEdit]="counter.canEdit"
                (nameChanged)="store.updateTeamName('A', $event)"
              />
            </div>
            <div class="flex justify-center">
              <pts-team-name-editor
                [teamName]="counter.teamBName"
                [canEdit]="counter.canEdit"
                (nameChanged)="store.updateTeamName('B', $event)"
              />
            </div>
          </div>
        }

      </div>
    }
  `,
})
export class CounterPageComponent implements OnInit, OnDestroy {
  readonly id = input.required<string>();
  readonly store = inject(CounterStore);
  private readonly dialog = inject(MatDialog);

  async ngOnInit(): Promise<void> {
    await this.store.load(this.id());
  }

  ngOnDestroy(): void {
    this.store.ngOnDestroy();
  }

  openShare(counterId: string): void {
    this.dialog.open<ShareDialogComponent, ShareDialogData>(ShareDialogComponent, {
      data: { counterId },
      panelClass: 'pts-dialog',
    });
  }

  async undo(): Promise<void> {
    const confirmed = await this.dialog
      .open<ConfirmDialogComponent, ConfirmDialogData, boolean>(ConfirmDialogComponent, {
        data: { title: 'Undo last action', message: 'This will revert the last score change.' },
      })
      .afterClosed()
      .toPromise();

    if (confirmed) {
      await this.store.undo();
    }
  }
}
