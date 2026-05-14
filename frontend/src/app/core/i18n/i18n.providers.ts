import { EnvironmentProviders, Provider, inject, provideAppInitializer } from '@angular/core';
import { TranslateService, provideTranslateService } from '@ngx-translate/core';
import { provideTranslateHttpLoader } from '@ngx-translate/http-loader';
import { firstValueFrom } from 'rxjs';

export const SUPPORTED_LOCALES = ['en', 'es', 'ca'] as const;
export const DEFAULT_LOCALE = 'it';
export const LANG_STORAGE_KEY = 'pts_lang';

function isSupported(tag: string): boolean {
  return (SUPPORTED_LOCALES as readonly string[]).includes(tag);
}

/**
 * Resolve which locale to start the app in. Honour the user's explicit
 * choice (saved by the settings page), otherwise fall back to the closest
 * supported browser locale, otherwise English.
 */
function detectInitialLocale(): string {
  try {
    const stored = localStorage.getItem(LANG_STORAGE_KEY);
    if (stored && isSupported(stored)) return stored;
  } catch { /* localStorage may be unavailable */ }

  const candidates = [
    ...(typeof navigator !== 'undefined' && navigator.languages ? navigator.languages : []),
    typeof navigator !== 'undefined' ? navigator.language : '',
  ];
  for (const c of candidates) {
    if (!c) continue;
    const tag = c.toLowerCase().split('-')[0];
    if (isSupported(tag)) return tag;
  }
  return DEFAULT_LOCALE;
}

export function provideI18n(): (EnvironmentProviders | Provider)[] {
  return [
    provideTranslateService({
      fallbackLang: DEFAULT_LOCALE,
    }),
    provideTranslateHttpLoader({ prefix: '/i18n/', suffix: '.json' }),
    provideAppInitializer(() => {
      const translate = inject(TranslateService);
      translate.addLangs([...SUPPORTED_LOCALES]);
      translate.setFallbackLang(DEFAULT_LOCALE);
      const detectedLocale = detectInitialLocale();
      console.debug(`[i18n] Initializing with locale: ${detectedLocale}`);

      // If using a non-default locale, preload the default language as fallback
      let loadPromise: Promise<any>;
      if (detectedLocale !== DEFAULT_LOCALE) {
        loadPromise = firstValueFrom(translate.get(DEFAULT_LOCALE)).then(() =>
          firstValueFrom(translate.use(detectedLocale))
        );
      } else {
        loadPromise = firstValueFrom(translate.use(detectedLocale));
      }

      return loadPromise.then(() => {
        console.debug(`[i18n] Locale ${detectedLocale} loaded successfully`);
        console.debug(`[i18n] Available languages: ${translate.getLangs().join(', ')}`);
        console.debug(`[i18n] Current language: ${translate.currentLang}`);
      }).catch((err: unknown) => {
        console.error(`[i18n] Failed to load locale ${detectedLocale}:`, err);
        // Fallback to default locale if not already using it
        if (detectedLocale !== DEFAULT_LOCALE) {
          return firstValueFrom(translate.use(DEFAULT_LOCALE)).then(() => {
            console.debug(`[i18n] Fallback to ${DEFAULT_LOCALE} successful`);
          });
        }
        throw err;
      });
    }),
  ];
}
