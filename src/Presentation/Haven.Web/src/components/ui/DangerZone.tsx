import { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';

import { ConfigSection } from '@/components/layout/ConfigSection';

interface DangerZoneProps {
  children: ReactNode;
}

export function DangerZone({ children }: DangerZoneProps) {
  const { t } = useTranslation('projects');

  return (
    <ConfigSection tone="danger" title={t('dangerZone')} description={t('dangerZoneDescription')}>
      {children}
    </ConfigSection>
  );
}
