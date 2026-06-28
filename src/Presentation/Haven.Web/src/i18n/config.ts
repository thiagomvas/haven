import i18n from 'i18next';
import LanguageDetector from 'i18next-browser-languagedetector';
import { initReactI18next } from 'react-i18next';

import enCommon from './locales/en/common.json';
import enDashboard from './locales/en/dashboard.json';
import enEnvironments from './locales/en/environments.json';
import enEvents from './locales/en/events.json';
import enGitCredentials from './locales/en/gitCredentials.json';
import enLayout from './locales/en/layout.json';
import enNotificationChannels from './locales/en/notificationChannels.json';
import enPages from './locales/en/pages.json';
import enProjects from './locales/en/projects.json';
import enServiceRegistry from './locales/en/serviceRegistry.json';
import enServices from './locales/en/services.json';
import enSettings from './locales/en/settings.json';

export const SUPPORTED_LANGUAGES = ['en'] as const;
export type SupportedLanguage = (typeof SUPPORTED_LANGUAGES)[number];
export const DEFAULT_LANGUAGE: SupportedLanguage = 'en';
export const LANGUAGE_STORAGE_KEY = 'haven-language';

i18n
  .use(LanguageDetector)
  .use(initReactI18next)
  .init({
    resources: {
      en: {
        common: enCommon,
        layout: enLayout,
        dashboard: enDashboard,
        projects: enProjects,
        environments: enEnvironments,
        services: enServices,
        events: enEvents,
        pages: enPages,
        gitCredentials: enGitCredentials,
        settings: enSettings,
        notificationChannels: enNotificationChannels,
        serviceRegistry: enServiceRegistry,
      },
    },
    fallbackLng: DEFAULT_LANGUAGE,
    supportedLngs: SUPPORTED_LANGUAGES,
    detection: {
      order: ['localStorage', 'navigator'],
      lookupLocalStorage: LANGUAGE_STORAGE_KEY,
      caches: ['localStorage'],
    },
    defaultNS: 'common',
    ns: [
      'common',
      'layout',
      'dashboard',
      'projects',
      'environments',
      'services',
      'events',
      'pages',
      'gitCredentials',
      'settings',
      'notificationChannels',
      'serviceRegistry',
    ],
    interpolation: { escapeValue: false },
    saveMissing: import.meta.env.DEV,
    missingKeyHandler: import.meta.env.DEV
      ? (_l, ns, key) => console.warn(`[i18n] Missing key: ${ns}:${key}`)
      : undefined,
  });

export default i18n;
