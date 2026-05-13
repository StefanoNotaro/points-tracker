import { Component, inject, input, signal, computed, OnInit, OnDestroy, effect } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { MatDialog } from '@angular/material/dialog';
import { MatMenuModule } from '@angular/material/menu';
import { AuthService } from '../../../../core/auth/auth.service';
import { ScoreBoardComponent } from '../../../../shared/components/score-board/score-board.component';
import { ScoreButtonComponent } from '../../../../shared/components/score-button/score-button.component';
import { TeamNameEditorComponent } from '../../../../shared/components/team-name-editor/team-name-editor.component';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { EventsLogComponent } from '../events-log/events-log.component';
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
    EventsLogComponent,
    MatMenuModule,
    RouterLink,
  ],
  providers: [CounterStore],
  template: `
    <!-- Loading -->
    @if (store.isLoading()) {
      <div class="flex items-center justify-center min-h-[60vh]">
        <pts-loading-spinner size="lg" />
      </div>

    <!-- Error -->
    } @else if (store.loadState() === 'error') {
      <div class="flex flex-col items-center justify-center min-h-[60vh] gap-4 text-center px-4">
        <span class="material-symbols-rounded text-7xl text-error">error_outline</span>
        <h2 class="text-xl font-bold text-on-surface">Counter not found</h2>
        <p class="text-on-surface-muted text-sm">This counter doesn't exist or you don't have access.</p>
      </div>

    <!-- Counter loaded -->
    } @else if (store.counter(); as counter) {
      <div class="flex flex-col gap-4 sm:gap-5">

        <!-- Tournament link bar — only when this counter was spawned for a match -->
        @if (counter.linkedTournament; as link) {
          <a [routerLink]="['/tournaments', link.tournamentId]"
             class="pts-card !p-2.5 flex items-center gap-2 hover:border-primary transition-colors">
            <span class="material-symbols-rounded text-primary text-xl shrink-0">emoji_events</span>
            <span class="flex-1 min-w-0">
              <span class="block text-xs uppercase tracking-wide text-on-surface-muted">Tournament match</span>
              <span class="block text-sm font-semibold text-on-surface truncate">{{ link.tournamentName }}</span>
            </span>
            <span class="material-symbols-rounded text-on-surface-muted">chevron_right</span>
          </a>
        }

        <!-- Top bar: small sport chip + action menu -->
        <div class="flex items-center justify-between gap-2">
          <div class="inline-flex items-center gap-1.5 text-on-surface-muted">
            <span class="material-symbols-rounded text-base text-primary">
              {{ store.sportConfig()?.icon }}
            </span>
            <span class="text-xs font-medium uppercase tracking-wide">
              {{ store.sportConfig()?.label }}
            </span>
          </div>

          <div class="flex items-center gap-0.5">
            @if (counter.canEdit) {
              <button
                type="button"
                class="pts-btn-icon"
                [disabled]="!counter.canUndo || store.actionPending()"
                (click)="undoOnce()"
                (pointerdown)="startHoldUndo($event)"
                (pointerup)="stopHold()"
                (pointerleave)="stopHold()"
                (pointercancel)="stopHold()"
                aria-label="Undo last action (hold to repeat)"
                title="Undo (hold to undo more)"
              >
                <span class="material-symbols-rounded text-xl">undo</span>
              </button>
              @if (counter.canRedo) {
                <button
                  type="button"
                  class="pts-btn-icon"
                  [disabled]="store.actionPending()"
                  (click)="redoOnce()"
                  (pointerdown)="startHoldRedo($event)"
                  (pointerup)="stopHold()"
                  (pointerleave)="stopHold()"
                  (pointercancel)="stopHold()"
                  aria-label="Redo (hold to repeat)"
                  title="Redo (hold to redo more)"
                >
                  <span class="material-symbols-rounded text-xl">redo</span>
                </button>
              }
            }
            <button
              type="button"
              class="pts-btn-icon"
              [matMenuTriggerFor]="moreMenu"
              aria-label="More options"
              title="More"
            >
              <span class="material-symbols-rounded text-xl">more_vert</span>
            </button>

            <mat-menu #moreMenu="matMenu">
              @if (canShare()) {
                <button mat-menu-item (click)="openShare(counter.id)">
                  <span class="material-symbols-rounded mr-2 text-base align-middle">share</span>
                  Share
                </button>
              }
              @if (counter.canEdit) {
                <button mat-menu-item (click)="toggleEditTeams()">
                  <span class="material-symbols-rounded mr-2 text-base align-middle">edit</span>
                  {{ editingTeams() ? 'Hide team editor' : 'Rename teams' }}
                </button>
              }
              <button mat-menu-item (click)="toggleMatchInfo()">
                <span class="material-symbols-rounded mr-2 text-base align-middle">info</span>
                {{ showMatchInfo() ? 'Hide match info' : 'Match info' }}
              </button>
              @if (counter.canEdit && counter.status === 'active') {
                <button mat-menu-item (click)="switchSidesManually()">
                  <span class="material-symbols-rounded mr-2 text-base align-middle">swap_horiz</span>
                  Switch sides
                </button>
                <button mat-menu-item (click)="confirmEndMatch()">
                  <span class="material-symbols-rounded mr-2 text-base align-middle">stop_circle</span>
                  End match now
                </button>
              }
              @if (counter.isOwner) {
                <button mat-menu-item (click)="confirmDelete()" class="!text-error">
                  <span class="material-symbols-rounded mr-2 text-base align-middle">delete</span>
                  Delete counter
                </button>
              }
            </mat-menu>
          </div>
        </div>

        <!-- Score board card -->
        <div class="pts-card">
          <pts-score-board
            [teamAName]="counter.teamAName"
            [teamBName]="counter.teamBName"
            [scoreA]="counter.currentScoreA"
            [scoreB]="counter.currentScoreB"
            [setsWonA]="counter.setsWonA"
            [setsWonB]="counter.setsWonB"
            [totalSetsToWin]="counter.rules.setsToWin"
            [currentSet]="counter.currentSetNumber"
            [swap]="swapSides()"
          />
          @if (swapSides()) {
            <div class="mt-2 flex justify-center">
              <span class="pts-badge bg-surface-variant text-on-surface-muted text-[11px]">
                <span class="material-symbols-rounded text-sm">swap_horiz</span>
                Sides switched
              </span>
            </div>
          }
        </div>

        <!-- Match finished banner -->
        @if (counter.status !== 'active') {
          <div class="flex items-center justify-center gap-2 rounded-2xl py-3
                      bg-success/10 border border-success/20 text-success text-sm font-semibold">
            <span class="material-symbols-rounded">emoji_events</span>
            <span>Match finished</span>
          </div>
        }

        <!-- Score buttons (active + editable) - big, thumb-reachable -->
        @if (counter.canEdit && counter.status === 'active') {
          <div class="flex gap-3 sm:gap-4" [class.flex-row-reverse]="swapSides()">
            <pts-score-button
              class="flex-1 min-w-0"
              [label]="counter.teamAName"
              team="A"
              [disabled]="store.actionPending()"
              (increment)="store.incrementScore('A')"
              (decrement)="store.decrementScore('A')"
            />
            <pts-score-button
              class="flex-1 min-w-0"
              [label]="counter.teamBName"
              team="B"
              [disabled]="store.actionPending()"
              (increment)="store.incrementScore('B')"
              (decrement)="store.decrementScore('B')"
            />
          </div>
        }

        <!-- Timeouts: per-team button with remaining indicator -->
        @if (counter.status === 'active' && counter.rules.timeoutsPerSet > 0) {
          <div class="flex gap-3 sm:gap-4" [class.flex-row-reverse]="swapSides()">
            <button
              type="button"
              class="pts-card flex-1 min-w-0 flex items-center justify-between gap-2
                     px-3 py-2 text-left hover:bg-surface-variant/40
                     disabled:opacity-50 disabled:hover:bg-surface"
              [disabled]="!counter.canEdit || counter.timeoutsRemainingA === 0 || store.actionPending() || timeoutActive()"
              (click)="callTimeout('A')"
              [attr.aria-label]="'Call timeout for ' + counter.teamAName"
            >
              <span class="flex items-center gap-2 min-w-0">
                <span class="material-symbols-rounded text-base text-team-a">pause_circle</span>
                <span class="text-xs uppercase tracking-wide text-on-surface-muted truncate">
                  Timeout
                </span>
              </span>
              <span class="pts-badge bg-team-a/10 text-team-a text-[11px] font-mono">
                {{ counter.timeoutsRemainingA }}/{{ counter.rules.timeoutsPerSet }}
              </span>
            </button>
            <button
              type="button"
              class="pts-card flex-1 min-w-0 flex items-center justify-between gap-2
                     px-3 py-2 text-left hover:bg-surface-variant/40
                     disabled:opacity-50 disabled:hover:bg-surface"
              [disabled]="!counter.canEdit || counter.timeoutsRemainingB === 0 || store.actionPending() || timeoutActive()"
              (click)="callTimeout('B')"
              [attr.aria-label]="'Call timeout for ' + counter.teamBName"
            >
              <span class="flex items-center gap-2 min-w-0">
                <span class="material-symbols-rounded text-base text-team-b">pause_circle</span>
                <span class="text-xs uppercase tracking-wide text-on-surface-muted truncate">
                  Timeout
                </span>
              </span>
              <span class="pts-badge bg-team-b/10 text-team-b text-[11px] font-mono">
                {{ counter.timeoutsRemainingB }}/{{ counter.rules.timeoutsPerSet }}
              </span>
            </button>
          </div>
        }

        <!-- Active timeout countdown banner — visible to every connected client -->
        @if (timeoutActive(); as active) {
          <div class="flex items-center justify-between gap-3 rounded-2xl px-4 py-3
                      bg-warning/10 border border-warning/30 text-warning">
            <span class="flex items-center gap-2 text-sm font-semibold min-w-0">
              <span class="material-symbols-rounded shrink-0">pause_circle</span>
              <span class="truncate">
                {{ active.team === 'A' ? counter.teamAName : counter.teamBName }} timeout
              </span>
            </span>
            <span class="flex items-center gap-2 shrink-0">
              <span class="font-mono text-lg tabular-nums">{{ active.remaining }}s</span>
              @if (counter.canEdit) {
                <button
                  type="button"
                  class="pts-btn-icon h-8 w-8"
                  [disabled]="store.actionPending()"
                  (click)="cancelTimeout()"
                  aria-label="Cancel timeout"
                  title="Cancel timeout"
                >
                  <span class="material-symbols-rounded text-lg">close</span>
                </button>
              }
            </span>
          </div>
        }

        <!-- View-only chip (small, unobtrusive) -->
        @if (!counter.canEdit) {
          <div class="flex justify-center">
            <span class="pts-badge bg-surface-variant text-on-surface-muted">
              <span class="material-symbols-rounded text-sm">visibility</span>
              View only
            </span>
          </div>
        }

        <!-- Team name editor — only when toggled from the menu -->
        @if (counter.canEdit && editingTeams()) {
          <div class="pts-card flex flex-col gap-3">
            <div class="flex items-center justify-between">
              <p class="pts-label">Team names</p>
              <button
                type="button"
                class="pts-btn-icon h-8 w-8"
                (click)="editingTeams.set(false)"
                aria-label="Close team editor"
              >
                <span class="material-symbols-rounded text-lg">close</span>
              </button>
            </div>
            <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
              <div class="flex items-center gap-2">
                <span class="inline-block w-2 h-2 rounded-full bg-team-a shrink-0"></span>
                <pts-team-name-editor
                  [teamName]="counter.teamAName"
                  [canEdit]="counter.canEdit"
                  (nameChanged)="store.updateTeamName('A', $event)"
                />
              </div>
              <div class="flex items-center gap-2">
                <span class="inline-block w-2 h-2 rounded-full bg-team-b shrink-0"></span>
                <pts-team-name-editor
                  [teamName]="counter.teamBName"
                  [canEdit]="counter.canEdit"
                  (nameChanged)="store.updateTeamName('B', $event)"
                />
              </div>
            </div>
          </div>
        }

        <!-- Match info — only when toggled -->
        @if (showMatchInfo()) {
          <div class="pts-card flex flex-col gap-2 text-xs text-on-surface-muted">
            <div class="flex justify-between">
              <span>Points per set</span>
              <span class="font-mono text-on-surface">{{ counter.rules.pointsPerSet }}</span>
            </div>
            <div class="flex justify-between">
              <span>Last-set points</span>
              <span class="font-mono text-on-surface">{{ counter.rules.lastSetPoints }}</span>
            </div>
            <div class="flex justify-between">
              <span>Sets to win</span>
              <span class="font-mono text-on-surface">{{ counter.rules.setsToWin }} of {{ counter.rules.totalSets }}</span>
            </div>
            <div class="flex justify-between">
              <span>Must win by 2</span>
              <span class="font-mono text-on-surface">{{ counter.rules.winByTwo ? 'Yes' : 'No' }}</span>
            </div>
          </div>
        }

        <!-- Events log — collapsed by default -->
        <pts-events-log
          [events]="counter.events"
          [teamAName]="counter.teamAName"
          [teamBName]="counter.teamBName"
        />

      </div>
    }
  `,
})
export class CounterPageComponent implements OnInit, OnDestroy {
  readonly id    = input.required<string>();
  readonly store = inject(CounterStore);
  private readonly dialog = inject(MatDialog);
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);

  // Sharing is owner-only: share-link visitors and read-only viewers should
  // not be able to re-distribute access from this UI.
  readonly canShare = computed(() => this.store.counter()?.isOwner === true);

  // Progressive disclosure — both off by default.
  readonly editingTeams = signal(false);
  readonly showMatchInfo = signal(false);

  // Server-driven timeout countdown. The DTO carries activeTimeout = { team,
  // startedAt, durationSeconds } whenever a timeout is currently running, so
  // every subscribed client (this browser, other browsers, even read-only
  // viewers) sees the same countdown from the same reference time.
  //
  // `now` ticks every second while a timeout is active so the template
  // re-evaluates the remaining seconds.
  private readonly now = signal(Date.now());
  private nowInterval: ReturnType<typeof setInterval> | null = null;
  readonly timeoutActive = computed(() => {
    const t = this.store.counter()?.activeTimeout;
    if (!t) return null;
    const startedMs = new Date(t.startedAt).getTime();
    const elapsed = Math.floor((this.now() - startedMs) / 1000);
    const remaining = Math.max(0, t.durationSeconds - elapsed);
    if (remaining <= 0) return null;
    return { team: t.team, remaining };
  });

  // True when teams have switched sides an odd number of times — used to
  // mirror the scoreboard and the score buttons so the team on the left of
  // the screen matches the team on the left of the court.
  readonly swapSides = computed(() => {
    const c = this.store.counter();
    return !!c && c.sideSwitchCount % 2 === 1;
  });

  // Guard so the indoor side-switch dialog only opens once per pending state transition.
  private sideSwitchDialogOpen = false;
  // Track the count we last observed so we can detect beach auto-switches.
  private lastObservedSwitchCount: number | null = null;

  // Press-and-hold state for undo/redo.
  private holdTimer: ReturnType<typeof setInterval> | null = null;
  private holdInitialTimer: ReturnType<typeof setTimeout> | null = null;
  private holdSuppressClick = false;
  // First click fires immediately; if the user keeps holding, repeat after a
  // short delay so a normal tap doesn't accidentally fire two undos.
  private readonly HOLD_DELAY_MS = 400;
  private readonly HOLD_INTERVAL_MS = 180;

  constructor() {
    // Either a successful local delete, or a remote delete relayed by SignalR
    // (e.g. owner deletes from the dashboard while a viewer is on this page),
    // navigates away to a sensible landing page.
    effect(() => {
      if (this.store.counterDeleted()) {
        this.router.navigate([this.auth.isAuthenticated() ? '/dashboard' : '/new-counter']);
      }
    });

    // Tick the `now` signal once per second only while a timeout is running,
    // and stop when it ends — saves a permanent 1Hz interval on every counter
    // page.
    effect(() => {
      const active = this.store.counter()?.activeTimeout;
      if (active) this.startNowTicker();
      else this.stopNowTicker();
    });

    effect(() => {
      const counter = this.store.counter();
      if (!counter) return;

      // 1. Pending confirmation — used by indoor (every set) and by beach when
      //    auto-switch is OFF. The dialog wording is tweaked per sport.
      if (counter.canEdit && counter.pendingSideSwitchConfirmation) {
        if (!this.sideSwitchDialogOpen) {
          this.sideSwitchDialogOpen = true;
          this.openConfirmSwitchDialog(counter.rules.sideSwitchMode);
        }
      } else {
        this.sideSwitchDialogOpen = false;
      }

      // 2. Beach (auto): the server has just bumped sideSwitchCount. Show an
      //    info dialog that auto-dismisses after 5 seconds. Skip on the very
      //    first observation so loading an existing counter doesn't pop one.
      if (this.lastObservedSwitchCount !== null &&
          counter.sideSwitchCount > this.lastObservedSwitchCount &&
          counter.rules.sideSwitchMode === 'autoeverypoints' &&
          counter.beachAutoSwitchSides) {
        this.openSideSwitchInfoDialog();
      }
      this.lastObservedSwitchCount = counter.sideSwitchCount;
    });
  }

  async ngOnInit(): Promise<void> {
    await this.store.load(this.id());
  }

  ngOnDestroy(): void {
    this.clearHoldTimers();
    this.stopNowTicker();
    this.store.ngOnDestroy();
  }

  toggleEditTeams(): void {
    this.editingTeams.update((v) => !v);
  }

  toggleMatchInfo(): void {
    this.showMatchInfo.update((v) => !v);
  }

  openShare(counterId: string): void {
    this.dialog.open<ShareDialogComponent, ShareDialogData>(ShareDialogComponent, {
      data: { counterId },
      panelClass: 'pts-dialog',
    });
  }

  undoOnce(): void {
    // pointerdown -> pointerup also synthesizes a click. After a hold burst
    // we want to ignore that click; this flag is set when the hold-repeat
    // path actually fired.
    if (this.holdSuppressClick) {
      this.holdSuppressClick = false;
      return;
    }
    void this.store.undo(1);
  }

  redoOnce(): void {
    if (this.holdSuppressClick) {
      this.holdSuppressClick = false;
      return;
    }
    void this.store.redo(1);
  }

  startHoldUndo(ev: PointerEvent): void {
    // Only react to primary button / touch / pen.
    if (ev.pointerType === 'mouse' && ev.button !== 0) return;
    this.beginHold(() => this.store.undo(1));
  }

  startHoldRedo(ev: PointerEvent): void {
    if (ev.pointerType === 'mouse' && ev.button !== 0) return;
    this.beginHold(() => this.store.redo(1));
  }

  stopHold(): void {
    this.clearHoldTimers();
  }

  switchSidesManually(): void {
    void this.store.switchSidesManually();
  }

  async confirmEndMatch(): Promise<void> {
    const counter = this.store.counter();
    if (!counter) return;
    const confirmed = await this.dialog
      .open<ConfirmDialogComponent, ConfirmDialogData, boolean>(ConfirmDialogComponent, {
        data: {
          title: 'End match now?',
          message:
            'The match will be marked finished. The winner will be the team with more sets — or, on a tie, more total points.',
          confirmLabel: 'End match',
        },
      })
      .afterClosed()
      .toPromise();
    if (confirmed) await this.store.endMatch();
  }

  async confirmDelete(): Promise<void> {
    const counter = this.store.counter();
    if (!counter) return;
    const confirmed = await this.dialog
      .open<ConfirmDialogComponent, ConfirmDialogData, boolean>(ConfirmDialogComponent, {
        data: {
          title: 'Delete this counter?',
          message: `"${counter.teamAName} vs ${counter.teamBName}" and its history will be removed. This can't be undone.`,
          confirmLabel: 'Delete',
        },
      })
      .afterClosed()
      .toPromise();
    if (confirmed) await this.store.deleteCurrent();
  }

  async callTimeout(team: 'A' | 'B'): Promise<void> {
    await this.store.callTimeout(team);
    // The countdown is now driven from store.counter().activeTimeout, which
    // every subscribed client receives via SignalR. No local timer to seed.
  }

  async cancelTimeout(): Promise<void> {
    await this.store.cancelTimeout();
  }

  private startNowTicker(): void {
    if (this.nowInterval) return;
    this.nowInterval = setInterval(() => this.now.set(Date.now()), 1000);
  }

  private stopNowTicker(): void {
    if (this.nowInterval) {
      clearInterval(this.nowInterval);
      this.nowInterval = null;
    }
  }

  private beginHold(action: () => Promise<void>): void {
    this.clearHoldTimers();
    this.holdInitialTimer = setTimeout(() => {
      this.holdSuppressClick = true;
      void action();
      this.holdTimer = setInterval(() => {
        void action();
      }, this.HOLD_INTERVAL_MS);
    }, this.HOLD_DELAY_MS);
  }

  private clearHoldTimers(): void {
    if (this.holdInitialTimer) {
      clearTimeout(this.holdInitialTimer);
      this.holdInitialTimer = null;
    }
    if (this.holdTimer) {
      clearInterval(this.holdTimer);
      this.holdTimer = null;
    }
  }

  private async openConfirmSwitchDialog(mode: string): Promise<void> {
    // Beach (auto OFF): the rule says it's time to switch but the user opted out
    // of automatic switches. Indoor: the set just ended; ask whether they switched.
    const isBeach = mode === 'autoeverypoints';
    const data: ConfirmDialogData = isBeach
      ? {
          title: 'Switch sides?',
          message: 'The points total just hit a switch boundary. Have the teams switched sides?',
          confirmLabel: 'Yes, switched',
          cancelLabel: 'Not yet',
        }
      : {
          title: 'Switch sides',
          message: 'End of set — teams must switch sides before the next set begins. Confirm when done.',
          confirmLabel: 'Sides switched',
          cancelLabel: 'Skip',
        };

    const result = await this.dialog
      .open<ConfirmDialogComponent, ConfirmDialogData, boolean>(ConfirmDialogComponent, {
        data,
        disableClose: true,
      })
      .afterClosed()
      .toPromise();

    await this.store.resolveSideSwitch(result === true);
  }

  private openSideSwitchInfoDialog(): void {
    this.dialog.open<ConfirmDialogComponent, ConfirmDialogData, boolean>(
      ConfirmDialogComponent,
      {
        data: {
          title: 'Sides switched',
          message: 'Teams just switched sides — court change required.',
          confirmLabel: 'Got it',
          hideCancel: true,
          autoDismissSeconds: 5,
        },
      },
    );
  }
}
