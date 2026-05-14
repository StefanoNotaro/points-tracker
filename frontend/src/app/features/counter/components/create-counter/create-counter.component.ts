import { Component, inject, signal, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { SportSelectorComponent } from '../../../../shared/components/sport-selector/sport-selector.component';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { CounterService } from '../../services/counter.service';
import { NotificationService } from '../../../../core/services/notification.service';
import { SessionTokenService } from '../../../../core/auth/session-token.service';
import { AuthService } from '../../../../core/auth/auth.service';
import { SportType, SPORT_CONFIGS } from '../../../../shared/models/sport.model';

@Component({
  selector: 'pts-create-counter',
  imports: [ReactiveFormsModule, RouterLink, SportSelectorComponent, LoadingSpinnerComponent, TranslatePipe],
  templateUrl: './create-counter.component.html',
})
export class CreateCounterComponent implements OnInit {
  private readonly router         = inject(Router);
  private readonly counterService = inject(CounterService);
  private readonly notifications  = inject(NotificationService);
  private readonly sessionTokens  = inject(SessionTokenService);
  readonly auth                   = inject(AuthService);
  private readonly fb             = inject(FormBuilder);
  private readonly i18n           = inject(TranslateService);

  readonly sports     = Object.values(SPORT_CONFIGS);
  readonly submitting = signal(false);
  readonly customizeRules = signal(false);
  readonly customizeTimeouts = signal(false);
  readonly resumeCounterId = signal<string | null>(null);

  private readonly TIMEOUT_DEFAULTS: Record<SportType, { count: number; seconds: number }> = {
    volleyball:       { count: 2, seconds: 30 },
    beach_volleyball: { count: 1, seconds: 30 },
    custom:           { count: 2, seconds: 30 },
  };

  readonly form = this.fb.group({
    sportType:  this.fb.control<SportType | null>(null, Validators.required),
    teamAName: [this.i18n.instant('counter.teamA'), [Validators.required, Validators.maxLength(100)]],
    teamBName: [this.i18n.instant('counter.teamB'), [Validators.required, Validators.maxLength(100)]],
    rules: this.fb.group({
      pointsPerSet:  [25, [Validators.required, Validators.min(1), Validators.max(99)]],
      lastSetPoints: [15, [Validators.required, Validators.min(1), Validators.max(99)]],
      setsToWin:     [ 3, [Validators.required, Validators.min(1), Validators.max(9)]],
      totalSets:     [ 5, [Validators.required, Validators.min(1), Validators.max(9)]],
      winByTwo:      [true],
    }),
    indoorSwitchEverySets: this.fb.control<number>(1),
    beachAutoSwitchSides: this.fb.control<boolean>(true),
    timeouts: this.fb.group({
      timeoutsPerSet:         [ 2, [Validators.required, Validators.min(0), Validators.max(9)]],
      timeoutDurationSeconds: [30, [Validators.required, Validators.min(5), Validators.max(600)]],
    }),
  });

  ngOnInit(): void {
    const ids = this.sessionTokens.getAllCounterIds();
    if (ids.length > 0) this.resumeCounterId.set(ids[0]);
  }

  onSportSelected(type: SportType): void {
    this.form.patchValue({ sportType: type });
    const cfg = SPORT_CONFIGS[type];
    if (type !== 'custom') {
      this.form.controls.rules.patchValue({
        pointsPerSet:  cfg.pointsPerSet,
        lastSetPoints: cfg.lastSetPoints,
        setsToWin:     cfg.setsToWin,
        totalSets:     cfg.totalSets,
        winByTwo:      cfg.winByTwo,
      });
      this.customizeRules.set(false);
      this.form.controls.rules.disable();
    } else {
      this.customizeRules.set(true);
      this.form.controls.rules.enable();
    }

    const t = this.TIMEOUT_DEFAULTS[type];
    this.form.controls.timeouts.patchValue({
      timeoutsPerSet:         t.count,
      timeoutDurationSeconds: t.seconds,
    });
    this.customizeTimeouts.set(false);
    this.form.controls.timeouts.disable();
  }

  toggleCustomizeTimeouts(on: boolean): void {
    this.customizeTimeouts.set(on);
    if (on) this.form.controls.timeouts.enable();
    else this.form.controls.timeouts.disable();
  }

  onProModeToggle(on: boolean): void {
    this.form.patchValue({ indoorSwitchEverySets: on ? 2 : 1 });
  }

  toggleCustomize(on: boolean): void {
    this.customizeRules.set(on);
    if (on) this.form.controls.rules.enable();
    else this.form.controls.rules.disable();
  }

  async submit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const sportType = this.form.value.sportType!;
    const sendCustom = sportType === 'custom' || this.customizeRules();
    const rulesValue = this.form.controls.rules.getRawValue();
    const timeoutsValue = this.form.controls.timeouts.getRawValue();
    const sendTimeouts = this.customizeTimeouts();

    this.submitting.set(true);
    try {
      const { counter } = await this.counterService.create({
        sportType,
        teamAName: this.form.value.teamAName!,
        teamBName: this.form.value.teamBName!,
        customRules: sendCustom ? {
          pointsPerSet:  rulesValue.pointsPerSet!,
          lastSetPoints: rulesValue.lastSetPoints!,
          setsToWin:     rulesValue.setsToWin!,
          totalSets:     rulesValue.totalSets!,
          winByTwo:      rulesValue.winByTwo!,
        } : undefined,
        indoorSwitchEverySets: sportType === 'volleyball'
          ? this.form.value.indoorSwitchEverySets ?? 1
          : null,
        beachAutoSwitchSides: sportType === 'beach_volleyball'
          ? this.form.value.beachAutoSwitchSides ?? true
          : true,
        customTimeoutsPerSet:         sendTimeouts ? timeoutsValue.timeoutsPerSet         ?? null : null,
        customTimeoutDurationSeconds: sendTimeouts ? timeoutsValue.timeoutDurationSeconds ?? null : null,
      });
      await this.router.navigate(['/counter', counter.id]);
    } catch {
      this.notifications.error(this.i18n.instant('counter.createError'));
    } finally {
      this.submitting.set(false);
    }
  }
}
