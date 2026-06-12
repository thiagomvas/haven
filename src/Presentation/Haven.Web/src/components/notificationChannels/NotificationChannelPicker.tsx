import { Webhook } from 'lucide-react';
import type { NotificationChannel } from '@/api/types';
import styles from './NotificationChannelPicker.module.css';

interface ChannelOption {
  channel: NotificationChannel;
  label: string;
  description: string;
  icon: React.ReactNode;
}

const CHANNEL_OPTIONS: ChannelOption[] = [
  {
    channel: 'Webhook',
    label: 'Webhook',
    description: 'Send HTTP POST requests to a custom URL',
    icon: <Webhook size={28} />,
  },
];

interface NotificationChannelPickerProps {
  value: NotificationChannel;
  onChange: (channel: NotificationChannel) => void;
  disabled?: boolean;
}

export function NotificationChannelPicker({ value, onChange, disabled }: NotificationChannelPickerProps) {
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
          <div className={styles.icon}>{opt.icon}</div>
          <span className={styles.label}>{opt.label}</span>
          <span className={styles.description}>{opt.description}</span>
        </button>
      ))}
    </div>
  );
}
