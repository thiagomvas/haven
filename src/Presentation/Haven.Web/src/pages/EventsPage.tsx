import { useTranslation } from 'react-i18next';

import { useSetBreadcrumbs } from '@/hooks/useSetBreadcrumbs';

export function EventsPage() {
  const { t } = useTranslation('events');

  useSetBreadcrumbs([{ label: 'Events' }]);

  return (
    <div>
      <h1>{t('title')}</h1>
      <p>{t('comingSoon')}</p>
    </div>
  );
}
