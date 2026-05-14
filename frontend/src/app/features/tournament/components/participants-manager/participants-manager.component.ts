import { Component, computed, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import {
  TournamentFormat,
  TournamentParticipant,
  minTeamsForFormat,
} from '../../../../shared/models/tournament.model';
import { TournamentService } from '../../services/tournament.service';
import { NotificationService } from '../../../../core/services/notification.service';

@Component({
  selector: 'pts-participants-manager',
  imports: [FormsModule, TranslatePipe],
  templateUrl: './participants-manager.component.html',
})
export class ParticipantsManagerComponent {
  private readonly service = inject(TournamentService);
  private readonly notifications = inject(NotificationService);
  private readonly i18n = inject(TranslateService);

  readonly tournamentId = input.required<string>();
  readonly participants = input.required<TournamentParticipant[]>();
  readonly canEdit = input<boolean>(false);
  readonly format = input<TournamentFormat | null>(null);
  readonly groupCount = input<number | null>(null);
  readonly changed = output<void>();
  readonly shuffleChanged = output<boolean>();

  readonly adding = signal(false);
  readonly shuffleUnseeded = signal(false);
  readonly minTeams = computed(() => {
    const f = this.format();
    return f ? minTeamsForFormat(f, this.groupCount()) : 2;
  });

  newName = '';
  newSeed: number | null = null;

  onShuffleToggle(checked: boolean): void {
    this.shuffleUnseeded.set(checked);
    this.shuffleChanged.emit(checked);
  }

  async add(): Promise<void> {
    const name = this.newName.trim();
    if (!name) return;
    this.adding.set(true);
    try {
      await this.service.addParticipant(this.tournamentId(), name, this.newSeed ?? null);
      this.newName = '';
      this.newSeed = null;
      this.changed.emit();
    } catch (err: any) {
      this.notifications.error(err?.error?.detail ?? this.i18n.instant('tournament.participants.addError'));
    } finally {
      this.adding.set(false);
    }
  }

  async remove(p: TournamentParticipant): Promise<void> {
    try {
      await this.service.removeParticipant(this.tournamentId(), p.id);
      this.changed.emit();
    } catch (err: any) {
      this.notifications.error(err?.error?.detail ?? this.i18n.instant('tournament.participants.removeError'));
    }
  }
}
