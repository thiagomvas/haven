import { useTranslation } from 'react-i18next';

import { ConfigurationPageLayout } from '@/components/layout/ConfigurationPageLayout';
import { useCurrentUser } from '@/hooks/useCurrentUser';
import { usePermission } from '@/hooks/usePermission';
import { useSetBreadcrumbs } from '@/hooks/useSetBreadcrumbs';
import { useUrlState } from '@/hooks/useUrlState';

import { AboutPage } from './AboutPage';
import { BackupsPage } from './BackupsPage';
import { ConfigurationManifestPage } from './ConfigurationManifestPage';
import { GitHubAppPage } from './GitHubAppPage';
import { InstancePage } from './InstancePage';
import { MaintenancePage } from './MaintenancePage';
import { SslCertificatesPage } from './SslCertificatesPage';
import { TelemetryPage } from './TelemetryPage';
import { UsersPage } from './UsersPage';

export function SettingsPage() {
  const { t } = useTranslation('settings');
  const { t: tc } = useTranslation('common');
  const currentUser = useCurrentUser();
  useSetBreadcrumbs([{ label: t('title') }]);

  const isAdmin = currentUser?.isAdmin ?? false;
  const canReadUsers = usePermission('system.read_users');
  const canReadProjects = usePermission('projects.read');
  const [selectedTab, setSelectedTab] = useUrlState('tab', 'about');

  const sections = [
    {
      id: 'general',
      label: tc('labels.general'),
      items: [
        { id: 'about', label: t('menu.about'), content: <AboutPage /> },
        ...(isAdmin
          ? [{ id: 'instance', label: t('menu.instance'), content: <InstancePage /> }]
          : []),
        ...(isAdmin ? [{ id: 'backups', label: t('menu.backups'), content: <BackupsPage /> }] : []),
        ...(canReadUsers ? [{ id: 'users', label: t('menu.users'), content: <UsersPage /> }] : []),
        ...(canReadProjects
          ? [
              {
                id: 'ssl-certificates',
                label: t('menu.sslCertificates'),
                content: <SslCertificatesPage />,
              },
            ]
          : []),
      ],
    },
    ...(isAdmin
      ? [
          {
            id: 'advanced',
            label: tc('labels.advanced'),
            items: [
              {
                id: 'config-manifest',
                label: t('menu.configManifest'),
                content: <ConfigurationManifestPage />,
              },
              {
                id: 'telemetry',
                label: t('menu.telemetry'),
                content: <TelemetryPage />,
              },
              {
                id: 'github-app',
                label: t('menu.githubApp'),
                content: <GitHubAppPage />,
              },
              {
                id: 'maintenance',
                label: t('menu.maintenance'),
                content: <MaintenancePage />,
              },
            ],
          },
        ]
      : []),
  ];

  return (
    <ConfigurationPageLayout
      mainHeader={<h1>{t('title')}</h1>}
      configHeader={<h1>{t('title')}</h1>}
      sections={sections}
      isConfigOpen
      hideCloseButton
      hideConfigButton
      selectedMenuId={selectedTab}
      onSelectedMenuIdChange={setSelectedTab}
    >
      {null}
    </ConfigurationPageLayout>
  );
}
