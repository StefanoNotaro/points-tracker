import { Component, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TournamentParticipant } from '../../../../shared/models/tournament.model';
import { TournamentService } from '../../services/tournament.service';
import { NotificationService } from '../../../../core/services/notification.service';

@Component({
  selector: 'pts-participants-manager',
  imports: [FormsModule],
  template: `
    <div class="flex flex-col gap-3">
      @if (canEdit()) {
        <form class="flex gap-2" (submit)="$event.preventDefault(); add()">
          <input type="text" class="pts-input" maxlength="100"
                 placeholder="Team name" [(ngModel)]="newName" name="newName" />
          <input type="number" class="pts-input !w-20" min="1" max="99"
                 placeholder="Seed" [(ngModel)]="newSeed" name="newSeed" />
          <button type="submit" class="pts-btn-primary"
                  [disabled]="!newName.trim() || adding()">
            <span class="material-symbols-rounded text-lg">add</span>
          </button>
        </form>
      }

      @if (participants().length === 0) {
        <p class="text-sm text-on-surface-muted text-center py-4">
          Add at least two teams to start the bracket.
        </p>
      } @else {
        <ul class="flex flex-col gap-2">
          @for (p of participants(); track p.id) {
            <li class="pts-card !p-3 flex items-center gap-3">
              <span class="material-symbols-rounded text-on-surface-muted text-xl">groups</span>
              <span class="flex-1 min-w-0">
                <span class="block font-medium text-sm text-on-surface truncate">{{ p.teamName }}</span>
                @if (p.seed) {
                  <span class="block text-xs text-on-surface-muted">Seed #{{ p.seed }}</span>
                }
              </span>
              @if (canEdit()) {
                <button type="button" class="pts-btn-icon" (click)="remove(p)" aria-label="Remove">
                  <span class="material-symbols-rounded text-on-surface-muted hover:text-error">
                    delete_outline
                  </span>
                </button>
              }
            </li>
          }
        </ul>
      }
    </div>
  `,
})
export class ParticipantsManagerComponent {
  private readonly service = inject(TournamentService);
  private readonly notifications = inject(NotificationService);

  readonly tournamentId = input.required<string>();
  readonly participants = input.required<TournamentParticipant[]>();
  readonly canEdit = input<boolean>(false);
  readonly changed = output<void>();

  readonly adding = signal(false);
  newName = '';
  newSeed: number | null = null;

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
      this.notifications.error(err?.error?.detail ?? 'Could not add participant.');
    } finally {
      this.adding.set(false);
    }
  }

  async remove(p: TournamentParticipant): Promise<void> {
    try {
      await this.service.removeParticipant(this.tournamentId(), p.id);
      this.changed.emit();
    } catch (err: any) {
      this.notifications.error(err?.error?.detail ?? 'Could not remove participant.');
    }
  }
}
