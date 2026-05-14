import { Component, effect, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NgClass, NgTemplateOutlet } from '@angular/common';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
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
  imports: [FormsModule, NgClass, NgTemplateOutlet, TranslatePipe],
  templateUrl: './tournament-settings.component.html',
})
export class TournamentSettingsComponent {
  private readonly service = inject(TournamentService);
  private readonly notifications = inject(NotificationService);
  private readonly i18n = inject(TranslateService);

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
      this.notifications.success(this.i18n.instant('tournament.settings.saved'));
    } catch (err: any) {
      this.notifications.error(err?.error?.detail ?? this.i18n.instant('tournament.settings.saveError'));
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
