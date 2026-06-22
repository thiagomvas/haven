import { useTranslation } from 'react-i18next';
import { useSetBreadcrumbs } from '@/hooks/useSetBreadcrumbs';
import { ConfigurationPageLayout } from '@/components/layout/ConfigurationPageLayout';
import { useCurrentUser } from '@/hooks/useCurrentUser';
import { AboutPage } from './AboutPage';
import { BackupsPage } from './BackupsPage';
import { ConfigurationManifestPage } from './ConfigurationManifestPage';
import { InstancePage } from './InstancePage';
import { UsersPage } from './UsersPage';
import { usePermission } from '@/hooks/usePermission';

export function SettingsPage() {
  const { t } = useTranslation('settings');
  const { t: tc } = useTranslation('common');
  const currentUser = useCurrentUser();
  useSetBreadcrumbs([{ label: t('title') }]);

  const isAdmin = currentUser?.isAdmin ?? false;
  const canReadUsers = usePermission('system.read_users');

  const sections = [
    {
      id: 'general',
      label: tc('labels.general'),
      items: [
        { id: 'about', label: t('menu.about'), content: <AboutPage /> },
        ...(isAdmin ? [{ id: 'instance', label: t('menu.instance'), content: <InstancePage /> }] : []),
        ...(isAdmin ? [{ id: 'backups', label: t('menu.backups'), content: <BackupsPage /> }] : []),
        ...(canReadUsers ? [{ id: 'users', label: t('menu.users'), content: <UsersPage /> }] : []),
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
    >
      {null}
    </ConfigurationPageLayout>
  );
}
