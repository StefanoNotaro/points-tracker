import { Component, inject, input, signal, computed, OnInit, OnDestroy, effect } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { MatDialog } from '@angular/material/dialog';
import { MatMenuModule } from '@angular/material/menu';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
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
    TranslatePipe,
  ],
  providers: [CounterStore],
  templateUrl: './counter-page.component.html',
})
export class CounterPageComponent implements OnInit, OnDestroy {
  readonly id    = input.required<string>();
  readonly store = inject(CounterStore);
  private readonly dialog = inject(MatDialog);
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);
  private readonly i18n = inject(TranslateService);

  readonly canShare = computed(() => this.store.counter()?.isOwner === true);

  readonly editingTeams = signal(false);
  readonly showMatchInfo = signal(false);

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

  readonly swapSides = computed(() => {
    const c = this.store.counter();
    return !!c && c.sideSwitchCount % 2 === 1;
  });

  private sideSwitchDialogOpen = false;
  private lastObservedSwitchCount: number | null = null;

  private holdTimer: ReturnType<typeof setInterval> | null = null;
  private holdInitialTimer: ReturnType<typeof setTimeout> | null = null;
  private holdSuppressClick = false;
  private readonly HOLD_DELAY_MS = 400;
  private readonly HOLD_INTERVAL_MS = 180;

  constructor() {
    effect(() => {
      if (this.store.counterDeleted()) {
        this.router.navigate([this.auth.isAuthenticated() ? '/dashboard' : '/new-counter']);
      }
    });

    effect(() => {
      const active = this.store.counter()?.activeTimeout;
      if (active) this.startNowTicker();
      else this.stopNowTicker();
    });

    effect(() => {
      const counter = this.store.counter();
      if (!counter) return;

      if (counter.canEdit && counter.pendingSideSwitchConfirmation) {
        if (!this.sideSwitchDialogOpen) {
          this.sideSwitchDialogOpen = true;
          this.openConfirmSwitchDialog(counter.rules.sideSwitchMode);
        }
      } else {
        this.sideSwitchDialogOpen = false;
      }

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
          title: this.i18n.instant('counter.page.endMatchTitle'),
          message: this.i18n.instant('counter.page.endMatchMessage'),
          confirmLabel: this.i18n.instant('counter.page.endMatchConfirm'),
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
          title: this.i18n.instant('counter.page.deleteTitle'),
          message: this.i18n.instant('counter.page.deleteMessage', { a: counter.teamAName, b: counter.teamBName }),
          confirmLabel: this.i18n.instant('counter.page.deleteConfirm'),
        },
      })
      .afterClosed()
      .toPromise();
    if (confirmed) await this.store.deleteCurrent();
  }

  async callTimeout(team: 'A' | 'B'): Promise<void> {
    await this.store.callTimeout(team);
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
    const isBeach = mode === 'autoeverypoints';
    const prefix = isBeach ? 'counter.page.sideSwitchAuto' : 'counter.page.sideSwitchEndSet';
    const data: ConfirmDialogData = {
      title: this.i18n.instant(`${prefix}.title`),
      message: this.i18n.instant(`${prefix}.message`),
      confirmLabel: this.i18n.instant(`${prefix}.confirm`),
      cancelLabel: this.i18n.instant(`${prefix}.cancel`),
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
          title: this.i18n.instant('counter.page.sideSwitchInfo.title'),
          message: this.i18n.instant('counter.page.sideSwitchInfo.message'),
          confirmLabel: this.i18n.instant('counter.page.sideSwitchInfo.confirm'),
          hideCancel: true,
          autoDismissSeconds: 5,
        },
      },
    );
  }
}
