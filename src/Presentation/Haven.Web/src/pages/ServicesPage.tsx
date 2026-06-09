import { useTranslation } from 'react-i18next';
import { useParams } from 'react-router-dom';
import { useSetBreadcrumbs } from '@/hooks/useSetBreadcrumbs';

export function ServicesPage() {
  const { t } = useTranslation('services');
  const { projectId, environmentId } = useParams<{ projectId: string; environmentId: string }>();

  useSetBreadcrumbs([
    { label: 'Projects', to: '/projects' },
    { label: '…', to: projectId ? `/projects/${projectId}` : undefined },
    {
      label: '…',
      to:
        projectId && environmentId
          ? `/projects/${projectId}/environments/${environmentId}`
          : undefined,
    },
    { label: 'Services' },
  ]);

  return (
    <div>
      <h1>{t('title')}</h1>
      <p>{t('comingSoon')}</p>
    </div>
  );
}
