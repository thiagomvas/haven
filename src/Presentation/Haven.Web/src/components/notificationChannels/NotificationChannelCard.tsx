import { Bell } from 'lucide-react';
import { NotificationChannelConfigDto, WebhookNotificationConfig } from '@/api/types';
import styles from './NotificationChannelCard.module.css';

interface NotificationChannelCardProps {
  config: NotificationChannelConfigDto;
}

export function NotificationChannelCard({ config }: NotificationChannelCardProps) {
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
          <span className={styles.channelBadge}>{config.channel}</span>
          {webhookUrl && <p className={styles.url}>{webhookUrl}</p>}
        </div>
      </div>

      <div className={styles.cardFooter}>
        <span className={`${styles.statusBadge} ${config.enabled ? styles.enabled : styles.disabled}`}>
          <span className={styles.statusDot} />
          {config.enabled ? 'Enabled' : 'Disabled'}
        </span>
        <span className={styles.rulesCount}>
          {config.rulesCount} {config.rulesCount === 1 ? 'rule' : 'rules'}
        </span>
      </div>
    </div>
  );
}
