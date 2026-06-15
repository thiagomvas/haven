import { useEffect, useRef, useState } from 'react';
import { CheckCircle, History, Pencil, Send, Trash2, XCircle } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { NotificationChannelConfigDto, WebhookNotificationConfig } from '@/api/types';
import { Modal } from '@/components/ui/Modal';
import { Button } from '@/components/ui/Button';
import { ToggleChip } from '@/components/ui/ToggleChip';
import { Tooltip } from '@/components/ui/Tooltip';
import styles from './NotificationChannelCard.module.css';
import { NotificationChannelIcon } from './NotificationChannelIcon';

type TestResult = { success: boolean; response: string | null; errorMessage: string | null };

interface NotificationChannelCardProps {
  config: NotificationChannelConfigDto;
  onEdit?: (config: NotificationChannelConfigDto) => void;
  onToggleEnabled?: (id: string, enabled: boolean) => Promise<void>;
  onDelete?: (id: string) => Promise<void>;
  onTest?: (id: string) => Promise<TestResult>;
  onViewHistory?: (config: NotificationChannelConfigDto) => void;
}

export function NotificationChannelCard({ config, onEdit, onToggleEnabled, onDelete, onTest, onViewHistory }: NotificationChannelCardProps) {
  const { t } = useTranslation(['notificationChannels', 'common']);
  const [isDeleteConfirmOpen, setIsDeleteConfirmOpen] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);
  const [deleteError, setDeleteError] = useState<string | undefined>(undefined);
  const [isTogglingEnabled, setIsTogglingEnabled] = useState(false);
  const [isTesting, setIsTesting] = useState(false);
  const [testResult, setTestResult] = useState<TestResult | null>(null);
  const testClearTimer = useRef<ReturnType<typeof setTimeout> | null>(null);

  let webhookUrl: string | undefined;
  if (config.channel === 'Webhook') {
    try {
      const parsed: WebhookNotificationConfig = JSON.parse(config.config);
      webhookUrl = parsed.url;
    } catch {
      // ignore malformed stored config
    }
  }

  useEffect(() => () => { if (testClearTimer.current) clearTimeout(testClearTimer.current); }, []);

  const handleTest = async () => {
    if (!onTest || isTesting) return;
    if (testClearTimer.current) clearTimeout(testClearTimer.current);
    setIsTesting(true);
    setTestResult(null);
    try {
      const result = await onTest(config.id);
      setTestResult(result);
      testClearTimer.current = setTimeout(() => setTestResult(null), 5000);
    } catch {
      setTestResult({ success: false, response: null, errorMessage: t('test.error') });
      testClearTimer.current = setTimeout(() => setTestResult(null), 5000);
    } finally {
      setIsTesting(false);
    }
  };

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
          <NotificationChannelIcon channel={config.channel} size={28} />
        </div>

        <div className={styles.headerContent}>
          <h3 className={styles.name}>{config.name}</h3>
          <span className={styles.channelBadge}>{t(`channels.${config.channel.toLowerCase()}.label` as any)}</span>
          {webhookUrl && <p className={styles.url}>{webhookUrl}</p>}
        </div>

        <div className={styles.cardActions}>
          {onViewHistory && (
            <Tooltip content={t('attempts.ariaLabel')} direction="above">
              <button
                type="button"
                className={styles.historyButton}
                onClick={() => onViewHistory(config)}
                aria-label={t('attempts.ariaLabel')}
              >
                <History size={14} />
              </button>
            </Tooltip>
          )}
          {onTest && (
            <Tooltip content={t('test.ariaLabel')} direction="above">
              <button
                type="button"
                className={styles.testButton}
                onClick={handleTest}
                disabled={isTesting}
                aria-label={t('test.ariaLabel')}
              >
                <Send size={14} />
              </button>
            </Tooltip>
          )}
          {onEdit && (
            <Tooltip content={t('common:actions.edit')} direction="above">
              <button
                type="button"
                className={styles.editButton}
                onClick={() => onEdit(config)}
                aria-label={t('common:actions.edit')}
              >
                <Pencil size={14} />
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
          checked={config.enabled}
          onLabel={t('common:labels.enabled')}
          offLabel={t('common:labels.disabled')}
          onChange={onToggleEnabled ? e => handleToggleEnabled(e) : undefined}
          disabled={isTogglingEnabled}
        />
        {isTesting && (
          <span className={styles.testStatus}>{t('test.testing')}</span>
        )}
        {!isTesting && testResult && (
          <span className={testResult.success ? styles.testSuccess : styles.testFailure}>
            {testResult.success
              ? <><CheckCircle size={12} /> {t('test.success')}</>
              : <><XCircle size={12} /> {testResult.errorMessage ?? t('test.error')}</>
            }
          </span>
        )}
        {!isTesting && !testResult && (
          <span className={styles.rulesCount}>
            {t('card.rules', { count: config.rulesCount })}
          </span>
        )}
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
