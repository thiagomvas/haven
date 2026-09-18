import { Download } from 'lucide-react';
import { FormEvent, useState } from 'react';
import { useTranslation } from 'react-i18next';

import { DockerConfig, DockerfileConfig, ServiceSourceConfig, ServiceType } from '@/api/types';
import styles from '@/styles/components/projects/CreateProjectModal.module.css';

import { servicesApi } from '../../api/services';
import { Banner } from '../ui/Banner';
import { Button } from '../ui/Button';
import { CodeBlock } from '../ui/CodeBlock';
import { Form } from '../ui/Form';
import { Modal } from '../ui/Modal';
import { SelectInput } from '../ui/SelectInput';

type ServiceExportFormat = 'docker-compose';

interface ExportFormatDefinition {
  labelKey: 'export.formats.dockerCompose';
  fileExtension: string;
  fetch: (projectId: string, environmentId: string, serviceId: string) => Promise<string | null>;
}

// Register additional formats here (e.g. ansible, kubernetes) as they gain backend support.
const EXPORT_FORMATS: Record<ServiceExportFormat, ExportFormatDefinition> = {
  'docker-compose': {
    labelKey: 'export.formats.dockerCompose',
    fileExtension: 'yaml',
    fetch: servicesApi.exportToDockerCompose,
  },
};

interface ExportServiceModalProps {
  isOpen: boolean;
  onClose: () => void;
  projectId: string;
  environmentId: string;
  serviceId: string;
  serviceName: string;
  serviceType: ServiceType;
  serviceSourceConfig?: ServiceSourceConfig | DockerConfig;
}

export function ExportServiceModal({
  isOpen,
  onClose,
  projectId,
  environmentId,
  serviceId,
  serviceName,
  serviceType,
  serviceSourceConfig,
}: ExportServiceModalProps) {
  const { t } = useTranslation(['services', 'common']);
  // Raw (non-Git) Dockerfile content has no on-disk file Compose can reference, so it can't be exported.
  const isExportUnsupported =
    serviceType === 'Dockerfile' &&
    (serviceSourceConfig as DockerfileConfig | undefined)?.source === 'Raw';
  const [format, setFormat] = useState<ServiceExportFormat>('docker-compose');
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | undefined>(undefined);
  const [exported, setExported] = useState<string | null>(null);

  const handleClose = () => {
    setFormat('docker-compose');
    setError(undefined);
    setExported(null);
    onClose();
  };

  const handleExport = async (e?: FormEvent<HTMLFormElement>) => {
    e?.preventDefault();
    try {
      setIsLoading(true);
      setError(undefined);
      const result = await EXPORT_FORMATS[format].fetch(projectId, environmentId, serviceId);
      setExported(result ?? '');
    } catch (err) {
      setError(err instanceof Error ? err.message : t('export.error'));
    } finally {
      setIsLoading(false);
    }
  };

  const handleDownload = () => {
    if (exported === null) return;
    const definition = EXPORT_FORMATS[format];
    const blob = new Blob([exported], { type: 'text/plain' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `${serviceName}.${definition.fileExtension}`;
    link.click();
    URL.revokeObjectURL(url);
  };

  const formatOptions = Object.entries(EXPORT_FORMATS).map(([value, definition]) => ({
    value,
    label: t(definition.labelKey),
  }));

  return (
    <Modal
      isOpen={isOpen}
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
              onClick={() => handleExport()}
              isLoading={isLoading}
              disabled={isExportUnsupported}
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
        <Form onSubmit={handleExport} isLoading={isLoading}>
          {serviceType === 'Dockerfile' && (
            <Banner
              variant={isExportUnsupported ? 'error' : 'warning'}
              description={t(
                isExportUnsupported ? 'export.dockerfileUnsupported' : 'export.dockerfileWarning'
              )}
            />
          )}
          <SelectInput
            label={t('export.format')}
            options={formatOptions}
            value={format}
            onChange={value => setFormat(value as ServiceExportFormat)}
            disabled={isLoading}
          />
        </Form>
      ) : (
        <CodeBlock
          code={exported}
          copyable
          header={`${serviceName}.${EXPORT_FORMATS[format].fileExtension}`}
        />
      )}
    </Modal>
  );
}
