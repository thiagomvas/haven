import type enCommon from './locales/en/common.json';
import type enDashboard from './locales/en/dashboard.json';
import type enEnvironments from './locales/en/environments.json';
import type enEvents from './locales/en/events.json';
import type enGitCredentials from './locales/en/gitCredentials.json';
import type enLayout from './locales/en/layout.json';
import type enNotificationChannels from './locales/en/notificationChannels.json';
import type enPages from './locales/en/pages.json';
import type enProjects from './locales/en/projects.json';
import type enServiceRegistry from './locales/en/serviceRegistry.json';
import type enServices from './locales/en/services.json';
import type enSettings from './locales/en/settings.json';

declare module 'i18next' {
  interface CustomTypeOptions {
    defaultNS: 'common';
    resources: {
      common: typeof enCommon;
      layout: typeof enLayout;
      dashboard: typeof enDashboard;
      projects: typeof enProjects;
      environments: typeof enEnvironments;
      services: typeof enServices;
      events: typeof enEvents;
      pages: typeof enPages;
      gitCredentials: typeof enGitCredentials;
      settings: typeof enSettings;
      notificationChannels: typeof enNotificationChannels;
      serviceRegistry: typeof enServiceRegistry;
    };
  }
}
