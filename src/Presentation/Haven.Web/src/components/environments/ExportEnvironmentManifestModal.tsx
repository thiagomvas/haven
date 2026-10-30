import { Download } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import styles from '@/styles/components/projects/CreateProjectModal.module.css';

import { environmentsApi } from '../../api/environments';
import { EnvironmentDto } from '../../api/types';
import { Button } from '../ui/Button';
import { Checkbox } from '../ui/Checkbox';
import { CodeBlock } from '../ui/CodeBlock';
import { Modal } from '../ui/Modal';

interface ExportEnvironmentManifestModalProps {
  onClose: () => void;
  projectId: string;
  environment: EnvironmentDto;
}

/** Only mounted by the caller while open, so each open starts from the default selection. */
export function ExportEnvironmentManifestModal({
  onClose,
  projectId,
  environment,
}: ExportEnvironmentManifestModalProps) {
  const { t } = useTranslation(['environments', 'common']);
  const [includeServices, setIncludeServices] = useState(true);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | undefined>(undefined);
  const [exported, setExported] = useState<string | null>(null);

  const handleExport = async () => {
    try {
      setIsLoading(true);
      setError(undefined);
      const result = await environmentsApi.exportManifest(
        projectId,
        environment.id,
        includeServices
      );
      setExported(result ?? '');
    } catch (err) {
      setError(err instanceof Error ? err.message : t('exportManifest.error'));
    } finally {
      setIsLoading(false);
    }
  };

  const handleDownload = () => {
    if (exported === null) return;
    const blob = new Blob([exported], { type: 'text/plain' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `${environment.name}.yaml`;
    link.click();
    URL.revokeObjectURL(url);
  };

  return (
    <Modal
      isOpen
      onClose={onClose}
      title={t('exportManifest.title')}
      description={t('exportManifest.description', { name: environment.name })}
      size="md"
      error={error}
      footer={
        exported === null ? (
          <div className={styles.footer}>
            <Button variant="ghost" onClick={onClose} disabled={isLoading}>
              {t('common:actions.cancel')}
            </Button>
            <Button variant="primary" onClick={handleExport} isLoading={isLoading}>
              {t('exportManifest.generate')}
            </Button>
          </div>
        ) : (
          <div className={styles.footer}>
            <Button variant="ghost" onClick={() => setExported(null)}>
              {t('common:actions.back')}
            </Button>
            <Button variant="primary" icon={<Download size={16} />} onClick={handleDownload}>
              {t('common:exportEnv.download')}
            </Button>
          </div>
        )
      }
    >
      {exported === null ? (
        <Checkbox
          label={t('exportManifest.includeServices')}
          description={t('exportManifest.includeServicesDescription')}
          checked={includeServices}
          onChange={e => setIncludeServices(e.target.checked)}
        />
      ) : (
        <CodeBlock code={exported} copyable header={`${environment.name}.yaml`} />
      )}
    </Modal>
  );
}
