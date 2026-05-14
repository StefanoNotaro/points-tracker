import { Component, input, output, signal, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'pts-team-name-editor',
  imports: [FormsModule],
  templateUrl: './team-name-editor.component.html',
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
