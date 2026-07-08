import { KeyRound, Pencil, RefreshCw, Trash2 } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import { GitCredentialDto } from '@/api/types';
import { Button } from '@/components/ui/Button';
import { Modal } from '@/components/ui/Modal';
import { ToggleChip } from '@/components/ui/ToggleChip';
import { Tooltip } from '@/components/ui/Tooltip';
import { useFormatDate } from '@/hooks/useFormatDate';
import { useStartGitHubOAuth } from '@/hooks/useGitCredentials';
import styles from '@/styles/components/git/GitCredentialCard.module.css';

import { ProviderIcon } from './ProviderIcon';

interface GitCredentialCardProps {
  credential: GitCredentialDto;
  onEdit?: (credential: GitCredentialDto) => void;
  onRotate?: (credential: GitCredentialDto) => void;
  onToggleActive?: (id: string, isActive: boolean) => Promise<void>;
  onDelete?: (id: string) => Promise<void>;
}

const PROVIDER_COLORS: Record<string, string> = {
  GitHub: '#24292e',
  GitLab: '#fc6d26',
  Bitbucket: '#0052cc',
  Gitea: '#609926',
  Generic: '#6366f1',
};

export function GitCredentialCard({
  credential,
  onEdit,
  onRotate,
  onToggleActive,
  onDelete,
}: GitCredentialCardProps) {
  const { t } = useTranslation(['gitCredentials', 'common']);
  const formatDate = useFormatDate();
  const startGitHubOAuth = useStartGitHubOAuth();

  const [isDeleteConfirmOpen, setIsDeleteConfirmOpen] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);
  const [deleteError, setDeleteError] = useState<string | undefined>(undefined);
  const [isTogglingActive, setIsTogglingActive] = useState(false);

  const providerColor = PROVIDER_COLORS[credential.providerType] || PROVIDER_COLORS.Generic;

  const getAuthMethodLabel = () => {
    if (credential.authMethod === 'Token') return t('auth.token');
    if (credential.authMethod === 'OAuth') return t('auth.oauth');
    return t('auth.ssh');
  };

  const getHostUrlDisplay = () => {
    if (!credential.hostUrl) {
      return t('card.cloudHosted');
    }
    return t('card.selfHosted', { url: credential.hostUrl });
  };

  const handleToggleActive = async (isActive: boolean) => {
    if (!onToggleActive) return;
    try {
      setIsTogglingActive(true);
      await onToggleActive(credential.id, isActive);
    } finally {
      setIsTogglingActive(false);
    }
  };

  const handleDeleteConfirm = async () => {
    if (!onDelete) return;
    try {
      setIsDeleting(true);
      setDeleteError(undefined);
      await onDelete(credential.id);
      setIsDeleteConfirmOpen(false);
    } catch {
      setDeleteError(t('card.deleteError'));
    } finally {
      setIsDeleting(false);
    }
  };

  return (
    <div className={styles.card}>
      <div className={styles.cardHeader}>
        <div className={styles.iconContainer} style={{ backgroundColor: `${providerColor}15` }}>
          <ProviderIcon provider={credential.providerType} size={28} />
        </div>

        <div className={styles.headerContent}>
          <h3 className={styles.displayName}>{credential.displayName}</h3>

          <div className={styles.badges}>
            <span className={styles.authBadge}>{getAuthMethodLabel()}</span>
          </div>

          <p className={styles.hostUrl}>{getHostUrlDisplay()}</p>
        </div>

        <div className={styles.cardActions}>
          {onEdit && (
            <Tooltip content={t('common:actions.edit')} direction="above">
              <button
                type="button"
                className={styles.editButton}
                onClick={() => onEdit(credential)}
                aria-label={t('common:actions.edit')}
              >
                <Pencil size={14} />
              </button>
            </Tooltip>
          )}
          {onRotate && credential.authMethod !== 'OAuth' && (
            <Tooltip content={t('rotate.action')} direction="above">
              <button
                type="button"
                className={styles.editButton}
                onClick={() => onRotate(credential)}
                aria-label={t('rotate.action')}
              >
                <KeyRound size={14} />
              </button>
            </Tooltip>
          )}
          {credential.authMethod === 'OAuth' && credential.providerType === 'GitHub' && (
            <Tooltip content={t('reconnect.action')} direction="above">
              <button
                type="button"
                className={styles.editButton}
                onClick={() => startGitHubOAuth.mutate(credential.id)}
                disabled={startGitHubOAuth.isPending}
                aria-label={t('reconnect.action')}
              >
                <RefreshCw size={14} />
              </button>
            </Tooltip>
          )}
          {onDelete && (
            <Tooltip content={t('common:actions.delete')} direction="above">
              <button
                type="button"
                className={styles.deleteButton}
                onClick={() => setIsDeleteConfirmOpen(true)}
                aria-label={t('common:actions.delete')}
              >
                <Trash2 size={14} />
              </button>
            </Tooltip>
          )}
        </div>
      </div>

      <div className={styles.cardFooter}>
        <ToggleChip
          checked={credential.isActive}
          onLabel={t('card.active')}
          offLabel={t('card.inactive')}
          onChange={onToggleActive ? handleToggleActive : undefined}
          disabled={isTogglingActive}
        />

        <div className={styles.dateInfo}>
          <span>{t('card.lastValidated', { date: formatDate(credential.lastValidatedAt) })}</span>
        </div>
      </div>

      <Modal
        isOpen={isDeleteConfirmOpen}
        onClose={() => !isDeleting && setIsDeleteConfirmOpen(false)}
        title={t('card.deleteTitle')}
        size="sm"
        closeOnBackdropClick={!isDeleting}
        error={deleteError}
        footer={
          <>
            <Button
              variant="ghost"
              onClick={() => setIsDeleteConfirmOpen(false)}
              disabled={isDeleting}
            >
              {t('common:actions.cancel')}
            </Button>
            <Button variant="danger" onClick={handleDeleteConfirm} isLoading={isDeleting}>
              {t('common:actions.delete')}
            </Button>
          </>
        }
      >
        <p>{t('card.deleteMessage', { name: credential.displayName })}</p>
      </Modal>
    </div>
  );
}
