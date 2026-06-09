import { clsx, type ClassValue } from 'clsx';
import type { TFunction } from 'i18next';
import i18n from '@/i18n';

export const cn = (...inputs: ClassValue[]) => clsx(inputs);

export const formatDate = (iso: string) =>
  new Intl.DateTimeFormat(i18n.language, {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(iso));

export const formatRelative = (iso: string, t: TFunction<'common'>): string => {
  const diff = Date.now() - new Date(iso).getTime();
  if (diff < 60_000) return t('time.justNow');
  if (diff < 3_600_000) return t('time.minutesAgo', { count: Math.floor(diff / 60_000) });
  if (diff < 86_400_000) return t('time.hoursAgo', { count: Math.floor(diff / 3_600_000) });
  return t('time.daysAgo', { count: Math.floor(diff / 86_400_000) });
};

export const getStatusColor = (status: string): 'success' | 'warning' | 'danger' | 'default' => {
  switch (status) {
    case 'Running':
      return 'success';
    case 'Stopped':
      return 'default';
    case 'Degraded':
      return 'warning';
    case 'DeploymentPending':
      return 'default';
    case 'Unknown':
      return 'default';
    default:
      return 'default';
  }
};
