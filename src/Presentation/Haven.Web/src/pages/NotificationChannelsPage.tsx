import { useState } from 'react';
import { ChevronLeft, ChevronRight, Bell } from 'lucide-react';
import { useNotificationChannels } from '@/hooks/useNotificationChannels';
import { useSetBreadcrumbs } from '@/hooks/useSetBreadcrumbs';
import { usePermission } from '@/hooks/usePermission';
import { Button } from '@/components/ui/Button';
import { Spinner } from '@/components/ui/Spinner';
import { NotificationChannelCard } from '@/components/notificationChannels/NotificationChannelCard';
import { CreateNotificationChannelModal } from '@/components/notificationChannels/CreateNotificationChannelModal';
import styles from './NotificationChannelsPage.module.css';

export function NotificationChannelsPage() {
  useSetBreadcrumbs([{ label: 'Notifications' }]);
  const [currentPage, setCurrentPage] = useState(1);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const canView = usePermission('system.read_notifications');
  const canCreate = usePermission('system.manage_notifications');

  const { data, isLoading, error } = useNotificationChannels({
    pageNumber: currentPage,
    pageSize: 12,
  });

  const handleModalClose = () => {
    setIsModalOpen(false);
  };

  if (!canView) return null;

  if (isLoading) {
    return (
      <div className={styles.container}>
        <div className={styles.header}>
          <div className={styles.headerContent}>
            <h1 className={styles.title}>Notification Channels</h1>
          </div>
          {canCreate && (
            <Button onClick={() => setIsModalOpen(true)} disabled>
              Add Channel
            </Button>
          )}
        </div>
        <div className={styles.loadingContainer}>
          <Spinner />
        </div>
        <CreateNotificationChannelModal isOpen={isModalOpen} onClose={handleModalClose} />
      </div>
    );
  }

  if (error) {
    return (
      <div className={styles.container}>
        <div className={styles.header}>
          <div className={styles.headerContent}>
            <h1 className={styles.title}>Notification Channels</h1>
          </div>
          {canCreate && <Button onClick={() => setIsModalOpen(true)}>Add Channel</Button>}
        </div>
        <div className={styles.errorContainer}>
          <div className={styles.errorMessage}>
            {error instanceof Error ? error.message : 'Failed to load notification channels'}
          </div>
        </div>
        <CreateNotificationChannelModal isOpen={isModalOpen} onClose={handleModalClose} />
      </div>
    );
  }

  if (!data || data.items.length === 0) {
    return (
      <div className={styles.container}>
        <div className={styles.header}>
          <div className={styles.headerContent}>
            <h1 className={styles.title}>Notification Channels</h1>
          </div>
          {canCreate && <Button onClick={() => setIsModalOpen(true)}>Add Channel</Button>}
        </div>
        <div className={styles.emptyContainer}>
          <div className={styles.emptyIcon}>
            <Bell size={64} />
          </div>
          <h2 className={styles.emptyTitle}>No notification channels</h2>
          <p className={styles.emptyDescription}>
            Add a webhook to receive notifications about your services.
          </p>
          {canCreate && <Button onClick={() => setIsModalOpen(true)}>Add Channel</Button>}
        </div>
        <CreateNotificationChannelModal isOpen={isModalOpen} onClose={handleModalClose} />
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <div className={styles.headerContent}>
          <h1 className={styles.title}>Notification Channels</h1>
          <p className={styles.subtitle}>
            {data.totalCount} {data.totalCount === 1 ? 'channel' : 'channels'} configured
          </p>
        </div>
        {canCreate && <Button onClick={() => setIsModalOpen(true)}>Add Channel</Button>}
      </div>

      <div className={styles.grid}>
        {data.items.map(config => (
          <NotificationChannelCard key={config.id} config={config} />
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
            Page {data.pageNumber} of {data.totalPages}
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

      <CreateNotificationChannelModal isOpen={isModalOpen} onClose={handleModalClose} />
    </div>
  );
}
