import { Download } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import styles from '@/styles/components/projects/CreateProjectModal.module.css';

import { projectsApi } from '../../api/projects';
import { ProjectDto } from '../../api/types';
import { Button } from '../ui/Button';
import { Checkbox } from '../ui/Checkbox';
import { CodeBlock } from '../ui/CodeBlock';
import { Modal } from '../ui/Modal';

interface ExportProjectModalProps {
  onClose: () => void;
  project: ProjectDto;
}

/** Only mounted by the caller while open, so each open starts from the default selection. */
export function ExportProjectModal({ onClose, project }: ExportProjectModalProps) {
  const { t } = useTranslation(['projects', 'common']);
  const [includeEnvironments, setIncludeEnvironments] = useState(true);
  const [includeServices, setIncludeServices] = useState(true);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | undefined>(undefined);
  const [exported, setExported] = useState<string | null>(null);

  const handleToggleEnvironments = (checked: boolean) => {
    setIncludeEnvironments(checked);
    if (!checked) setIncludeServices(false);
  };

  const handleExport = async () => {
    try {
      setIsLoading(true);
      setError(undefined);
      const result = await projectsApi.exportManifest(
        project.id,
        includeEnvironments,
        includeEnvironments && includeServices
      );
      setExported(result ?? '');
    } catch (err) {
      setError(err instanceof Error ? err.message : t('export.error'));
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
    link.download = `${project.name}.yaml`;
    link.click();
    URL.revokeObjectURL(url);
  };

  return (
    <Modal
      isOpen
      onClose={onClose}
      title={t('export.title')}
      description={t('export.description', { name: project.name })}
      size="md"
      error={error}
      footer={
        exported === null ? (
          <div className={styles.footer}>
            <Button variant="ghost" onClick={onClose} disabled={isLoading}>
              {t('common:actions.cancel')}
            </Button>
            <Button variant="primary" onClick={handleExport} isLoading={isLoading}>
              {t('export.generate')}
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
        <div style={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-2)' }}>
          <Checkbox
            label={t('export.includeEnvironments')}
            description={t('export.includeEnvironmentsDescription')}
            checked={includeEnvironments}
            onChange={e => handleToggleEnvironments(e.target.checked)}
          />
          <Checkbox
            label={t('export.includeServices')}
            description={t('export.includeServicesDescription')}
            checked={includeEnvironments && includeServices}
            disabled={!includeEnvironments}
            onChange={e => setIncludeServices(e.target.checked)}
          />
        </div>
      ) : (
        <CodeBlock code={exported} copyable header={`${project.name}.yaml`} />
      )}
    </Modal>
  );
}
