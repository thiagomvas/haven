import type enCommon from './locales/en/common.json'
import type enLayout from './locales/en/layout.json'
import type enDashboard from './locales/en/dashboard.json'
import type enProjects from './locales/en/projects.json'
import type enEnvironments from './locales/en/environments.json'
import type enServices from './locales/en/services.json'
import type enEvents from './locales/en/events.json'

declare module 'i18next' {
  interface CustomTypeOptions {
    defaultNS: 'common'
    resources: {
      common: typeof enCommon
      layout: typeof enLayout
      dashboard: typeof enDashboard
      projects: typeof enProjects
      environments: typeof enEnvironments
      services: typeof enServices
      events: typeof enEvents
    }
  }
}
