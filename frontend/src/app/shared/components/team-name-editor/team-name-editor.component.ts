import { Component, input, output, signal, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'pts-team-name-editor',
  imports: [FormsModule],
  template: `
    <div class="flex items-center gap-2">
      @if (editing()) {
        <input
          type="text"
          class="border-b-2 border-primary bg-transparent text-on-surface text-center font-semibold
                 outline-none w-full max-w-[160px] pb-1 text-sm"
          [ngModel]="draft()"
          (ngModelChange)="draft.set($event)"
          (blur)="commit()"
          (keydown.enter)="commit()"
          (keydown.escape)="cancel()"
          [attr.aria-label]="editLabel()"
          #input
        />
      } @else {
        <button
          type="button"
          class="text-sm font-semibold text-on-surface hover:text-primary transition-colors flex items-center gap-1"
          (click)="startEditing()"
          [attr.aria-label]="editLabelNamed()"
          [disabled]="!canEdit()"
        >
          {{ teamName() }}
          @if (canEdit()) {
            <span class="material-symbols-rounded text-base text-on-surface-muted">edit</span>
          }
        </button>
      }
    </div>
  `,
})
export class TeamNameEditorComponent {
  teamName = input.required<string>();
  canEdit = input(false);
  nameChanged = output<string>();

  private readonly i18n = inject(TranslateService);

  editing = signal(false);
  draft = signal('');

  editLabel(): string { return this.i18n.instant('counter.team.editAria'); }
  editLabelNamed(): string {
    return this.i18n.instant('counter.team.editAriaNamed', { name: this.teamName() });
  }

  startEditing(): void {
    if (!this.canEdit()) return;
    this.draft.set(this.teamName());
    this.editing.set(true);
  }

  commit(): void {
    const name = this.draft().trim();
    if (name && name !== this.teamName()) {
      this.nameChanged.emit(name);
    }
    this.editing.set(false);
  }

  cancel(): void {
    this.editing.set(false);
  }
}
