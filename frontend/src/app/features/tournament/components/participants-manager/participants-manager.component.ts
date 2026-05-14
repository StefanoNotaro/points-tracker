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
  template: `
    <div class="flex flex-col gap-3">
      @if (canEdit()) {
        <form class="flex flex-col gap-2" (submit)="$event.preventDefault(); add()">
          <div class="flex gap-2">
            <input type="text" class="pts-input flex-1" maxlength="100" required
                   [placeholder]="'tournament.participants.namePlaceholder' | translate"
                   [(ngModel)]="newName" name="newName" />
            <button type="submit" class="pts-btn-primary"
                    [disabled]="!newName.trim() || adding()"
                    [attr.aria-label]="'tournament.participants.addAria' | translate">
              <span class="material-symbols-rounded text-lg">add</span>
            </button>
          </div>

          <!-- Advanced section: hidden by default, holds seed + shuffle options -->
          <details class="rounded-lg border border-border bg-surface-raised">
            <summary class="flex items-center gap-2 px-3 py-2 cursor-pointer select-none text-sm font-medium text-on-surface">
              <span class="material-symbols-rounded text-base text-on-surface-muted">tune</span>
              <span class="flex-1">{{ 'tournament.participants.advanced.toggle' | translate }}</span>
              <span class="material-symbols-rounded text-base text-on-surface-muted transition-transform">expand_more</span>
            </summary>
            <div class="flex flex-col gap-3 p-3 pt-0">
              <p class="text-xs text-on-surface-muted">
                {{ 'tournament.participants.advanced.help' | translate }}
              </p>

              <label class="flex flex-col gap-1">
                <span class="text-xs text-on-surface-muted">
                  {{ 'tournament.participants.advanced.seedLabel' | translate }}
                </span>
                <input type="number" class="pts-input !w-32" min="1" max="99"
                       placeholder="—" [(ngModel)]="newSeed" name="newSeed" />
                <span class="text-[11px] text-on-surface-muted">
                  {{ 'tournament.participants.advanced.seedHelp' | translate }}
                </span>
              </label>

              <label class="flex items-start gap-2 cursor-pointer">
                <input type="checkbox" class="mt-0.5"
                       [checked]="shuffleUnseeded()"
                       (change)="onShuffleToggle($any($event.target).checked)"
                       name="shuffleUnseeded" />
                <span class="flex flex-col">
                  <span class="text-sm font-medium text-on-surface">
                    {{ 'tournament.participants.advanced.shuffleLabel' | translate }}
                  </span>
                  <span class="text-[11px] text-on-surface-muted">
                    {{ 'tournament.participants.advanced.shuffleHelp' | translate }}
                  </span>
                </span>
              </label>
            </div>
          </details>
        </form>
      }

      @if (participants().length === 0 || participants().length < minTeams()) {
        <p class="text-sm text-on-surface-muted text-center py-4">
          {{ 'tournament.participants.emptyMin' | translate: { min: minTeams() } }}
        </p>
      }

      @if (participants().length > 0) {
        <ul class="flex flex-col gap-2">
          @for (p of participants(); track p.id) {
            <li class="pts-card !p-3 flex items-center gap-3">
              <span class="material-symbols-rounded text-on-surface-muted text-xl">groups</span>
              <span class="flex-1 min-w-0">
                <span class="block font-medium text-sm text-on-surface truncate">{{ p.teamName }}</span>
                @if (p.seed) {
                  <span class="block text-xs text-on-surface-muted">
                    {{ 'tournament.participants.seedLabel' | translate: { seed: p.seed } }}
                  </span>
                }
              </span>
              @if (canEdit()) {
                <button type="button" class="pts-btn-icon" (click)="remove(p)"
                        [attr.aria-label]="'tournament.participants.removeAria' | translate">
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
