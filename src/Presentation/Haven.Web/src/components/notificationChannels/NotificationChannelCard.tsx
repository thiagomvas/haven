import { useState } from 'react';
import { Bell, Pencil, Trash2 } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { NotificationChannelConfigDto, WebhookNotificationConfig } from '@/api/types';
import { Modal } from '@/components/ui/Modal';
import { Button } from '@/components/ui/Button';
import { ToggleChip } from '@/components/ui/ToggleChip';
import styles from './NotificationChannelCard.module.css';

interface NotificationChannelCardProps {
  config: NotificationChannelConfigDto;
  onEdit?: (config: NotificationChannelConfigDto) => void;
  onToggleEnabled?: (id: string, enabled: boolean) => Promise<void>;
  onDelete?: (id: string) => Promise<void>;
}

export function NotificationChannelCard({ config, onEdit, onToggleEnabled, onDelete }: NotificationChannelCardProps) {
  const { t } = useTranslation(['notificationChannels', 'common']);
  const [isDeleteConfirmOpen, setIsDeleteConfirmOpen] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);
  const [deleteError, setDeleteError] = useState<string | undefined>(undefined);
  const [isTogglingEnabled, setIsTogglingEnabled] = useState(false);

  let webhookUrl: string | undefined;
  if (config.channel === 'Webhook') {
    try {
      const parsed: WebhookNotificationConfig = JSON.parse(config.config);
      webhookUrl = parsed.url;
    } catch {
      // ignore malformed stored config
    }
  }

  const handleToggleEnabled = async (enabled: boolean) => {
    try {
      setIsTogglingEnabled(true);
      await onToggleEnabled!(config.id, enabled);
    } finally {
      setIsTogglingEnabled(false);
    }
  };

  const handleDeleteConfirm = async () => {
    if (!onDelete) return;
    try {
      setIsDeleting(true);
      setDeleteError(undefined);
      await onDelete(config.id);
      setIsDeleteConfirmOpen(false);
    } catch {
      setDeleteError(t('delete.error'));
    } finally {
      setIsDeleting(false);
    }
  };

  return (
    <div className={styles.card}>
      <div className={styles.cardHeader}>
        <div className={styles.iconContainer}>
          <Bell size={28} />
        </div>

        <div className={styles.headerContent}>
          <h3 className={styles.name}>{config.name}</h3>
          <span className={styles.channelBadge}>{t(`channels.${config.channel.toLowerCase()}.label` as any)}</span>
          {webhookUrl && <p className={styles.url}>{webhookUrl}</p>}
        </div>

        <div className={styles.cardActions}>
          {onEdit && (
            <button
              type="button"
              className={styles.editButton}
              onClick={() => onEdit(config)}
              aria-label={t('common:actions.edit')}
            >
              <Pencil size={14} />
            </button>
          )}
          {onDelete && (
            <button
              type="button"
              className={styles.deleteButton}
              onClick={() => setIsDeleteConfirmOpen(true)}
              aria-label={t('common:actions.delete')}
            >
              <Trash2 size={14} />
            </button>
          )}
        </div>
      </div>

      <div className={styles.cardFooter}>
        <ToggleChip
          checked={config.enabled}
          onLabel={t('common:labels.enabled')}
          offLabel={t('common:labels.disabled')}
          onChange={onToggleEnabled ? e => handleToggleEnabled(e) : undefined}
          disabled={isTogglingEnabled}
        />
        <span className={styles.rulesCount}>
          {t('card.rules', { count: config.rulesCount })}
        </span>
      </div>

      <Modal
        isOpen={isDeleteConfirmOpen}
        onClose={() => !isDeleting && setIsDeleteConfirmOpen(false)}
        title={t('delete.confirmTitle')}
        size="sm"
        closeOnBackdropClick={!isDeleting}
        error={deleteError}
        footer={
          <>
            <Button variant="ghost" onClick={() => setIsDeleteConfirmOpen(false)} disabled={isDeleting}>
              {t('common:actions.cancel')}
            </Button>
            <Button variant="danger" onClick={handleDeleteConfirm} isLoading={isDeleting}>
              {isDeleting ? t('delete.deleting') : t('delete.confirm')}
            </Button>
          </>
        }
      >
        <p>{t('delete.confirmMessage', { name: config.name })}</p>
      </Modal>
    </div>
  );
}
