import { ArrowDownAZ, ArrowUpAZ, Bell, ChevronLeft, ChevronRight, Search } from 'lucide-react';
import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';

import type { NotificationChannelConfigDto, NotificationChannelConfigSortBy } from '@/api/types';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { SelectInput } from '@/components/ui/SelectInput';
import { Spinner } from '@/components/ui/Spinner';
import {
  useDeleteNotificationChannel,
  useNotificationChannels,
  useSetNotificationChannelEnabled,
  useSetSystemDefaultNotificationChannel,
  useTestNotificationChannel,
} from '@/hooks/useNotificationChannels';
import { usePermission } from '@/hooks/usePermission';
import styles from '@/styles/components/notifications/ProvidersTab.module.css';

import { CreateNotificationChannelModal } from './CreateNotificationChannelModal';
import { NotificationChannelAttemptsModal } from './NotificationChannelAttemptsModal';
import { NotificationChannelCard } from './NotificationChannelCard';

export function ProvidersTab() {
  const { t } = useTranslation(['notificationChannels', 'common']);
  const [currentPage, setCurrentPage] = useState(1);
  const [search, setSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [sortBy, setSortBy] = useState<NotificationChannelConfigSortBy>('Name');
  const [sortAscending, setSortAscending] = useState(true);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editConfig, setEditConfig] = useState<NotificationChannelConfigDto | undefined>(undefined);
  const [attemptsChannelId, setAttemptsChannelId] = useState<string | null>(null);
  const [attemptsChannelName, setAttemptsChannelName] = useState('');
  const canCreate = usePermission('system.manage_notifications');
  const deleteChannel = useDeleteNotificationChannel();
  const setEnabled = useSetNotificationChannelEnabled();
  const testChannel = useTestNotificationChannel();
  const setSystemDefault = useSetSystemDefaultNotificationChannel();

  useEffect(() => {
    const id = setTimeout(() => {
      setDebouncedSearch(search);
      setCurrentPage(1);
    }, 300);
    return () => clearTimeout(id);
  }, [search]);

  const sortOptions = [
    { value: 'Name', label: t('page.sort.name') },
    { value: 'Channel', label: t('page.sort.channel') },
    { value: 'Enabled', label: t('page.sort.enabled') },
  ];

  const { data, isLoading, error } = useNotificationChannels({
    pageNumber: currentPage,
    pageSize: 12,
    search: debouncedSearch || undefined,
    sortBy,
    sortAscending,
  });

  const toolbar = (
    <div className={styles.toolbar}>
      <div className={styles.searchWrapper}>
        <Search size={16} className={styles.searchIcon} />
        <Input
          placeholder={t('page.searchPlaceholder')}
          value={search}
          onChange={e => setSearch(e.target.value)}
          className={styles.searchInput}
        />
      </div>
      <div className={styles.sortSelect}>
        <SelectInput
          options={sortOptions}
          value={sortBy}
          onChange={value => setSortBy(value as NotificationChannelConfigSortBy)}
        />
      </div>
      <button
        type="button"
        className={styles.sortDirectionButton}
        onClick={() => setSortAscending(v => !v)}
        aria-label={sortAscending ? t('page.sort.ascendingLabel') : t('page.sort.descendingLabel')}
        title={sortAscending ? t('page.sort.ascendingLabel') : t('page.sort.descendingLabel')}
      >
        {sortAscending ? <ArrowDownAZ size={18} /> : <ArrowUpAZ size={18} />}
      </button>
    </div>
  );

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

  const hasActiveSearch = debouncedSearch.trim().length > 0;

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
        {hasActiveSearch && toolbar}
        <div className={styles.emptyContainer}>
          <div className={styles.emptyIcon}>
            <Bell size={64} />
          </div>
          <h2 className={styles.emptyTitle}>
            {hasActiveSearch ? t('page.noResults.title') : t('page.empty.title')}
          </h2>
          <p className={styles.emptyDescription}>
            {hasActiveSearch ? t('page.noResults.description') : t('page.empty.description')}
          </p>
          {!hasActiveSearch && canCreate && (
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

      {toolbar}

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
            onSetSystemDefault={canCreate ? id => setSystemDefault.mutateAsync(id) : undefined}
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
