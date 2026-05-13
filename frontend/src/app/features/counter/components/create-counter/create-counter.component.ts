import { Component, inject, signal, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { SportSelectorComponent } from '../../../../shared/components/sport-selector/sport-selector.component';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { CounterService } from '../../services/counter.service';
import { NotificationService } from '../../../../core/services/notification.service';
import { SessionTokenService } from '../../../../core/auth/session-token.service';
import { AuthService } from '../../../../core/auth/auth.service';
import { SportType, SPORT_CONFIGS } from '../../../../shared/models/sport.model';

@Component({
  selector: 'pts-create-counter',
  imports: [ReactiveFormsModule, RouterLink, SportSelectorComponent, LoadingSpinnerComponent],
  template: `
    <div class="flex flex-col gap-6 pb-8">

      <!-- Hero -->
      <div class="text-center pt-4">
        <div class="inline-flex items-center justify-center w-16 h-16 rounded-2xl bg-primary/10 mb-4">
          <span class="material-symbols-rounded text-4xl text-primary">scoreboard</span>
        </div>
        <h1 class="text-2xl font-bold text-on-surface">New Counter</h1>
        <p class="text-on-surface-muted text-sm mt-1">Choose a sport and name your teams to get started.</p>
      </div>

      <!-- Anonymous: existing counter resume hint -->
      @if (resumeCounterId() && !auth.isAuthenticated()) {
        <a
          [routerLink]="['/counter', resumeCounterId()]"
          class="pts-card flex items-center gap-3 hover:border-primary transition-colors"
        >
          <span class="material-symbols-rounded text-primary text-3xl">play_circle</span>
          <div class="flex-1">
            <p class="font-semibold text-on-surface text-sm">Resume your counter</p>
            <p class="text-xs text-on-surface-muted">
              You already have a counter running. Sign in to keep more than one at a time.
            </p>
          </div>
          <span class="material-symbols-rounded text-on-surface-muted">chevron_right</span>
        </a>
      }

      <!-- Logged-in: my counters shortcut -->
      @if (auth.isAuthenticated()) {
        <a
          routerLink="/my-counters"
          class="pts-card flex items-center gap-3 hover:border-primary transition-colors"
        >
          <span class="material-symbols-rounded text-primary text-3xl">list_alt</span>
          <div class="flex-1">
            <p class="font-semibold text-on-surface text-sm">My Counters</p>
            <p class="text-xs text-on-surface-muted">View, resume or delete your existing counters.</p>
          </div>
          <span class="material-symbols-rounded text-on-surface-muted">chevron_right</span>
        </a>
      }

      <form [formGroup]="form" (ngSubmit)="submit()" class="flex flex-col gap-5">

        <!-- Sport selection -->
        <div class="pts-card flex flex-col gap-3">
          <p class="pts-label">Sport</p>
          <pts-sport-selector
            [sports]="sports"
            [selected]="form.value.sportType ?? null"
            (sportSelected)="onSportSelected($event)"
          />
          @if (form.controls.sportType.invalid && form.controls.sportType.touched) {
            <p class="text-error text-xs flex items-center gap-1">
              <span class="material-symbols-rounded text-sm">error</span>
              Please select a sport.
            </p>
          }
        </div>

        <!-- Custom rules — collapsed by default for built-in sports, always open for Custom -->
        @if (form.value.sportType) {
          <div class="pts-card flex flex-col gap-3" [formGroup]="form.controls.rules">

            <!-- Header / toggle -->
            <button
              type="button"
              class="flex items-center justify-between gap-2 -m-1 p-1 rounded-lg
                     hover:bg-surface-variant transition-colors disabled:opacity-100"
              (click)="form.value.sportType !== 'custom' && toggleCustomize(!customizeRules())"
              [disabled]="form.value.sportType === 'custom'"
            >
              <span class="flex items-center gap-2">
                <span class="material-symbols-rounded text-base text-on-surface-muted">tune</span>
                <span class="pts-label">Rules</span>
                @if (!customizeRules() && form.value.sportType !== 'custom') {
                  <span class="text-xs text-on-surface-muted font-normal normal-case tracking-normal">
                    · default
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
                  <span class="text-xs text-on-surface-muted">Points per set</span>
                  <input type="number" min="1" max="99" formControlName="pointsPerSet" class="pts-input" />
                </label>
                <label class="flex flex-col gap-1">
                  <span class="text-xs text-on-surface-muted">Last-set points</span>
                  <input type="number" min="1" max="99" formControlName="lastSetPoints" class="pts-input" />
                </label>
                <label class="flex flex-col gap-1">
                  <span class="text-xs text-on-surface-muted">Sets to win</span>
                  <input type="number" min="1" max="9" formControlName="setsToWin" class="pts-input" />
                </label>
                <label class="flex flex-col gap-1">
                  <span class="text-xs text-on-surface-muted">Total sets</span>
                  <input type="number" min="1" max="9" formControlName="totalSets" class="pts-input" />
                </label>
              </div>
              <label class="flex items-center gap-2 text-sm text-on-surface">
                <input type="checkbox" formControlName="winByTwo" class="accent-primary" />
                Must win by 2
              </label>

              <!-- Indoor volleyball: pro mode switches every 2 sets instead of every set. -->
              @if (form.value.sportType === 'volleyball') {
                <label class="flex items-start gap-2 text-sm text-on-surface">
                  <input
                    type="checkbox"
                    class="accent-primary mt-0.5"
                    [checked]="form.value.indoorSwitchEverySets === 2"
                    (change)="onProModeToggle($any($event.target).checked)"
                  />
                  <span>
                    Switch sides every <strong>2 sets</strong> (pro mode)
                    <span class="block text-xs text-on-surface-muted font-normal mt-0.5">
                      Off: switch every set (standard).
                    </span>
                  </span>
                </label>
              }

              <!-- Beach volleyball: optionally let the server auto-switch sides
                   at the points boundary; otherwise the user uses the manual button. -->
              @if (form.value.sportType === 'beach_volleyball') {
                <label class="flex items-start gap-2 text-sm text-on-surface">
                  <input
                    type="checkbox"
                    class="accent-primary mt-0.5"
                    [formControl]="form.controls.beachAutoSwitchSides"
                  />
                  <span>
                    Auto-switch sides
                    <span class="block text-xs text-on-surface-muted font-normal mt-0.5">
                      On: server switches automatically every 7 points (5 in the deciding set), with a 5-second notification.
                      Off: you'll be asked to confirm each switch.
                    </span>
                  </span>
                </label>
              }
            }
          </div>
        }

        <!-- Team names -->
        <div class="pts-card flex flex-col gap-4">
          <p class="pts-label">Teams</p>

          <div class="flex flex-col gap-1.5">
            <label class="text-sm font-medium text-on-surface" for="teamA">
              <span class="inline-block w-2 h-2 rounded-full bg-team-a mr-2"></span>Team A
            </label>
            <input
              id="teamA"
              type="text"
              formControlName="teamAName"
              maxlength="100"
              placeholder="Team A"
              class="pts-input"
            />
          </div>

          <div class="flex flex-col gap-1.5">
            <label class="text-sm font-medium text-on-surface" for="teamB">
              <span class="inline-block w-2 h-2 rounded-full bg-team-b mr-2"></span>Team B
            </label>
            <input
              id="teamB"
              type="text"
              formControlName="teamBName"
              maxlength="100"
              placeholder="Team B"
              class="pts-input"
            />
          </div>
        </div>

        <!-- Submit -->
        <button
          type="submit"
          class="pts-btn-primary w-full py-4 text-base rounded-2xl"
          [disabled]="form.invalid || submitting()"
        >
          @if (submitting()) {
            <pts-loading-spinner size="sm" />
            <span>Starting…</span>
          } @else {
            <span class="material-symbols-rounded">play_arrow</span>
            <span>Start Counter</span>
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

  readonly sports     = Object.values(SPORT_CONFIGS);
  readonly submitting = signal(false);
  readonly customizeRules = signal(false);
  readonly resumeCounterId = signal<string | null>(null);

  readonly form = this.fb.group({
    sportType:  this.fb.control<SportType | null>(null, Validators.required),
    teamAName: ['Team A', [Validators.required, Validators.maxLength(100)]],
    teamBName: ['Team B', [Validators.required, Validators.maxLength(100)]],
    rules: this.fb.group({
      pointsPerSet:  [25, [Validators.required, Validators.min(1), Validators.max(99)]],
      lastSetPoints: [15, [Validators.required, Validators.min(1), Validators.max(99)]],
      setsToWin:     [ 3, [Validators.required, Validators.min(1), Validators.max(9)]],
      totalSets:     [ 5, [Validators.required, Validators.min(1), Validators.max(9)]],
      winByTwo:      [true],
    }),
    // Indoor volleyball only: 1 = switch every set, 2 = switch every two sets (pro).
    indoorSwitchEverySets: this.fb.control<number>(1),
    // Beach volleyball only: when off, server stops auto-switching at the
    // points boundary and the user is expected to use the manual switch button.
    beachAutoSwitchSides: this.fb.control<boolean>(true),
  });

  ngOnInit(): void {
    // For anonymous users, surface the most-recent stored counter so they can resume it.
    const ids = this.sessionTokens.getAllCounterIds();
    if (ids.length > 0) this.resumeCounterId.set(ids[0]);
  }

  onSportSelected(type: SportType): void {
    this.form.patchValue({ sportType: type });
    const cfg = SPORT_CONFIGS[type];
    // Reset rules to the defaults for the picked sport. "custom" keeps current values.
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
      });
      await this.router.navigate(['/counter', counter.id]);
    } catch {
      this.notifications.error('Failed to create counter. Please try again.');
    } finally {
      this.submitting.set(false);
    }
  }
}
