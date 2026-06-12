import { useState } from 'react';
import { ChevronLeft, ChevronRight, Bell } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { useNotificationChannels, useDeleteNotificationChannel, useSetNotificationChannelEnabled } from '@/hooks/useNotificationChannels';
import { useSetBreadcrumbs } from '@/hooks/useSetBreadcrumbs';
import { usePermission } from '@/hooks/usePermission';
import { Button } from '@/components/ui/Button';
import { Spinner } from '@/components/ui/Spinner';
import { NotificationChannelCard } from '@/components/notificationChannels/NotificationChannelCard';
import { CreateNotificationChannelModal } from '@/components/notificationChannels/CreateNotificationChannelModal';
import type { NotificationChannelConfigDto } from '@/api/types';
import styles from './NotificationChannelsPage.module.css';

export function NotificationChannelsPage() {
  const { t } = useTranslation(['notificationChannels', 'common']);

  useSetBreadcrumbs([{ label: t('page.title') }]);
  const [currentPage, setCurrentPage] = useState(1);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editConfig, setEditConfig] = useState<NotificationChannelConfigDto | undefined>(undefined);
  const canView = usePermission('system.read_notifications');
  const canCreate = usePermission('system.manage_notifications');
  const deleteChannel = useDeleteNotificationChannel();
  const setEnabled = useSetNotificationChannelEnabled();

  const { data, isLoading, error } = useNotificationChannels({
    pageNumber: currentPage,
    pageSize: 12,
  });

  const handleModalClose = () => {
    setIsModalOpen(false);
    setEditConfig(undefined);
  };

  const handleEdit = (config: NotificationChannelConfigDto) => {
    setEditConfig(config);
    setIsModalOpen(true);
  };

  if (!canView) return null;

  if (isLoading) {
    return (
      <div className={styles.container}>
        <div className={styles.header}>
          <div className={styles.headerContent}>
            <h1 className={styles.title}>{t('page.title')}</h1>
          </div>
          {canCreate && (
            <Button onClick={() => setIsModalOpen(true)} disabled>
              {t('page.addChannel')}
            </Button>
          )}
        </div>
        <div className={styles.loadingContainer}>
          <Spinner />
        </div>
        <CreateNotificationChannelModal isOpen={isModalOpen} onClose={handleModalClose} editConfig={editConfig} />
      </div>
    );
  }

  if (error) {
    return (
      <div className={styles.container}>
        <div className={styles.header}>
          <div className={styles.headerContent}>
            <h1 className={styles.title}>{t('page.title')}</h1>
          </div>
          {canCreate && (
            <Button onClick={() => setIsModalOpen(true)}>{t('page.addChannel')}</Button>
          )}
        </div>
        <div className={styles.errorContainer}>
          <div className={styles.errorMessage}>{t('page.loadError')}</div>
        </div>
        <CreateNotificationChannelModal isOpen={isModalOpen} onClose={handleModalClose} editConfig={editConfig} />
      </div>
    );
  }

  if (!data || data.items.length === 0) {
    return (
      <div className={styles.container}>
        <div className={styles.header}>
          <div className={styles.headerContent}>
            <h1 className={styles.title}>{t('page.title')}</h1>
          </div>
          {canCreate && (
            <Button onClick={() => setIsModalOpen(true)}>{t('page.addChannel')}</Button>
          )}
        </div>
        <div className={styles.emptyContainer}>
          <div className={styles.emptyIcon}>
            <Bell size={64} />
          </div>
          <h2 className={styles.emptyTitle}>{t('page.empty.title')}</h2>
          <p className={styles.emptyDescription}>{t('page.empty.description')}</p>
          {canCreate && (
            <Button onClick={() => setIsModalOpen(true)}>{t('page.addChannel')}</Button>
          )}
        </div>
        <CreateNotificationChannelModal isOpen={isModalOpen} onClose={handleModalClose} editConfig={editConfig} />
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <div className={styles.headerContent}>
          <h1 className={styles.title}>{t('page.title')}</h1>
          <p className={styles.subtitle}>
            {t('page.channelCount', { count: data.totalCount })}
          </p>
        </div>
        {canCreate && (
          <Button onClick={() => setIsModalOpen(true)}>{t('page.addChannel')}</Button>
        )}
      </div>

      <div className={styles.grid}>
        {data.items.map(config => (
          <NotificationChannelCard
            key={config.id}
            config={config}
            onEdit={canCreate ? handleEdit : undefined}
            onToggleEnabled={canCreate ? (id, enabled) => setEnabled.mutateAsync({ id, enabled }) : undefined}
            onDelete={canCreate ? (id) => deleteChannel.mutateAsync(id) : undefined}
          />
        ))}
      </div>

      {data.totalPages > 1 && (
        <div className={styles.pagination}>
          <button
            className={styles.paginationButton}
            onClick={() => setCurrentPage(p => Math.max(1, p - 1))}
            disabled={!data.hasPreviousPage}
          >
            <ChevronLeft size={18} />
          </button>
          <span className={styles.paginationInfo}>
            {t('common:labels.pageOf', { current: data.pageNumber, total: data.totalPages })}
          </span>
          <button
            className={styles.paginationButton}
            onClick={() => setCurrentPage(p => p + 1)}
            disabled={!data.hasNextPage}
          >
            <ChevronRight size={18} />
          </button>
        </div>
      )}

      <CreateNotificationChannelModal isOpen={isModalOpen} onClose={handleModalClose} editConfig={editConfig} />
    </div>
  );
}
