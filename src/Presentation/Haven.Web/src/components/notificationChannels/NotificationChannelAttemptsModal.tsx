import { ChevronLeft, ChevronRight } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import type { NotificationDeliveryStatus } from '@/api/types';
import { Badge } from '@/components/ui/Badge';
import { Modal } from '@/components/ui/Modal';
import { Spinner } from '@/components/ui/Spinner';
import { useNotificationAttempts } from '@/hooks/useNotificationChannels';
import styles from '@/styles/components/notifications/NotificationChannelAttemptsModal.module.css';

interface NotificationChannelAttemptsModalProps {
  channelConfigId: string | null;
  channelName: string;
  onClose: () => void;
}

const PAGE_SIZE = 10;

const statusVariant: Record<NotificationDeliveryStatus, 'success' | 'danger' | 'warning'> = {
  Delivered: 'success',
  Failed: 'danger',
  Pending: 'warning',
};

const formatter = new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' });

export function NotificationChannelAttemptsModal({
  channelConfigId,
  channelName,
  onClose,
}: NotificationChannelAttemptsModalProps) {
  const { t } = useTranslation(['notificationChannels', 'common']);
  const [page, setPage] = useState(1);

  const { data, isLoading, error } = useNotificationAttempts(channelConfigId, {
    pageNumber: page,
    pageSize: PAGE_SIZE,
  });

  const handleClose = () => {
    setPage(1);
    onClose();
  };

  return (
    <Modal
      isOpen={channelConfigId !== null}
      onClose={handleClose}
      title={t('attempts.modalTitle', { name: channelName })}
      size="lg"
    >
      {isLoading && (
        <div className={styles.loadingContainer}>
          <Spinner />
        </div>
      )}

      {!isLoading && error && (
        <div className={styles.errorContainer}>{t('attempts.loadError')}</div>
      )}

      {!isLoading && !error && (!data || data.items.length === 0) && (
        <div className={styles.emptyContainer}>{t('attempts.empty')}</div>
      )}

      {!isLoading && !error && data && data.items.length > 0 && (
        <>
          <div className={styles.tableContainer}>
            <table className={styles.table}>
              <thead>
                <tr>
                  <th className={styles.th}>{t('attempts.columns.status')}</th>
                  <th className={styles.th}>{t('attempts.columns.event')}</th>
                  <th className={styles.th}>{t('attempts.columns.time')}</th>
                  <th className={styles.th}>{t('attempts.columns.error')}</th>
                </tr>
              </thead>
              <tbody>
                {data.items.map(attempt => (
                  <tr key={attempt.id}>
                    <td className={styles.td}>
                      <Badge variant={statusVariant[attempt.status]}>
                        {t(`attempts.status.${attempt.status}` as any)}
                      </Badge>
                    </td>
                    <td className={styles.td}>{attempt.eventType}</td>
                    <td className={`${styles.td} ${styles.timeCell}`}>
                      {attempt.attemptedAt ? (
                        formatter.format(new Date(attempt.attemptedAt))
                      ) : (
                        <span className={styles.dash}>-</span>
                      )}
                    </td>
                    <td className={styles.td}>
                      {attempt.errorMessage ? (
                        <span className={styles.errorCell} title={attempt.errorMessage}>
                          {attempt.errorMessage}
                        </span>
                      ) : (
                        <span className={styles.dash}>-</span>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {data.totalPages > 1 && (
            <div className={styles.pagination}>
              <button
                className={styles.paginationButton}
                onClick={() => setPage(p => Math.max(1, p - 1))}
                disabled={!data.hasPreviousPage}
              >
                <ChevronLeft size={18} />
              </button>
              <span className={styles.paginationInfo}>
                {t('common:labels.pageOf', { current: data.pageNumber, total: data.totalPages })}
              </span>
              <button
                className={styles.paginationButton}
                onClick={() => setPage(p => p + 1)}
                disabled={!data.hasNextPage}
              >
                <ChevronRight size={18} />
              </button>
            </div>
          )}
        </>
      )}
    </Modal>
  );
}
