import { Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { SportSelectorComponent } from '../../../../shared/components/sport-selector/sport-selector.component';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { CounterService } from '../../services/counter.service';
import { NotificationService } from '../../../../core/services/notification.service';
import { SportType, SPORT_CONFIGS } from '../../../../shared/models/sport.model';

@Component({
  selector: 'pts-create-counter',
  imports: [ReactiveFormsModule, SportSelectorComponent, LoadingSpinnerComponent],
  template: `
    <div class="max-w-md mx-auto py-8">
      <h1 class="text-2xl font-bold text-on-surface mb-2">New Counter</h1>
      <p class="text-on-surface-muted mb-8">Choose a sport and name your teams to get started.</p>

      <form [formGroup]="form" (ngSubmit)="submit()" class="flex flex-col gap-6">
        <section>
          <h2 class="text-sm font-semibold text-on-surface-muted uppercase tracking-wide mb-3">Sport</h2>
          <pts-sport-selector
            [sports]="sports"
            [selected]="form.value.sportType ?? null"
            (sportSelected)="form.patchValue({ sportType: $event })"
          />
          @if (form.controls.sportType.invalid && form.controls.sportType.touched) {
            <p class="text-error text-sm mt-2">Please select a sport.</p>
          }
        </section>

        <section class="flex flex-col gap-3">
          <h2 class="text-sm font-semibold text-on-surface-muted uppercase tracking-wide mb-1">Teams</h2>

          <div>
            <label class="text-sm text-on-surface-muted mb-1 block" for="teamA">Team A</label>
            <input
              id="teamA"
              type="text"
              formControlName="teamAName"
              maxlength="100"
              placeholder="Team A"
              class="w-full border border-border rounded-md px-3 py-2 bg-surface text-on-surface
                     focus:outline-none focus:border-primary focus:ring-1 focus:ring-primary text-sm"
            />
          </div>

          <div>
            <label class="text-sm text-on-surface-muted mb-1 block" for="teamB">Team B</label>
            <input
              id="teamB"
              type="text"
              formControlName="teamBName"
              maxlength="100"
              placeholder="Team B"
              class="w-full border border-border rounded-md px-3 py-2 bg-surface text-on-surface
                     focus:outline-none focus:border-primary focus:ring-1 focus:ring-primary text-sm"
            />
          </div>
        </section>

        <button
          type="submit"
          class="pts-btn-primary w-full py-3 text-base"
          [disabled]="form.invalid || submitting()"
        >
          @if (submitting()) {
            <pts-loading-spinner size="sm" />
          } @else {
            Start Counter
          }
        </button>
      </form>
    </div>
  `,
})
export class CreateCounterComponent {
  private readonly router = inject(Router);
  private readonly counterService = inject(CounterService);
  private readonly notifications = inject(NotificationService);
  private readonly fb = inject(FormBuilder);

  readonly sports = Object.values(SPORT_CONFIGS);
  readonly submitting = signal(false);

  readonly form = this.fb.group({
    sportType: this.fb.control<SportType | null>(null, Validators.required),
    teamAName: ['Team A', [Validators.required, Validators.maxLength(100)]],
    teamBName: ['Team B', [Validators.required, Validators.maxLength(100)]],
  });

  async submit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    try {
      const { counter } = await this.counterService.create({
        sportType: this.form.value.sportType!,
        teamAName: this.form.value.teamAName!,
        teamBName: this.form.value.teamBName!,
      });
      await this.router.navigate(['/counter', counter.id]);
    } catch {
      this.notifications.error('Failed to create counter. Please try again.');
    } finally {
      this.submitting.set(false);
    }
  }
}
