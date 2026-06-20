import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { ChevronLeft, ChevronRight } from 'lucide-react';
import { useGitCredentials } from '@/hooks/useGitCredentials';
import { useSetBreadcrumbs } from '@/hooks/useSetBreadcrumbs';
import { usePermission } from '@/hooks/usePermission';
import { Button } from '@/components/ui/Button';
import { Spinner } from '@/components/ui/Spinner';
import { GitCredentialCard } from '@/components/gitCredentials/GitCredentialCard';
import { CreateGitCredentialModal } from '@/components/gitCredentials/CreateGitCredentialModal';
import { SiGit } from '@icons-pack/react-simple-icons';
import styles from './GitCredentialsPage.module.css';

export function GitCredentialsPage() {
  const { t } = useTranslation(['gitCredentials', 'common']);

  useSetBreadcrumbs([{ label: 'Git Providers' }]);
  const [currentPage, setCurrentPage] = useState(1);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const canView = usePermission('system.read_git_credentials');
  const canCreate = usePermission('system.manage_git_credentials');

  const { data, isLoading, error } = useGitCredentials({
    pageNumber: currentPage,
    pageSize: 12,
  });

  const handleModalClose = () => {
    setIsModalOpen(false);
  };

  if (!canView) return null;

  // Loading state
  if (isLoading) {
    return (
      <div className={styles.container}>
        <div className={styles.header}>
          <div className={styles.headerContent}>
            <h1 className={styles.title}>{t('title')}</h1>
          </div>
          {canCreate && (
            <Button onClick={() => setIsModalOpen(true)} disabled>
              {t('addCredential')}
            </Button>
          )}
        </div>

        <div className={styles.loadingContainer}>
          <Spinner />
        </div>

        <CreateGitCredentialModal isOpen={isModalOpen} onClose={handleModalClose} />
      </div>
    );
  }

  // Error state
  if (error) {
    return (
      <div className={styles.container}>
        <div className={styles.header}>
          <div className={styles.headerContent}>
            <h1 className={styles.title}>{t('title')}</h1>
          </div>
          {canCreate && <Button onClick={() => setIsModalOpen(true)}>{t('addCredential')}</Button>}
        </div>

        <div className={styles.errorContainer}>
          <div className={styles.errorMessage}>
            {error instanceof Error ? error.message : 'Failed to load git providers'}
          </div>
        </div>

        <CreateGitCredentialModal isOpen={isModalOpen} onClose={handleModalClose} />
      </div>
    );
  }

  // Empty state
  if (!data || data.items.length === 0) {
    return (
      <div className={styles.container}>
        <div className={styles.header}>
          <div className={styles.headerContent}>
            <h1 className={styles.title}>{t('title')}</h1>
          </div>
          {canCreate && <Button onClick={() => setIsModalOpen(true)}>{t('addCredential')}</Button>}
        </div>

        <div className={styles.emptyContainer}>
          <div className={styles.emptyIcon}>
            <SiGit size={64} />{' '}
          </div>
          <h2 className={styles.emptyTitle}>{t('title')}</h2>
          <p className={styles.emptyDescription}>{t('empty')}</p>
          {canCreate && <Button onClick={() => setIsModalOpen(true)}>{t('addCredential')}</Button>}
        </div>

        <CreateGitCredentialModal isOpen={isModalOpen} onClose={handleModalClose} />
      </div>
    );
  }

  // Loaded state
  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <div className={styles.headerContent}>
          <h1 className={styles.title}>{t('title')}</h1>
          <p className={styles.subtitle}>
            {data.totalCount} {data.totalCount === 1 ? 'provider' : 'providers'} configured
          </p>
        </div>
        <Button onClick={() => setIsModalOpen(true)}>{t('addCredential')}</Button>
      </div>

      <div className={styles.grid}>
        {data.items.map(credential => (
          <GitCredentialCard key={credential.id} credential={credential} />
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
            {t('common:labels.pageOf', {
              current: data.pageNumber,
              total: data.totalPages,
            })}
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

      <CreateGitCredentialModal isOpen={isModalOpen} onClose={handleModalClose} />
    </div>
  );
}
