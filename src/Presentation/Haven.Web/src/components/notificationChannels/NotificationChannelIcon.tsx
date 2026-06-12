import { Bell, Webhook } from 'lucide-react';
import type { NotificationChannel } from '@/api/types';

interface NotificationChannelIconProps {
  channel: NotificationChannel;
  size?: number;
}

export function NotificationChannelIcon({ channel, size = 28 }: NotificationChannelIconProps) {
  switch (channel) {
    case 'Webhook':
      return <Webhook size={size} />;
    default:
      return <Bell size={size} />;
  }
}
