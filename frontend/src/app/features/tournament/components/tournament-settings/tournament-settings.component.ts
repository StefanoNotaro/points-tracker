import { Component, effect, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NgClass, NgTemplateOutlet } from '@angular/common';
import { TournamentService } from '../../services/tournament.service';
import { NotificationService } from '../../../../core/services/notification.service';
import {
  CustomRulesPayload,
  Tournament,
  UpdateTournamentRequest,
} from '../../../../shared/models/tournament.model';

interface StageForm {
  override: boolean;
  pointsPerSet: number;
  lastSetPoints: number;
  setsToWin: number;
  totalSets: number;
  winByTwo: boolean;
  timeoutsPerSet: number | null;
  timeoutDuration: number | null;
}

@Component({
  selector: 'pts-tournament-settings',
  imports: [FormsModule, NgClass, NgTemplateOutlet],
  template: `
    <form class="flex flex-col gap-5" (submit)="$event.preventDefault(); save()">

      <!-- Identity -->
      <section class="flex flex-col gap-2">
        <h2 class="pts-label">Identity</h2>
        <label class="text-xs text-on-surface-muted" for="t-name">Name</label>
        <input id="t-name" type="text" class="pts-input" maxlength="200"
               [(ngModel)]="form.name" name="name" (ngModelChange)="touch()" />
      </section>

      <!-- Schedule -->
      <section class="flex flex-col gap-2">
        <h2 class="pts-label">Schedule</h2>
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <label class="flex flex-col gap-1">
            <span class="text-xs text-on-surface-muted">Starts</span>
            <input type="datetime-local" class="pts-input"
                   [(ngModel)]="form.startsAt" name="startsAt" (ngModelChange)="touch()" />
          </label>
          <label class="flex flex-col gap-1">
            <span class="text-xs text-on-surface-muted">Ends</span>
            <input type="datetime-local" class="pts-input"
                   [(ngModel)]="form.endsAt" name="endsAt" (ngModelChange)="touch()" />
          </label>
        </div>
      </section>

      <!-- Default match rules -->
      <section class="flex flex-col gap-3">
        <div class="flex items-center justify-between">
          <h2 class="pts-label">Default match rules</h2>
          <label class="flex items-center gap-2 text-xs text-on-surface-muted">
            <input type="checkbox" [(ngModel)]="form.overrideRules"
                   name="overrideRules" (ngModelChange)="touch()" />
            Override sport defaults
          </label>
        </div>
        @if (form.overrideRules) {
          <ng-container *ngTemplateOutlet="rulesGrid; context: { stage: form.defaultRules, prefix: 'd' }"></ng-container>
          <p class="text-xs text-on-surface-muted">
            Single-set indoor mode? Set sets to win = 1 and total sets = 1.
          </p>
        }

        <div class="grid grid-cols-2 gap-3">
          <label class="flex flex-col gap-1">
            <span class="text-xs text-on-surface-muted">Timeouts per set</span>
            <input type="number" class="pts-input" min="0" max="9"
                   [(ngModel)]="form.timeoutsPerSet" name="tps"
                   (ngModelChange)="touch()" />
          </label>
          <label class="flex flex-col gap-1">
            <span class="text-xs text-on-surface-muted">Timeout seconds</span>
            <input type="number" class="pts-input" min="5" max="600"
                   [(ngModel)]="form.timeoutDuration" name="td"
                   (ngModelChange)="touch()" />
          </label>
          <label class="flex flex-col gap-1 col-span-2">
            <span class="text-xs text-on-surface-muted">Indoor side-switch every N sets</span>
            <select class="pts-input" [(ngModel)]="form.indoorSwitchEverySets"
                    name="indoorSwitch" (ngModelChange)="touch()">
              <option [ngValue]="null">Sport default</option>
              <option [ngValue]="1">Every set (1)</option>
              <option [ngValue]="2">Every two sets (2)</option>
            </select>
          </label>
          <label class="flex items-center gap-2 col-span-2 text-sm">
            <input type="checkbox" [(ngModel)]="form.beachAutoSwitch"
                   name="beachAutoSwitch" (ngModelChange)="touch()" />
            <span>Beach: auto-switch sides at point milestones</span>
          </label>
        </div>
      </section>

      <!-- Final-match rules -->
      <section class="flex flex-col gap-3">
        <div class="flex items-center justify-between">
          <h2 class="pts-label">Final</h2>
          <label class="flex items-center gap-2 text-xs text-on-surface-muted">
            <input type="checkbox" [(ngModel)]="form.finalRules.override"
                   name="finalOverride" (ngModelChange)="touch()" />
            Custom rules for the final
          </label>
        </div>
        @if (form.finalRules.override) {
          <ng-container *ngTemplateOutlet="rulesGrid; context: { stage: form.finalRules, prefix: 'f' }"></ng-container>
          <ng-container *ngTemplateOutlet="timeoutsGrid; context: { stage: form.finalRules, prefix: 'f' }"></ng-container>
        }
      </section>

      <!-- Semifinal-match rules -->
      <section class="flex flex-col gap-3">
        <div class="flex items-center justify-between">
          <h2 class="pts-label">Semifinals (3rd / 4th place)</h2>
          <label class="flex items-center gap-2 text-xs text-on-surface-muted">
            <input type="checkbox" [(ngModel)]="form.semifinalRules.override"
                   name="semiOverride" (ngModelChange)="touch()" />
            Custom rules for semifinals
          </label>
        </div>
        @if (form.semifinalRules.override) {
          <ng-container *ngTemplateOutlet="rulesGrid; context: { stage: form.semifinalRules, prefix: 's' }"></ng-container>
          <ng-container *ngTemplateOutlet="timeoutsGrid; context: { stage: form.semifinalRules, prefix: 's' }"></ng-container>
        }
      </section>

      <div class="flex gap-2 pt-2">
        <button type="submit" class="pts-btn-primary flex-1"
                [disabled]="!dirty() || saving()"
                [ngClass]="dirty() ? '' : 'opacity-50'">
          @if (saving()) {
            <span class="material-symbols-rounded animate-spin text-lg">progress_activity</span>
          } @else {
            <span class="material-symbols-rounded text-lg">save</span>
          }
          <span>Save changes</span>
        </button>
      </div>
    </form>

    <ng-template #rulesGrid let-stage="stage" let-prefix="prefix">
      <div class="grid grid-cols-2 gap-3">
        <label class="flex flex-col gap-1">
          <span class="text-xs text-on-surface-muted">Points per set</span>
          <input type="number" class="pts-input" min="1" max="99"
                 [(ngModel)]="stage.pointsPerSet" [name]="prefix + 'pp'" (ngModelChange)="touch()" />
        </label>
        <label class="flex flex-col gap-1">
          <span class="text-xs text-on-surface-muted">Last-set points</span>
          <input type="number" class="pts-input" min="1" max="99"
                 [(ngModel)]="stage.lastSetPoints" [name]="prefix + 'lsp'" (ngModelChange)="touch()" />
        </label>
        <label class="flex flex-col gap-1">
          <span class="text-xs text-on-surface-muted">Sets to win</span>
          <input type="number" class="pts-input" min="1" max="9"
                 [(ngModel)]="stage.setsToWin" [name]="prefix + 'stw'" (ngModelChange)="touch()" />
        </label>
        <label class="flex flex-col gap-1">
          <span class="text-xs text-on-surface-muted">Total sets</span>
          <input type="number" class="pts-input" min="1" max="9"
                 [(ngModel)]="stage.totalSets" [name]="prefix + 'ts'" (ngModelChange)="touch()" />
        </label>
        <label class="flex items-center gap-2 col-span-2">
          <input type="checkbox" [(ngModel)]="stage.winByTwo" [name]="prefix + 'wbt'" (ngModelChange)="touch()" />
          <span class="text-sm">Win by two</span>
        </label>
      </div>
    </ng-template>

    <ng-template #timeoutsGrid let-stage="stage" let-prefix="prefix">
      <div class="grid grid-cols-2 gap-3">
        <label class="flex flex-col gap-1">
          <span class="text-xs text-on-surface-muted">Timeouts per set</span>
          <input type="number" class="pts-input" min="0" max="9"
                 [(ngModel)]="stage.timeoutsPerSet" [name]="prefix + 'tps'" (ngModelChange)="touch()" />
        </label>
        <label class="flex flex-col gap-1">
          <span class="text-xs text-on-surface-muted">Timeout seconds</span>
          <input type="number" class="pts-input" min="5" max="600"
                 [(ngModel)]="stage.timeoutDuration" [name]="prefix + 'td'" (ngModelChange)="touch()" />
        </label>
      </div>
    </ng-template>
  `,
})
export class TournamentSettingsComponent {
  private readonly service = inject(TournamentService);
  private readonly notifications = inject(NotificationService);

