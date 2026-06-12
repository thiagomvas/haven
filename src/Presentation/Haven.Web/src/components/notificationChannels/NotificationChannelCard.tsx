import { Bell, Pencil } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { NotificationChannelConfigDto, WebhookNotificationConfig } from '@/api/types';
import styles from './NotificationChannelCard.module.css';

interface NotificationChannelCardProps {
  config: NotificationChannelConfigDto;
  onEdit?: (config: NotificationChannelConfigDto) => void;
}

export function NotificationChannelCard({ config, onEdit }: NotificationChannelCardProps) {
  const { t } = useTranslation(['notificationChannels', 'common']);

  let webhookUrl: string | undefined;
  if (config.channel === 'Webhook') {
    try {
      const parsed: WebhookNotificationConfig = JSON.parse(config.config);
      webhookUrl = parsed.url;
    } catch {
      // ignore malformed stored config
    }
  }

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
      </div>

      <div className={styles.cardFooter}>
        <span className={`${styles.statusBadge} ${config.enabled ? styles.enabled : styles.disabled}`}>
          <span className={styles.statusDot} />
          {config.enabled ? t('common:labels.enabled') : t('common:labels.disabled')}
        </span>
        <span className={styles.rulesCount}>
          {t('card.rules', { count: config.rulesCount })}
        </span>
      </div>
    </div>
  );
}
