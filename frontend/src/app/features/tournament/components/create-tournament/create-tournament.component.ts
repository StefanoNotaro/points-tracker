import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NgClass } from '@angular/common';
import { Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { TournamentService } from '../../services/tournament.service';
import { NotificationService } from '../../../../core/services/notification.service';
import { SportSelectorComponent } from '../../../../shared/components/sport-selector/sport-selector.component';
import { SPORT_CONFIGS, SportType } from '../../../../shared/models/sport.model';
import {
  TournamentFormat,
  TOURNAMENT_FORMATS,
  isValidGroupConfig,
} from '../../../../shared/models/tournament.model';

@Component({
  selector: 'pts-create-tournament',
  imports: [FormsModule, NgClass, SportSelectorComponent, TranslatePipe],
  template: `
    <div class="flex flex-col gap-5 pb-8">
      <header class="flex flex-col gap-1">
        <p class="text-xs uppercase tracking-wide text-on-surface-muted">{{ 'tournament.create.kicker' | translate }}</p>
        <h1 class="text-xl sm:text-2xl font-bold text-on-surface">{{ 'tournament.create.title' | translate }}</h1>
      </header>

      <div class="flex flex-col gap-1">
        <label class="pts-label" for="t-name">{{ 'tournament.create.name.label' | translate }}</label>
        <input id="t-name" type="text" maxlength="200" required
               class="pts-input" [(ngModel)]="name"
               [placeholder]="'tournament.create.name.placeholder' | translate" />
        <p class="text-xs text-on-surface-muted">{{ 'tournament.create.name.help' | translate }}</p>
      </div>

      <section class="flex flex-col gap-1">
        <h2 class="pts-label">{{ 'tournament.create.sport.label' | translate }}</h2>
        <pts-sport-selector [sports]="sportOptions" [selected]="sport()"
                            (sportSelected)="sport.set($event)" />
        <p class="text-xs text-on-surface-muted">{{ 'tournament.create.sport.help' | translate }}</p>
      </section>

      <section class="flex flex-col gap-1">
        <h2 class="pts-label">{{ 'tournament.create.format.label' | translate }}</h2>
        <div class="grid grid-cols-1 gap-2">
          @for (f of formats; track f.value) {
            <button type="button"
                    class="flex items-center gap-3 p-3 rounded-xl border-2 text-left transition-all"
                    [ngClass]="format() === f.value
                      ? 'border-primary bg-primary/8'
                      : 'border-border bg-surface-raised hover:border-primary/30'"
                    (click)="format.set(f.value)">
              <span class="material-symbols-rounded text-2xl"
                    [class.text-primary]="format() === f.value"
                    [class.text-on-surface-muted]="format() !== f.value">{{ f.icon }}</span>
              <span class="flex-1 min-w-0">
                <span class="block font-semibold text-sm">
                  {{ 'tournament.format.' + f.value + '.label' | translate }}
                </span>
                <span class="block text-xs text-on-surface-muted">
                  {{ 'tournament.format.' + f.value + '.description' | translate }}
                </span>
              </span>
              @if (format() === f.value) {
                <span class="material-symbols-rounded text-primary">check_circle</span>
              }
            </button>
          }
        </div>
        <p class="text-xs text-on-surface-muted">{{ 'tournament.create.format.help' | translate }}</p>
      </section>

      @if (format() === 'groupstageelimination') {
        <section class="grid grid-cols-2 gap-3">
          <label class="flex flex-col gap-1">
            <span class="text-xs text-on-surface-muted">{{ 'tournament.create.groups.groupsLabel' | translate }}</span>
            <input type="number" class="pts-input" min="2" max="8"
                   [ngModel]="groupCount()" (ngModelChange)="groupCount.set($event)" />
            <span class="text-[11px] text-on-surface-muted">{{ 'tournament.create.groups.groupsHelp' | translate }}</span>
          </label>
          <label class="flex flex-col gap-1">
            <span class="text-xs text-on-surface-muted">{{ 'tournament.create.groups.advanceLabel' | translate }}</span>
            <input type="number" class="pts-input" min="1" max="4"
                   [ngModel]="advancePerGroup()" (ngModelChange)="advancePerGroup.set($event)" />
            <span class="text-[11px] text-on-surface-muted">{{ 'tournament.create.groups.advanceHelp' | translate }}</span>
          </label>
          <p class="col-span-2 text-xs"
             [class.text-on-surface-muted]="groupConfigValid()"
             [class.text-error]="!groupConfigValid()">
            @if (groupConfigValid()) {
              {{ 'tournament.create.groups.constraint' | translate }}
            } @else {
              {{ 'tournament.create.groups.invalid' | translate }}
            }
          </p>
        </section>
      }

      <div class="flex gap-2 pt-2">
        <button type="button" class="pts-btn-secondary flex-1" (click)="cancel()">
          {{ 'common.cancel' | translate }}
        </button>
        <button type="button" class="pts-btn-primary flex-1"
                [disabled]="!canSubmit() || submitting()" (click)="submit()">
          @if (submitting()) {
            <span class="material-symbols-rounded animate-spin text-lg">progress_activity</span>
          } @else {
            <span class="material-symbols-rounded text-lg">arrow_forward</span>
          }
          <span>{{ 'tournament.create.submit' | translate }}</span>
        </button>
      </div>
    </div>
  `,
})
export class CreateTournamentComponent {
  private readonly service = inject(TournamentService);
  private readonly router = inject(Router);
  private readonly notifications = inject(NotificationService);
  private readonly i18n = inject(TranslateService);

  readonly sportOptions = Object.values(SPORT_CONFIGS).filter((s) => s.type !== 'custom');
  readonly formats = TOURNAMENT_FORMATS;

  readonly name = signal('');
  readonly sport = signal<SportType | null>('volleyball');
  readonly format = signal<TournamentFormat | null>('singleelimination');
  readonly groupCount = signal<number>(2);
  readonly advancePerGroup = signal<number>(2);
  readonly submitting = signal(false);

  readonly groupConfigValid = computed(() =>
    this.format() !== 'groupstageelimination' ||
    isValidGroupConfig(this.groupCount(), this.advancePerGroup()),
  );

  readonly canSubmit = computed(() =>
    this.name().trim().length > 0
    && !!this.sport()
    && !!this.format()
    && this.groupConfigValid(),
  );

  cancel(): void { void this.router.navigate(['/tournaments']); }

  async submit(): Promise<void> {
    if (!this.canSubmit()) return;
    this.submitting.set(true);
    try {
      const res = await this.service.create({
        name: this.name().trim(),
        sportType: this.sport()!,
        format: this.format()!,
        beachAutoSwitchSides: true,
        groupCount: this.format() === 'groupstageelimination' ? this.groupCount() : null,
        advancePerGroup: this.format() === 'groupstageelimination' ? this.advancePerGroup() : null,
      });
      this.notifications.success(this.i18n.instant('tournament.create.success'));
      await this.router.navigate(['/tournaments', res.tournament.id]);
    } catch (err: any) {
      this.notifications.error(err?.error?.detail ?? this.i18n.instant('tournament.create.failure'));
    } finally {
      this.submitting.set(false);
    }
  }
}
