import i18n from 'i18next'
import { initReactI18next } from 'react-i18next'
import LanguageDetector from 'i18next-browser-languagedetector'

import enCommon from './locales/en/common.json'
import enLayout from './locales/en/layout.json'
import enDashboard from './locales/en/dashboard.json'
import enProjects from './locales/en/projects.json'
import enEnvironments from './locales/en/environments.json'
import enServices from './locales/en/services.json'
import enEvents from './locales/en/events.json'
import enPages from './locales/en/pages.json'
import enGitCredentials from './locales/en/gitCredentials.json'

export const SUPPORTED_LANGUAGES = ['en'] as const
export type SupportedLanguage = typeof SUPPORTED_LANGUAGES[number]
export const DEFAULT_LANGUAGE: SupportedLanguage = 'en'
export const LANGUAGE_STORAGE_KEY = 'haven-language'

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
      },
    },
    fallbackLng: DEFAULT_LANGUAGE,
    supportedLngs: SUPPORTED_LANGUAGES,
    detection: {
      order: ['localStorage', 'navigator'],
      lookupLocalStorage: LANGUAGE_STORAGE_KEY,
      cacheUserLanguage: true,
    },
    defaultNS: 'common',
    ns: ['common', 'layout', 'dashboard', 'projects', 'environments', 'services', 'events', 'pages', 'gitCredentials'],
    interpolation: { escapeValue: false },
    saveMissing: import.meta.env.DEV,
    missingKeyHandler: import.meta.env.DEV
      ? (_l, ns, key) => console.warn(`[i18n] Missing key: ${ns}:${key}`)
      : undefined,
  })

export default i18n
