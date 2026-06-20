import { useTranslation } from 'react-i18next';
import { useParams } from 'react-router-dom';
import { useSetBreadcrumbs } from '@/hooks/useSetBreadcrumbs';

export function EnvironmentsPage() {
  const { t } = useTranslation('environments');
  const { projectId } = useParams<{ projectId: string }>();

  useSetBreadcrumbs([
    { label: 'Projects', to: '/projects' },
    { label: '…', to: projectId ? `/projects/${projectId}` : undefined },
    { label: 'Environments' },
  ]);

  return (
    <div>
      <h1>{t('title')}</h1>
      <p>{t('comingSoon')}</p>
    </div>
  );
}
