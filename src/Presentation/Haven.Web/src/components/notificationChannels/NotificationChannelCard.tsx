import {
  CheckCircle,
  History,
  Mail,
  MoreVertical,
  Pencil,
  Send,
  Trash2,
  XCircle,
} from 'lucide-react';
import { useEffect, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';

import { NotificationChannelConfigDto } from '@/api/types';
import { DiscordNotificationConfig } from '@/api/types';
import { WebhookNotificationConfig } from '@/api/types';
import { Row } from '@/components/layout/Row';
import { Stack } from '@/components/layout/Stack';
import { Badge } from '@/components/ui/Badge';
import { Button } from '@/components/ui/Button';
import { Modal } from '@/components/ui/Modal';
import { ToggleChip } from '@/components/ui/ToggleChip';
import { Tooltip } from '@/components/ui/Tooltip';
import styles from '@/styles/components/notifications/NotificationChannelCard.module.css';

import { NotificationChannelIcon } from './NotificationChannelIcon';

type TestResult = { success: boolean; response: string | null; errorMessage: string | null };

interface NotificationChannelCardProps {
  config: NotificationChannelConfigDto;
  onEdit?: (config: NotificationChannelConfigDto) => void;
  onToggleEnabled?: (id: string, enabled: boolean) => Promise<void>;
  onDelete?: (id: string) => Promise<void>;
  onTest?: (id: string) => Promise<TestResult>;
  onViewHistory?: (config: NotificationChannelConfigDto) => void;
  onSetSystemDefault?: (id: string) => Promise<void>;
}

export function NotificationChannelCard({
  config,
  onEdit,
  onToggleEnabled,
  onDelete,
  onTest,
  onViewHistory,
  onSetSystemDefault,
}: NotificationChannelCardProps) {
  const { t } = useTranslation(['notificationChannels', 'common']);
  const [isDeleteConfirmOpen, setIsDeleteConfirmOpen] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);
  const [deleteError, setDeleteError] = useState<string | undefined>(undefined);
  const [isTogglingEnabled, setIsTogglingEnabled] = useState(false);
  const [isTesting, setIsTesting] = useState(false);
  const [isSettingSystemDefault, setIsSettingSystemDefault] = useState(false);
  const [testResult, setTestResult] = useState<TestResult | null>(null);
  const [isMenuOpen, setIsMenuOpen] = useState(false);
  const testClearTimer = useRef<ReturnType<typeof setTimeout> | null>(null);
  const menuRef = useRef<HTMLDivElement>(null);

  const handleSetSystemDefault = async () => {
    if (!onSetSystemDefault || isSettingSystemDefault) return;
    try {
      setIsSettingSystemDefault(true);
      await onSetSystemDefault(config.id);
    } finally {
      setIsSettingSystemDefault(false);
    }
  };

  let tip: string | undefined;
  if (config.channel === 'Webhook') {
    try {
      const parsed: WebhookNotificationConfig = JSON.parse(config.config);
      tip = parsed.url;
    } catch {
      // ignore malformed stored config
    }
  }

  useEffect(
    () => () => {
      if (testClearTimer.current) clearTimeout(testClearTimer.current);
    },
    []
  );

  useEffect(() => {
    if (!isMenuOpen) return;
    const handleClickOutside = (e: MouseEvent) => {
      if (menuRef.current && !menuRef.current.contains(e.target as Node)) {
        setIsMenuOpen(false);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, [isMenuOpen]);

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

  const canSetSystemDefault =
    config.channel === 'Smtp' && !config.isSystemDefault && !!onSetSystemDefault;
  const hasMenuItems = !!onViewHistory || canSetSystemDefault || !!onEdit || !!onDelete;

  return (
    <div className={styles.card}>
      <Row gap="4" align="flex-start" className={styles.cardHeader}>
        <div className={styles.iconContainer}>
          <NotificationChannelIcon channel={config.channel} size={28} />
        </div>

        <Stack gap="1">
          <h3 className={styles.name}>{config.name}</h3>
          <Row gap="2" wrap>
            <Badge>{t(`channels.${config.channel.toLowerCase()}.label` as any)}</Badge>
            {config.channel === 'Smtp' && config.isSystemDefault && (
              <Tooltip content={t('smtp.systemDefaultTooltip')} direction="above">
                <Badge variant="primary">{t('smtp.systemDefaultBadge')}</Badge>
              </Tooltip>
            )}
          </Row>
          {tip && <p className={styles.url}>{tip}</p>}
        </Stack>

        <Row gap="1" className={styles.cardActions}>
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
          {hasMenuItems && (
            <div className={styles.menuContainer} ref={menuRef}>
              <button
                type="button"
                className={styles.menuButton}
                onClick={() => setIsMenuOpen(open => !open)}
                aria-label={t('common:actions.moreOptions')}
                aria-haspopup="menu"
                aria-expanded={isMenuOpen}
              >
                <MoreVertical size={14} />
              </button>
              {isMenuOpen && (
                <div className={styles.menu} role="menu">
                  {onViewHistory && (
                    <button
                      type="button"
                      className={styles.menuItem}
                      role="menuitem"
                      onClick={() => {
                        setIsMenuOpen(false);
                        onViewHistory(config);
                      }}
                    >
                      <History size={14} />
                      {t('attempts.ariaLabel')}
                    </button>
                  )}
                  {canSetSystemDefault && (
                    <button
                      type="button"
                      className={styles.menuItem}
                      role="menuitem"
                      onClick={() => {
                        setIsMenuOpen(false);
                        handleSetSystemDefault();
                      }}
                      disabled={isSettingSystemDefault}
                    >
                      <Mail size={14} />
                      {t('smtp.setSystemDefault')}
                    </button>
                  )}
                  {onEdit && (
                    <button
                      type="button"
                      className={styles.menuItem}
                      role="menuitem"
                      onClick={() => {
                        setIsMenuOpen(false);
                        onEdit(config);
                      }}
                    >
                      <Pencil size={14} />
                      {t('common:actions.edit')}
                    </button>
                  )}
                  {onDelete && (
                    <button
                      type="button"
                      className={`${styles.menuItem} ${styles.menuItemDanger}`}
                      role="menuitem"
                      onClick={() => {
                        setIsMenuOpen(false);
                        setIsDeleteConfirmOpen(true);
                      }}
                    >
                      <Trash2 size={14} />
                      {t('common:actions.delete')}
                    </button>
                  )}
                </div>
              )}
            </div>
          )}
        </Row>
      </Row>

      <Row justify="space-between" className={styles.cardFooter}>
        <ToggleChip
          checked={config.enabled}
          onLabel={t('common:labels.enabled')}
          offLabel={t('common:labels.disabled')}
          onChange={onToggleEnabled ? e => handleToggleEnabled(e) : undefined}
          disabled={isTogglingEnabled}
        />
        {isTesting && <span className={styles.testStatus}>{t('test.testing')}</span>}
        {!isTesting && testResult && (
          <span className={testResult.success ? styles.testSuccess : styles.testFailure}>
            {testResult.success ? (
              <>
                <CheckCircle size={12} /> {t('test.success')}
              </>
            ) : (
              <>
                <XCircle size={12} /> {testResult.errorMessage ?? t('test.error')}
              </>
            )}
          </span>
        )}
        {!isTesting && !testResult && (
          <span className={styles.rulesCount}>{t('card.rules', { count: config.rulesCount })}</span>
        )}
      </Row>

      <Modal
        isOpen={isDeleteConfirmOpen}
        onClose={() => !isDeleting && setIsDeleteConfirmOpen(false)}
        title={t('delete.confirmTitle')}
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
