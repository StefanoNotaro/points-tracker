import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NgClass } from '@angular/common';
import { Router } from '@angular/router';
import { TournamentService } from '../../services/tournament.service';
import { NotificationService } from '../../../../core/services/notification.service';
import { SportSelectorComponent } from '../../../../shared/components/sport-selector/sport-selector.component';
import { SPORT_CONFIGS, SportType } from '../../../../shared/models/sport.model';
import { TournamentFormat, TOURNAMENT_FORMATS } from '../../../../shared/models/tournament.model';

@Component({
  selector: 'pts-create-tournament',
  imports: [FormsModule, NgClass, SportSelectorComponent],
  template: `
    <div class="flex flex-col gap-5 pb-8">
      <header class="flex flex-col gap-1">
        <p class="text-xs uppercase tracking-wide text-on-surface-muted">New tournament</p>
        <h1 class="text-xl sm:text-2xl font-bold text-on-surface">Set it up</h1>
      </header>

      <div class="flex flex-col gap-2">
        <label class="pts-label" for="t-name">Tournament name</label>
        <input id="t-name" type="text" maxlength="200"
               class="pts-input" [(ngModel)]="name" placeholder="Summer Open 2026" />
      </div>

      <section class="flex flex-col gap-2">
        <h2 class="pts-label">Sport</h2>
        <pts-sport-selector [sports]="sportOptions" [selected]="sport()"
                            (sportSelected)="sport.set($event)" />
      </section>

      <section class="flex flex-col gap-2">
        <h2 class="pts-label">Format</h2>
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
                <span class="block font-semibold text-sm">{{ f.label }}</span>
                <span class="block text-xs text-on-surface-muted">{{ f.description }}</span>
              </span>
              @if (format() === f.value) {
                <span class="material-symbols-rounded text-primary">check_circle</span>
              }
            </button>
          }
        </div>
      </section>

      @if (format() === 'groupstageelimination') {
        <section class="grid grid-cols-2 gap-3">
          <label class="flex flex-col gap-1">
            <span class="text-xs text-on-surface-muted">Groups</span>
            <input type="number" class="pts-input" min="2" max="8"
                   [ngModel]="groupCount()" (ngModelChange)="groupCount.set($event)" />
          </label>
          <label class="flex flex-col gap-1">
            <span class="text-xs text-on-surface-muted">Advance per group</span>
            <input type="number" class="pts-input" min="1" max="4"
                   [ngModel]="advancePerGroup()" (ngModelChange)="advancePerGroup.set($event)" />
          </label>
          <p class="col-span-2 text-xs text-on-surface-muted">
            (Groups × advance) must be a power of two — e.g. 2×2 = 4, 2×4 = 8, 4×2 = 8.
          </p>
        </section>
      }

      <div class="flex gap-2 pt-2">
        <button type="button" class="pts-btn-secondary flex-1" (click)="cancel()">Cancel</button>
        <button type="button" class="pts-btn-primary flex-1"
                [disabled]="!canSubmit() || submitting()" (click)="submit()">
          @if (submitting()) {
            <span class="material-symbols-rounded animate-spin text-lg">progress_activity</span>
          } @else {
            <span class="material-symbols-rounded text-lg">arrow_forward</span>
          }
          <span>Create</span>
        </button>
      </div>
    </div>
  `,
})
export class CreateTournamentComponent {
  private readonly service = inject(TournamentService);
  private readonly router = inject(Router);
  private readonly notifications = inject(NotificationService);

  readonly sportOptions = Object.values(SPORT_CONFIGS).filter((s) => s.type !== 'custom');
  readonly formats = TOURNAMENT_FORMATS;

  readonly name = signal('');
  readonly sport = signal<SportType | null>('volleyball');
  readonly format = signal<TournamentFormat | null>('singleelimination');
  readonly groupCount = signal<number>(2);
  readonly advancePerGroup = signal<number>(2);
  readonly submitting = signal(false);

  readonly canSubmit = computed(() =>
    this.name().trim().length > 0 && !!this.sport() && !!this.format(),
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
      this.notifications.success('Tournament created.');
      await this.router.navigate(['/tournaments', res.tournament.id]);
    } catch (err: any) {
      this.notifications.error(err?.error?.detail ?? 'Could not create tournament.');
    } finally {
      this.submitting.set(false);
    }
  }
}
