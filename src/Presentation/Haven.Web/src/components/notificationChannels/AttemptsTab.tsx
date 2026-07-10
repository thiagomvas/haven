import { ChevronLeft, ChevronRight, Eye } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import type { NotificationAttemptDto, NotificationDeliveryStatus } from '@/api/types';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/layout/Table';
import { Badge } from '@/components/ui/Badge';
import { Button } from '@/components/ui/Button';
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
          <Table hoverable striped>
            <TableHead>
              <TableRow isHeader hasActionsColumn>
                <TableHeader>{t('attemptsTab.columns.channel')}</TableHeader>
                <TableHeader>{t('attempts.columns.status')}</TableHeader>
                <TableHeader>{t('attempts.columns.event')}</TableHeader>
                <TableHeader>{t('attempts.columns.time')}</TableHeader>
                <TableHeader>{t('attempts.columns.error')}</TableHeader>
              </TableRow>
            </TableHead>
            <TableBody>
              {data.items.map(attempt => (
                <TableRow
                  key={attempt.id}
                  actions={
                    <Button
                      variant="text"
                      size="xs"
                      icon={<Eye size={14} />}
                      onClick={() => setSelectedAttempt(attempt)}
                      title={t('attemptsTab.viewPayload')}
                      aria-label={t('attemptsTab.viewPayload')}
                    />
                  }
                >
                  <TableCell>
                    <span className={styles.channelCell}>
                      <NotificationChannelIcon channel={attempt.channel} size={16} />
                      {attempt.channelConfigName}
                    </span>
                  </TableCell>
                  <TableCell>
                    <Badge variant={statusVariant[attempt.status]}>
                      {t(`attempts.status.${attempt.status}` as any)}
                    </Badge>
                  </TableCell>
                  <TableCell>{attempt.eventType}</TableCell>
                  <TableCell nowrap variant="muted">
                    {attempt.attemptedAt ? (
                      formatter.format(new Date(attempt.attemptedAt))
                    ) : (
                      <span className={styles.dash}>-</span>
                    )}
                  </TableCell>
                  <TableCell>
                    {attempt.errorMessage ? (
                      <span className={styles.errorCell} title={attempt.errorMessage}>
                        {attempt.errorMessage}
                      </span>
                    ) : (
                      <span className={styles.dash}>-</span>
                    )}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>

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
