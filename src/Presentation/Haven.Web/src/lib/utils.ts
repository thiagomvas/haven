import { type ClassValue, clsx } from 'clsx';
import type { TFunction } from 'i18next';

import { TimeFormat } from '@/api/setup';
import i18n from '@/i18n';

export const cn = (...inputs: ClassValue[]) => clsx(inputs);

const asUtc = (iso: string) => (/Z|[+-]\d{2}:?\d{2}$/.test(iso) ? iso : iso + 'Z');

export const formatDate = (iso: string, timezone?: string, timeFormat?: TimeFormat) =>
  new Intl.DateTimeFormat(i18n.language, {
    dateStyle: 'medium',
    timeStyle: 'short',
    ...(timezone ? { timeZone: timezone } : {}),
    ...(timeFormat ? { hour12: timeFormat === TimeFormat.Hour12 } : {}),
  }).format(new Date(asUtc(iso)));

export const formatRelative = (iso: string, t: TFunction<'common'>): string => {
  const diff = Date.now() - new Date(asUtc(iso)).getTime();
  const abs = Math.abs(diff);
  const future = diff < 0;

  if (abs < 10_000) return t('time.justNow');

  if (future) {
    if (abs < 3_600_000) return t('time.inMinutes', { count: Math.floor(abs / 60_000) });
    if (abs < 86_400_000) return t('time.inHours', { count: Math.floor(abs / 3_600_000) });
    if (abs < 604_800_000) return t('time.inDays', { count: Math.floor(abs / 86_400_000) });
    if (abs < 2_592_000_000) return t('time.inWeeks', { count: Math.floor(abs / 604_800_000) });
    if (abs < 31_536_000_000) return t('time.inMonths', { count: Math.floor(abs / 2_592_000_000) });
    return t('time.inYears', { count: Math.floor(abs / 31_536_000_000) });
  }

  if (abs < 3_600_000) return t('time.minutesAgo', { count: Math.floor(abs / 60_000) });
  if (abs < 86_400_000) return t('time.hoursAgo', { count: Math.floor(abs / 3_600_000) });
  if (abs < 604_800_000) return t('time.daysAgo', { count: Math.floor(abs / 86_400_000) });
  if (abs < 2_592_000_000) return t('time.weeksAgo', { count: Math.floor(abs / 604_800_000) });
  if (abs < 31_536_000_000) return t('time.monthsAgo', { count: Math.floor(abs / 2_592_000_000) });
  return t('time.yearsAgo', { count: Math.floor(abs / 31_536_000_000) });
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
