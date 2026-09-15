import { Download } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import styles from '@/styles/components/projects/CreateProjectModal.module.css';

import { environmentsApi } from '../../api/environments';
import { ServiceDto } from '../../api/types';
import { Button } from '../ui/Button';
import { Checkbox } from '../ui/Checkbox';
import { CodeBlock } from '../ui/CodeBlock';
import { Modal } from '../ui/Modal';

interface ExportEnvironmentModalProps {
  onClose: () => void;
  projectId: string;
  environmentId: string;
  environmentName: string;
  services: ServiceDto[];
}

/**
 * Only mounted by the caller while open, so each open gets a fresh default selection
 * (all services checked) derived from the current `services` prop.
 */
export function ExportEnvironmentModal({
  onClose,
  projectId,
  environmentId,
  environmentName,
  services,
}: ExportEnvironmentModalProps) {
  const { t } = useTranslation(['environments', 'common']);
  const [selectedServiceIds, setSelectedServiceIds] = useState<Set<string>>(
    () => new Set(services.map(s => s.id))
  );
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | undefined>(undefined);
  const [exported, setExported] = useState<string | null>(null);

  const handleClose = () => {
    onClose();
  };

  const handleToggleService = (serviceId: string) => {
    setSelectedServiceIds(prev => {
      const next = new Set(prev);
      if (next.has(serviceId)) {
        next.delete(serviceId);
      } else {
        next.add(serviceId);
      }
      return next;
    });
  };

  const allSelected = selectedServiceIds.size === services.length && services.length > 0;
  const handleToggleSelectAll = () => {
    setSelectedServiceIds(allSelected ? new Set() : new Set(services.map(s => s.id)));
  };

  const handleExport = async () => {
    try {
      setIsLoading(true);
      setError(undefined);
      const result = await environmentsApi.exportToDockerCompose(
        projectId,
        environmentId,
        Array.from(selectedServiceIds)
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
    link.download = `${environmentName}.yaml`;
    link.click();
    URL.revokeObjectURL(url);
  };

  return (
    <Modal
      isOpen
      onClose={handleClose}
      title={t('export.title')}
      description={t('export.description')}
      size="md"
      error={error}
      footer={
        exported === null ? (
          <div className={styles.footer}>
            <Button variant="ghost" onClick={handleClose} disabled={isLoading}>
              {t('common:actions.cancel')}
            </Button>
            <Button
              variant="primary"
              onClick={handleExport}
              isLoading={isLoading}
              disabled={selectedServiceIds.size === 0}
            >
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
        <div>
          <div
            style={{
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'space-between',
              marginBottom: 'var(--space-2)',
            }}
          >
            <span>{t('export.servicesToInclude')}</span>
            <Button variant="text" size="sm" onClick={handleToggleSelectAll}>
              {allSelected ? t('export.deselectAll') : t('export.selectAll')}
            </Button>
          </div>
          {services.length === 0 ? (
            <p>{t('noServices')}</p>
          ) : (
            <div style={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-2)' }}>
              {services.map(service => (
                <Checkbox
                  key={service.id}
                  label={service.alias ?? service.name}
                  checked={selectedServiceIds.has(service.id)}
                  onChange={() => handleToggleService(service.id)}
                />
              ))}
            </div>
          )}
        </div>
      ) : (
        <CodeBlock code={exported} copyable header={`${environmentName}.yaml`} />
      )}
    </Modal>
  );
}
