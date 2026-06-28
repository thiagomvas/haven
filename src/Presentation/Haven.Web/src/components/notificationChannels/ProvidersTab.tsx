import { Bell, ChevronLeft, ChevronRight } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import type { NotificationChannelConfigDto } from '@/api/types/notification.types';
import { Button } from '@/components/ui/Button';
import { Spinner } from '@/components/ui/Spinner';
import {
  useDeleteNotificationChannel,
  useNotificationChannels,
  useSetNotificationChannelEnabled,
  useTestNotificationChannel,
} from '@/hooks/useNotificationChannels';
import { usePermission } from '@/hooks/usePermission';

import { CreateNotificationChannelModal } from './CreateNotificationChannelModal';
import { NotificationChannelAttemptsModal } from './NotificationChannelAttemptsModal';
import { NotificationChannelCard } from './NotificationChannelCard';
import styles from './ProvidersTab.module.css';

export function ProvidersTab() {
  const { t } = useTranslation(['notificationChannels', 'common']);
  const [currentPage, setCurrentPage] = useState(1);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editConfig, setEditConfig] = useState<NotificationChannelConfigDto | undefined>(undefined);
  const [attemptsChannelId, setAttemptsChannelId] = useState<string | null>(null);
  const [attemptsChannelName, setAttemptsChannelName] = useState('');
  const canCreate = usePermission('system.manage_notifications');
  const deleteChannel = useDeleteNotificationChannel();
  const setEnabled = useSetNotificationChannelEnabled();
  const testChannel = useTestNotificationChannel();

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

  const handleViewHistory = (config: NotificationChannelConfigDto) => {
    setAttemptsChannelId(config.id);
    setAttemptsChannelName(config.name);
  };

  if (isLoading) {
    return (
      <>
        <div className={styles.tabHeader}>
          {canCreate && (
            <Button onClick={() => setIsModalOpen(true)} disabled>
              {t('page.addChannel')}
            </Button>
          )}
        </div>
        <div className={styles.loadingContainer}>
          <Spinner />
        </div>
        <CreateNotificationChannelModal
          isOpen={isModalOpen}
          onClose={handleModalClose}
          editConfig={editConfig}
        />
      </>
    );
  }

  if (error) {
    return (
      <>
        <div className={styles.tabHeader}>
          {canCreate && (
            <Button onClick={() => setIsModalOpen(true)}>{t('page.addChannel')}</Button>
          )}
        </div>
        <div className={styles.errorContainer}>
          <div className={styles.errorMessage}>{t('page.loadError')}</div>
        </div>
        <CreateNotificationChannelModal
          isOpen={isModalOpen}
          onClose={handleModalClose}
          editConfig={editConfig}
        />
      </>
    );
  }

  if (!data || data.items.length === 0) {
    return (
      <>
        <div className={styles.tabHeader}>
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
        <CreateNotificationChannelModal
          isOpen={isModalOpen}
          onClose={handleModalClose}
          editConfig={editConfig}
        />
      </>
    );
  }

  return (
    <>
      <div className={styles.tabHeader}>
        <p className={styles.subtitle}>{t('page.channelCount', { count: data.totalCount })}</p>
        {canCreate && <Button onClick={() => setIsModalOpen(true)}>{t('page.addChannel')}</Button>}
      </div>

      <div className={styles.grid}>
        {data.items.map(config => (
          <NotificationChannelCard
            key={config.id}
            config={config}
            onEdit={canCreate ? handleEdit : undefined}
            onToggleEnabled={
              canCreate ? (id, enabled) => setEnabled.mutateAsync({ id, enabled }) : undefined
            }
            onDelete={canCreate ? id => deleteChannel.mutateAsync(id) : undefined}
            onTest={canCreate ? id => testChannel.mutateAsync(id) : undefined}
            onViewHistory={handleViewHistory}
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

      <CreateNotificationChannelModal
        isOpen={isModalOpen}
        onClose={handleModalClose}
        editConfig={editConfig}
      />
      <NotificationChannelAttemptsModal
        channelConfigId={attemptsChannelId}
        channelName={attemptsChannelName}
        onClose={() => setAttemptsChannelId(null)}
      />
    </>
  );
}
