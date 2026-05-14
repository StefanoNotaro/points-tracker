import { Component, computed, inject, signal } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { AuthService } from '../../core/auth/auth.service';
import { ThemeService } from '../../core/services/theme.service';
import { SUPPORTED_LOCALES } from '../../core/i18n/i18n.providers';

const LANG_STORAGE_KEY = 'pts_lang';

@Component({
  selector: 'pts-settings',
  imports: [TranslatePipe],
  templateUrl: './settings.component.html',
})
export class SettingsComponent {
  readonly auth  = inject(AuthService);
  readonly theme = inject(ThemeService);
  private readonly i18n = inject(TranslateService);

  readonly locales = [...SUPPORTED_LOCALES];
  readonly currentLang = signal(this.i18n.currentLang ?? 'en');

  setLang(lang: string): void {
    this.i18n.use(lang);
    this.currentLang.set(lang);
    try { localStorage.setItem(LANG_STORAGE_KEY, lang); } catch { /* ignore */ }
  }
}
