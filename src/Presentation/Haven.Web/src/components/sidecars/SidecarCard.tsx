import { Download, Upload } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import { SidecarDto } from '@/api/types';
import { Row, Stack } from '@/components/layout';
import { Button } from '@/components/ui/Button';
import { HealthIndicator } from '@/components/ui/HealthIndicator';
import { ToggleChip } from '@/components/ui/ToggleChip';
import styles from '@/styles/components/sidecars/SidecarCard.module.css';

import { ExportSidecarManifestModal } from './ExportSidecarManifestModal';
import { ImportSidecarManifestModal } from './ImportSidecarManifestModal';
import { SidecarIcon } from './SidecarIcon';

interface SidecarCardProps {
  sidecar: SidecarDto;
  canManage: boolean;
  onToggle: (enabled: boolean) => Promise<void>;
  isToggling: boolean;
  onExportManifest: () => Promise<string>;
  isExportingManifest: boolean;
  onImportManifest: (manifestYaml: string) => Promise<void>;
  isImportingManifest: boolean;
}

export function SidecarCard({
  sidecar,
  canManage,
  onToggle,
  isToggling,
  onExportManifest,
  isExportingManifest,
  onImportManifest,
  isImportingManifest,
}: SidecarCardProps) {
  const { t } = useTranslation(['sidecars', 'common']);
  const [error, setError] = useState<string | undefined>(undefined);
  const [isImportModalOpen, setIsImportModalOpen] = useState(false);
  const [exportedManifest, setExportedManifest] = useState<string | null>(null);

  const handleToggle = async (enabled: boolean) => {
    setError(undefined);
    try {
      await onToggle(enabled);
    } catch (err) {
      setError(err instanceof Error ? err.message : t('toggleError'));
    }
  };

  const handleExportManifest = async () => {
    setError(undefined);
    try {
      setExportedManifest(await onExportManifest());
    } catch (err) {
      setError(err instanceof Error ? err.message : t('exportError'));
    }
  };

  return (
    <div className={styles.card}>
      <Row gap="4" align="flex-start">
        <div className={styles.iconContainer}>
          <SidecarIcon kind={sidecar.kind} size={28} />
        </div>

        <Stack gap="1" className={styles.info}>
          <Row gap="2" align="center">
            <h3 className={styles.name}>{sidecar.name}</h3>
            <HealthIndicator health={sidecar.status} useTooltip />
          </Row>
          <p className={styles.description}>
            {t(`descriptions.${sidecar.kind}` as const, { defaultValue: t('descriptions.Custom') })}
          </p>
        </Stack>
      </Row>

      <Row justify="space-between" align="center" className={styles.cardFooter}>
        <ToggleChip
          checked={sidecar.enabled}
          onLabel={t('common:labels.enabled')}
          offLabel={t('common:labels.disabled')}
          onChange={canManage ? handleToggle : undefined}
          disabled={isToggling}
        />
        <HealthIndicator health={sidecar.health} showLabel />
      </Row>

      {canManage && (
        <Row gap="2" className={styles.manifestActions}>
          <Button
            variant="ghost"
            size="xs"
            icon={<Download size={14} />}
            isLoading={isExportingManifest}
            disabled={isImportingManifest}
            onClick={handleExportManifest}
            title={t('exportTooltip')}
          >
            {t('export')}
          </Button>
          <Button
            variant="ghost"
            size="xs"
            icon={<Upload size={14} />}
            disabled={isExportingManifest}
            onClick={() => setIsImportModalOpen(true)}
            title={t('importTooltip')}
          >
            {t('import')}
          </Button>
        </Row>
      )}

      {error && <p className={styles.error}>{error}</p>}

      {canManage && (
        <ImportSidecarManifestModal
          sidecar={sidecar}
          isOpen={isImportModalOpen}
          onClose={() => setIsImportModalOpen(false)}
          onImport={onImportManifest}
          isImporting={isImportingManifest}
        />
      )}

      {exportedManifest !== null && (
        <ExportSidecarManifestModal
          sidecarName={sidecar.name}
          isOpen
          onClose={() => setExportedManifest(null)}
          manifestYaml={exportedManifest}
        />
      )}
    </div>
  );
}
