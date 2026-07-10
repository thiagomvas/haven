import { ChevronLeft, ChevronRight, Eye } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import type { NotificationAttemptDto, NotificationDeliveryStatus } from '@/api/types';
import { Badge } from '@/components/ui/Badge';
import { SelectInput } from '@/components/ui/SelectInput';
import { Spinner } from '@/components/ui/Spinner';
import {
  useAllNotificationAttempts,
  useNotificationChannels,
} from '@/hooks/useNotificationChannels';
import styles from '@/styles/components/notifications/AttemptsTab.module.css';

import { AttemptPayloadModal } from './AttemptPayloadModal';
import { NotificationChannelIcon } from './NotificationChannelIcon';

const PAGE_SIZE = 20;

const statusVariant: Record<NotificationDeliveryStatus, 'success' | 'danger' | 'warning'> = {
  Delivered: 'success',
  Failed: 'danger',
  Pending: 'warning',
};

const formatter = new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' });

export function AttemptsTab() {
  const { t } = useTranslation(['notificationChannels', 'common']);
  const [page, setPage] = useState(1);
  const [channelFilter, setChannelFilter] = useState('');
  const [selectedAttempt, setSelectedAttempt] = useState<NotificationAttemptDto | null>(null);

  const { data: channelsData } = useNotificationChannels({ pageSize: 100 });

  const { data, isLoading, error } = useAllNotificationAttempts({
    channelConfigId: channelFilter || undefined,
    pageNumber: page,
    pageSize: PAGE_SIZE,
  });

  const handleFilterChange = (value: string) => {
    setChannelFilter(value);
    setPage(1);
  };

  const channelOptions =
    channelsData?.items.map(config => ({ value: config.id, label: config.name })) ?? [];

  return (
    <>
      <div className={styles.tabHeader}>
        <p className={styles.subtitle}>{t('attemptsTab.subtitle')}</p>
        <div className={styles.filter}>
          <SelectInput
            label={t('attemptsTab.filterLabel')}
            placeholder={t('attemptsTab.allChannels')}
            value={channelFilter}
            onChange={handleFilterChange}
            options={channelOptions}
          />
        </div>
      </div>

      {isLoading && (
        <div className={styles.loadingContainer}>
          <Spinner />
        </div>
      )}

      {!isLoading && error && (
        <div className={styles.errorContainer}>
          <div className={styles.errorMessage}>{t('attemptsTab.loadError')}</div>
        </div>
      )}

      {!isLoading && !error && (!data || data.items.length === 0) && (
        <div className={styles.emptyContainer}>{t('attemptsTab.empty')}</div>
      )}

      {!isLoading && !error && data && data.items.length > 0 && (
        <>
          <div className={styles.tableContainer}>
            <table className={styles.table}>
              <thead>
                <tr>
                  <th className={styles.th}>{t('attemptsTab.columns.channel')}</th>
                  <th className={styles.th}>{t('attempts.columns.status')}</th>
                  <th className={styles.th}>{t('attempts.columns.event')}</th>
                  <th className={styles.th}>{t('attempts.columns.time')}</th>
                  <th className={styles.th}>{t('attempts.columns.error')}</th>
                  <th className={styles.th} />
                </tr>
              </thead>
              <tbody>
                {data.items.map(attempt => (
                  <tr key={attempt.id}>
                    <td className={styles.td}>
                      <span className={styles.channelCell}>
                        <NotificationChannelIcon channel={attempt.channel} size={16} />
                        {attempt.channelConfigName}
                      </span>
                    </td>
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
                    <td className={styles.td}>
                      <button
                        className={styles.viewButton}
                        onClick={() => setSelectedAttempt(attempt)}
                        title={t('attemptsTab.viewPayload')}
                        aria-label={t('attemptsTab.viewPayload')}
                        type="button"
                      >
                        <Eye size={16} />
                      </button>
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

      <AttemptPayloadModal attempt={selectedAttempt} onClose={() => setSelectedAttempt(null)} />
    </>
  );
}