  readonly tournament = input.required<Tournament>();
  readonly saved = output<Tournament>();

  readonly form = {
    name: '',
    startsAt: '',
    endsAt: '',
    overrideRules: false,
    defaultRules: this.emptyStage(),
    timeoutsPerSet: null as number | null,
    timeoutDuration: null as number | null,
    indoorSwitchEverySets: null as number | null,
    beachAutoSwitch: true,
    finalRules: this.emptyStage(),
    semifinalRules: this.emptyStage(),
  };

  readonly saving = signal(false);
  readonly dirty = signal(false);

  constructor() {
    effect(() => {
      const t = this.tournament();
      this.form.name = t.name;
      this.form.startsAt = toLocalInput(t.startsAt);
      this.form.endsAt = toLocalInput(t.endsAt);

      const r = t.rules;
      this.form.overrideRules = r.customPointsPerSet !== null;
      this.form.defaultRules = {
        override: this.form.overrideRules,
        pointsPerSet: r.customPointsPerSet ?? 25,
        lastSetPoints: r.customLastSetPoints ?? 15,
        setsToWin: r.customSetsToWin ?? 3,
        totalSets: r.customTotalSets ?? 5,
        winByTwo: r.customWinByTwo ?? true,
        timeoutsPerSet: r.customTimeoutsPerSet,
        timeoutDuration: r.customTimeoutDurationSeconds,
      };
      this.form.timeoutsPerSet = r.customTimeoutsPerSet;
      this.form.timeoutDuration = r.customTimeoutDurationSeconds;
      this.form.indoorSwitchEverySets = r.indoorSwitchEverySets;
      this.form.beachAutoSwitch = r.beachAutoSwitchSides;

      this.form.finalRules = this.hydrateStage(t.rules.finalRules ?? null);
      this.form.semifinalRules = this.hydrateStage(t.rules.semifinalRules ?? null);

      this.dirty.set(false);
    });
  }

