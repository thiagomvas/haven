import { useTranslation } from 'react-i18next';

import { Grid, Stack } from '@/components/layout';
import { SidecarCard } from '@/components/sidecars/SidecarCard';
import { ErrorAlert } from '@/components/ui/ErrorAlert';
import { Spinner } from '@/components/ui/Spinner';
import { usePermission } from '@/hooks/usePermission';
import { useSetBreadcrumbs } from '@/hooks/useSetBreadcrumbs';
import {
  useDisableSidecar,
  useEnableSidecar,
  useExportSidecarManifest,
  useImportSidecarManifest,
  useSidecars,
} from '@/hooks/useSidecars';
import styles from '@/styles/pages/SidecarsPage.module.css';

export function SidecarsPage() {
  const { t } = useTranslation('sidecars');
  const canManage = usePermission('sidecars.manage');

  useSetBreadcrumbs([{ label: t('title') }]);

  const { data, isLoading, isError } = useSidecars();
  const enableMutation = useEnableSidecar();
  const disableMutation = useDisableSidecar();
  const exportMutation = useExportSidecarManifest();
  const importMutation = useImportSidecarManifest();
  const items = data ?? [];

  const handleToggle = async (sidecarId: string, enabled: boolean) => {
    if (enabled) await enableMutation.mutateAsync(sidecarId);
    else await disableMutation.mutateAsync(sidecarId);
  };

  return (
    <Stack gap="5" className={styles.container}>
      <div className={styles.header}>
        <h1 className={styles.title}>{t('title')}</h1>
        <p className={styles.subtitle}>{t('subtitle')}</p>
      </div>

      {isError && <ErrorAlert message={t('error')} variant="block" />}

      {!isError && isLoading && (
        <div className={styles.spinner}>
          <Spinner />
          <p>{t('loading')}</p>
        </div>
      )}

      {!isError && !isLoading && items.length === 0 && (
        <p className={styles.emptyState}>{t('empty')}</p>
      )}

      {!isError && !isLoading && items.length > 0 && (
        <Grid columns="auto-fill" gap="4">
          {items.map(sidecar => (
            <SidecarCard
              key={sidecar.id}
              sidecar={sidecar}
              canManage={canManage}
              isToggling={
                (enableMutation.isPending && enableMutation.variables === sidecar.id) ||
                (disableMutation.isPending && disableMutation.variables === sidecar.id)
              }
              onToggle={enabled => handleToggle(sidecar.id, enabled)}
              onExportManifest={() => exportMutation.mutateAsync(sidecar.id)}
              isExportingManifest={
                exportMutation.isPending && exportMutation.variables === sidecar.id
              }
              onImportManifest={manifestYaml =>
                importMutation.mutateAsync({ sidecarId: sidecar.id, manifestYaml })
              }
              isImportingManifest={
                importMutation.isPending && importMutation.variables?.sidecarId === sidecar.id
              }
            />
          ))}
        </Grid>
      )}
    </Stack>
  );
}
