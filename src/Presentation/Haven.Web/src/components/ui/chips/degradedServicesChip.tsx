import { Container, FileCode, Layers, Terminal } from 'lucide-react';
import { useTranslation } from 'react-i18next';

import { ServiceType } from '@/api/types/service.types';

import { Chip } from '../Chip';
import { HealthIndicator } from '../HealthIndicator';

export interface DegradedServicesChipProps {
  count: number;
}

export function DegradedServicesChip({ count }: DegradedServicesChipProps) {
  const { t } = useTranslation('common');

  if (count <= 0) {
    return null;
  }

  return (
    <Chip
      icon={<HealthIndicator health="degraded" />}
      content={`${count} ${t('statuses.degraded')}`}
      size="sm"
      borderColor="var(--color-degraded)"
      textColor="var(--color-degraded)"
    />
  );
}
