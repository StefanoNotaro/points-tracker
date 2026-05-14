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
  templateUrl: './create-tournament.component.html',
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
