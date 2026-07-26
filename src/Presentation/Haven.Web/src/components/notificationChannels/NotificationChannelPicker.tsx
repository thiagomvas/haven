import { useTranslation } from 'react-i18next';

import type { NotificationChannel } from '@/api/types';
import styles from '@/styles/components/notifications/NotificationChannelPicker.module.css';

import { NotificationChannelIcon } from './NotificationChannelIcon';

interface ChannelOption {
  channel: NotificationChannel;
  labelKey: string;
  descriptionKey: string;
}

const CHANNEL_OPTIONS: ChannelOption[] = [
  {
    channel: 'Webhook',
    labelKey: 'channels.webhook.label',
    descriptionKey: 'channels.webhook.description',
  },
  {
    channel: 'Discord',
    labelKey: 'channels.discord.label',
    descriptionKey: 'channels.discord.description',
  },
  {
    channel: 'Ntfy',
    labelKey: 'channels.ntfy.label',
    descriptionKey: 'channels.ntfy.description',
  },
];

interface NotificationChannelPickerProps {
  value: NotificationChannel;
  onChange: (channel: NotificationChannel) => void;
  disabled?: boolean;
}

export function NotificationChannelPicker({
  value,
  onChange,
  disabled,
}: NotificationChannelPickerProps) {
  const { t } = useTranslation('notificationChannels');

  return (
    <div className={styles.grid}>
      {CHANNEL_OPTIONS.map(opt => (
        <button
          key={opt.channel}
          type="button"
          className={`${styles.card} ${value === opt.channel ? styles.selected : ''}`}
          onClick={() => onChange(opt.channel)}
          disabled={disabled}
        >
          <div className={styles.icon}>
            <NotificationChannelIcon channel={opt.channel} />
          </div>
          <span className={styles.label}>{t(opt.labelKey as any)}</span>
          <span className={styles.description}>{t(opt.descriptionKey as any)}</span>
        </button>
      ))}
    </div>
  );
}
