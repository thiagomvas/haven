import { useTranslation } from 'react-i18next';
import { useSetBreadcrumbs } from '@/hooks/useSetBreadcrumbs';
import { usePermission } from '@/hooks/usePermission';
import { useUrlState } from '@/hooks/useUrlState';
import { Tabs } from '@/components/ui/Tabs';
import { ProvidersTab } from '@/components/notificationChannels/ProvidersTab';
import { EventRoutingTab } from '@/components/notificationChannels/EventRoutingTab';
import styles from './NotificationChannelsPage.module.css';

export function NotificationChannelsPage() {
  const { t } = useTranslation('notificationChannels');
  const canView = usePermission('system.read_notifications');
  const [activeTab, setActiveTab] = useUrlState('tab', 'providers');

  useSetBreadcrumbs([{ label: t('page.title') }]);

  if (!canView) return null;

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <div className={styles.headerContent}>
          <h1 className={styles.title}>{t('page.title')}</h1>
        </div>
      </div>

      <Tabs
        activeTab={activeTab}
        onChange={setActiveTab}
        items={[
          {
            id: 'providers',
            label: t('page.tabs.providers'),
            content: <ProvidersTab />,
          },
          {
            id: 'eventRouting',
            label: t('page.tabs.eventRouting'),
            content: <EventRoutingTab />,
          },
        ]}
      />
    </div>
  );
}
