import { useTranslation } from 'react-i18next';

import { Badge } from '@/components/ui/Badge';
import { Tooltip } from '@/components/ui/Tooltip';

/** Marks a panel that renders mock data until its endpoint exists. Delete when wired up. */
export function SampleDataTag() {
  const { t } = useTranslation('dashboard');
  return (
    <Tooltip content={t('sample.hint')} direction="below" wrap>
      <Badge variant="default">{t('sample.label')}</Badge>
    </Tooltip>
  );
}
