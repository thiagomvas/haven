import { Bell, Webhook } from 'lucide-react';
import type { NotificationChannel } from '@/api/types';
import { SiDiscord } from '@icons-pack/react-simple-icons';

interface NotificationChannelIconProps {
  channel: NotificationChannel;
  size?: number;
}

export function NotificationChannelIcon({ channel, size = 28 }: NotificationChannelIconProps) {
  switch (channel) {
    case 'Webhook':
      return <Webhook size={size} />;
    case 'Discord':
      return <SiDiscord size={size} />;
    default:
      return <Bell size={size} />;
  }
}