  private emptyStage(): StageForm {
    return {
      override: false, pointsPerSet: 25, lastSetPoints: 15,
      setsToWin: 3, totalSets: 5, winByTwo: true,
      timeoutsPerSet: null, timeoutDuration: null,
    };
  }

  private hydrateStage(s: { pointsPerSet: number | null; lastSetPoints: number | null;
    setsToWin: number | null; totalSets: number | null; winByTwo: boolean | null;
    timeoutsPerSet: number | null; timeoutDurationSeconds: number | null; } | null): StageForm {
    if (!s) return this.emptyStage();
    return {
      override: true,
      pointsPerSet: s.pointsPerSet ?? 25,
      lastSetPoints: s.lastSetPoints ?? 15,
      setsToWin: s.setsToWin ?? 3,
      totalSets: s.totalSets ?? 5,
      winByTwo: s.winByTwo ?? true,
      timeoutsPerSet: s.timeoutsPerSet,
      timeoutDuration: s.timeoutDurationSeconds,
    };
  }

  touch(): void { this.dirty.set(true); }

  async save(): Promise<void> {
    if (!this.dirty()) return;
    this.saving.set(true);
    try {
      const payload: UpdateTournamentRequest = {
        name: this.form.name.trim() || undefined,
        startsAt: this.form.startsAt ? new Date(this.form.startsAt).toISOString() : null,
        endsAt: this.form.endsAt ? new Date(this.form.endsAt).toISOString() : null,
        clearStartsAt: !this.form.startsAt,
        clearEndsAt: !this.form.endsAt,
        beachAutoSwitchSides: this.form.beachAutoSwitch,
        customTimeoutsPerSet: this.form.timeoutsPerSet,
        customTimeoutDurationSeconds: this.form.timeoutDuration,
        indoorSwitchEverySets: this.form.indoorSwitchEverySets,
      };

      if (this.form.overrideRules) payload.customRules = this.stageToRules(this.form.defaultRules);
      else payload.clearCustomRules = true;

      if (this.form.finalRules.override) {
        payload.finalRules = this.stageToRules(this.form.finalRules);
        payload.finalTimeoutsPerSet = this.form.finalRules.timeoutsPerSet;
        payload.finalTimeoutDurationSeconds = this.form.finalRules.timeoutDuration;
      } else payload.clearFinalRules = true;

      if (this.form.semifinalRules.override) {
        payload.semifinalRules = this.stageToRules(this.form.semifinalRules);
        payload.semifinalTimeoutsPerSet = this.form.semifinalRules.timeoutsPerSet;
        payload.semifinalTimeoutDurationSeconds = this.form.semifinalRules.timeoutDuration;
      } else payload.clearSemifinalRules = true;

      const updated = await this.service.update(this.tournament().id, payload);
      this.saved.emit(updated);
      this.notifications.success('Tournament saved.');
    } catch (err: any) {
      this.notifications.error(err?.error?.detail ?? 'Could not save changes.');
    } finally {
      this.saving.set(false);
    }
  }

  private stageToRules(s: StageForm): CustomRulesPayload {
    return {
      pointsPerSet: s.pointsPerSet,
      lastSetPoints: s.lastSetPoints,
      setsToWin: s.setsToWin,
      totalSets: s.totalSets,
      winByTwo: s.winByTwo,
    };
  }
}

function toLocalInput(iso: string | null): string {
  if (!iso) return '';
  const d = new Date(iso);
  const pad = (n: number) => n.toString().padStart(2, '0');
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
}
