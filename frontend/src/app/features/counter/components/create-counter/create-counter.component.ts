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
  template: `
    <div class="flex flex-col gap-6 pb-8">

      <div class="text-center pt-4">
        <div class="inline-flex items-center justify-center w-16 h-16 rounded-2xl bg-primary/10 mb-4">
          <span class="material-symbols-rounded text-4xl text-primary">scoreboard</span>
        </div>
        <h1 class="text-2xl font-bold text-on-surface">{{ 'counter.newTitle' | translate }}</h1>
        <p class="text-on-surface-muted text-sm mt-1">{{ 'counter.newSubtitle' | translate }}</p>
      </div>

      @if (resumeCounterId() && !auth.isAuthenticated()) {
        <a
          [routerLink]="['/counter', resumeCounterId()]"
          class="pts-card flex items-center gap-3 hover:border-primary transition-colors"
        >
          <span class="material-symbols-rounded text-primary text-3xl">play_circle</span>
          <div class="flex-1">
            <p class="font-semibold text-on-surface text-sm">{{ 'counter.resumeTitle' | translate }}</p>
            <p class="text-xs text-on-surface-muted">{{ 'counter.resumeHelp' | translate }}</p>
          </div>
          <span class="material-symbols-rounded text-on-surface-muted">chevron_right</span>
        </a>
      }

      @if (auth.isAuthenticated()) {
        <a
          routerLink="/my-counters"
          class="pts-card flex items-center gap-3 hover:border-primary transition-colors"
        >
          <span class="material-symbols-rounded text-primary text-3xl">list_alt</span>
          <div class="flex-1">
            <p class="font-semibold text-on-surface text-sm">{{ 'counter.myShortcutTitle' | translate }}</p>
            <p class="text-xs text-on-surface-muted">{{ 'counter.myShortcutHelp' | translate }}</p>
          </div>
          <span class="material-symbols-rounded text-on-surface-muted">chevron_right</span>
        </a>
      }

      <form [formGroup]="form" (ngSubmit)="submit()" class="flex flex-col gap-5">

        <div class="pts-card flex flex-col gap-3">
          <p class="pts-label">{{ 'tournament.create.sport.label' | translate }}</p>
          <pts-sport-selector
            [sports]="sports"
            [selected]="form.value.sportType ?? null"
            (sportSelected)="onSportSelected($event)"
          />
          @if (form.controls.sportType.invalid && form.controls.sportType.touched) {
            <p class="text-error text-xs flex items-center gap-1">
              <span class="material-symbols-rounded text-sm">error</span>
              {{ 'counter.sportRequired' | translate }}
            </p>
          }
        </div>

        @if (form.value.sportType) {
          <div class="pts-card flex flex-col gap-3" [formGroup]="form.controls.rules">

            <button
              type="button"
              class="flex items-center justify-between gap-2 -m-1 p-1 rounded-lg
                     hover:bg-surface-variant transition-colors disabled:opacity-100"
              (click)="form.value.sportType !== 'custom' && toggleCustomize(!customizeRules())"
              [disabled]="form.value.sportType === 'custom'"
            >
              <span class="flex items-center gap-2">
                <span class="material-symbols-rounded text-base text-on-surface-muted">tune</span>
                <span class="pts-label">{{ 'rules.rulesSection' | translate }}</span>
                @if (!customizeRules() && form.value.sportType !== 'custom') {
                  <span class="text-xs text-on-surface-muted font-normal normal-case tracking-normal">
                    · {{ 'rules.rulesDefault' | translate }}
                  </span>
                }
              </span>
              @if (form.value.sportType !== 'custom') {
                <span class="material-symbols-rounded text-on-surface-muted text-lg transition-transform"
                      [style.transform]="customizeRules() ? 'rotate(180deg)' : 'rotate(0deg)'">
                  expand_more
                </span>
              }
            </button>

            @if (customizeRules() || form.value.sportType === 'custom') {
              <div class="grid grid-cols-2 gap-3">
                <label class="flex flex-col gap-1">
                  <span class="text-xs text-on-surface-muted">{{ 'rules.pointsPerSet' | translate }}</span>
                  <input type="number" min="1" max="99" formControlName="pointsPerSet" class="pts-input" />
                </label>
                <label class="flex flex-col gap-1">
                  <span class="text-xs text-on-surface-muted">{{ 'rules.lastSetPoints' | translate }}</span>
                  <input type="number" min="1" max="99" formControlName="lastSetPoints" class="pts-input" />
                </label>
                <label class="flex flex-col gap-1">
                  <span class="text-xs text-on-surface-muted">{{ 'rules.setsToWin' | translate }}</span>
                  <input type="number" min="1" max="9" formControlName="setsToWin" class="pts-input" />
                </label>
                <label class="flex flex-col gap-1">
                  <span class="text-xs text-on-surface-muted">{{ 'rules.totalSets' | translate }}</span>
                  <input type="number" min="1" max="9" formControlName="totalSets" class="pts-input" />
                </label>
              </div>
              <label class="flex items-center gap-2 text-sm text-on-surface">
                <input type="checkbox" formControlName="winByTwo" class="accent-primary" />
                {{ 'rules.winByTwo' | translate }}
              </label>

              @if (form.value.sportType === 'volleyball') {
                <label class="flex items-start gap-2 text-sm text-on-surface">
                  <input
                    type="checkbox"
                    class="accent-primary mt-0.5"
                    [checked]="form.value.indoorSwitchEverySets === 2"
                    (change)="onProModeToggle($any($event.target).checked)"
                  />
                  <span>
                    <span [innerHTML]="'rules.proModeLabel' | translate"></span>
                    <span class="block text-xs text-on-surface-muted font-normal mt-0.5">
                      {{ 'rules.proModeHelp' | translate }}
                    </span>
                  </span>
                </label>
              }

              @if (form.value.sportType === 'beach_volleyball') {
                <label class="flex items-start gap-2 text-sm text-on-surface">
                  <input
                    type="checkbox"
                    class="accent-primary mt-0.5"
                    [formControl]="form.controls.beachAutoSwitchSides"
                  />
                  <span>
                    {{ 'rules.beachAutoLabel' | translate }}
                    <span class="block text-xs text-on-surface-muted font-normal mt-0.5">
                      {{ 'rules.beachAutoHelp' | translate }}
                    </span>
                  </span>
                </label>
              }
            }
          </div>
        }

        @if (form.value.sportType) {
          <div class="pts-card flex flex-col gap-3" [formGroup]="form.controls.timeouts">
            <button
              type="button"
              class="flex items-center justify-between gap-2 -m-1 p-1 rounded-lg
                     hover:bg-surface-variant transition-colors"
              (click)="toggleCustomizeTimeouts(!customizeTimeouts())"
            >
              <span class="flex items-center gap-2">
                <span class="material-symbols-rounded text-base text-on-surface-muted">pause_circle</span>
                <span class="pts-label">{{ 'rules.timeoutsSection' | translate }}</span>
                @if (!customizeTimeouts()) {
                  <span class="text-xs text-on-surface-muted font-normal normal-case tracking-normal">
                    · {{ 'rules.timeoutsSummary' | translate: {
                          count: form.controls.timeouts.value.timeoutsPerSet,
                          seconds: form.controls.timeouts.value.timeoutDurationSeconds
                        } }}
                  </span>
                }
              </span>
              <span class="material-symbols-rounded text-on-surface-muted text-lg transition-transform"
                    [style.transform]="customizeTimeouts() ? 'rotate(180deg)' : 'rotate(0deg)'">
                expand_more
              </span>
            </button>

            @if (customizeTimeouts()) {
              <div class="grid grid-cols-2 gap-3">
                <label class="flex flex-col gap-1">
                  <span class="text-xs text-on-surface-muted">{{ 'rules.perTeamPerSet' | translate }}</span>
                  <input type="number" min="0" max="9" formControlName="timeoutsPerSet" class="pts-input" />
                </label>
                <label class="flex flex-col gap-1">
                  <span class="text-xs text-on-surface-muted">{{ 'rules.durationSeconds' | translate }}</span>
                  <input type="number" min="5" max="600" formControlName="timeoutDurationSeconds" class="pts-input" />
                </label>
              </div>
              <p class="text-xs text-on-surface-muted">{{ 'rules.timeoutsHelp' | translate }}</p>
            }
          </div>
        }

        <div class="pts-card flex flex-col gap-4">
          <p class="pts-label">{{ 'counter.teamsLabel' | translate }}</p>

          <div class="flex flex-col gap-1.5">
            <label class="text-sm font-medium text-on-surface" for="teamA">
              <span class="inline-block w-2 h-2 rounded-full bg-team-a mr-2"></span>{{ 'counter.teamA' | translate }}
            </label>
            <input
              id="teamA"
              type="text"
              formControlName="teamAName"
              maxlength="100"
              [placeholder]="'counter.teamA' | translate"
              class="pts-input"
            />
          </div>

          <div class="flex flex-col gap-1.5">
            <label class="text-sm font-medium text-on-surface" for="teamB">
              <span class="inline-block w-2 h-2 rounded-full bg-team-b mr-2"></span>{{ 'counter.teamB' | translate }}
            </label>
            <input
              id="teamB"
              type="text"
              formControlName="teamBName"
              maxlength="100"
              [placeholder]="'counter.teamB' | translate"
              class="pts-input"
            />
          </div>
        </div>

        <button
          type="submit"
          class="pts-btn-primary w-full py-4 text-base rounded-2xl"
          [disabled]="form.invalid || submitting()"
        >
          @if (submitting()) {
            <pts-loading-spinner size="sm" />
            <span>{{ 'common.starting' | translate }}</span>
          } @else {
            <span class="material-symbols-rounded">play_arrow</span>
            <span>{{ 'counter.submit' | translate }}</span>
          }
        </button>
      </form>
    </div>
  `,
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
