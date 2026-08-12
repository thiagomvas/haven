import { Download } from 'lucide-react';
import { FormEvent, useState } from 'react';
import { useTranslation } from 'react-i18next';

import styles from '@/styles/components/projects/CreateProjectModal.module.css';

import { servicesApi } from '../../api/services';
import { Button } from '../ui/Button';
import { Checkbox } from '../ui/Checkbox';
import { CodeBlock } from '../ui/CodeBlock';
import { Form, FormGroup } from '../ui/Form';
import { Modal } from '../ui/Modal';

interface ExportEnvironmentVariablesModalProps {
  isOpen: boolean;
  onClose: () => void;
  serviceId: string;
  serviceName: string;
}

export function ExportEnvironmentVariablesModal({
  isOpen,
  onClose,
  serviceId,
  serviceName,
}: ExportEnvironmentVariablesModalProps) {
  const { t } = useTranslation('services');
  const { t: tCommon } = useTranslation('common');
  const [includeValues, setIncludeValues] = useState(false);
  const [includeFeatureFlags, setIncludeFeatureFlags] = useState(true);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | undefined>(undefined);
  const [exported, setExported] = useState<string | null>(null);

  const handleClose = () => {
    setIncludeValues(false);
    setIncludeFeatureFlags(true);
    setError(undefined);
    setExported(null);
    onClose();
  };

  const handleExport = async (e?: FormEvent<HTMLFormElement>) => {
    e?.preventDefault();
    try {
      setIsLoading(true);
      setError(undefined);
      const result = await servicesApi.exportEnvExample({
        parentId: serviceId,
        parentType: 'Service',
        includeValues,
        includeFeatureFlags,
      });
      setExported(result ?? '');
    } catch (err) {
      setError(err instanceof Error ? err.message : t('exportEnv.error'));
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
    link.download = `${serviceName}.env`;
    link.click();
    URL.revokeObjectURL(url);
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={handleClose}
      title={t('exportEnv.title')}
      description={t('exportEnv.description')}
      size="md"
      error={error}
      footer={
        exported === null ? (
          <div className={styles.footer}>
            <Button variant="ghost" onClick={handleClose} disabled={isLoading}>
              {tCommon('actions.cancel')}
            </Button>
            <Button variant="primary" onClick={handleExport} isLoading={isLoading}>
              {t('exportEnv.export')}
            </Button>
          </div>
        ) : (
          <div className={styles.footer}>
            <Button variant="ghost" onClick={() => setExported(null)}>
              {tCommon('actions.back')}
            </Button>
            <Button variant="primary" icon={<Download size={16} />} onClick={handleDownload}>
              {t('exportEnv.download')}
            </Button>
          </div>
        )
      }
    >
      {exported === null ? (
        <Form onSubmit={handleExport} isLoading={isLoading}>
          <FormGroup>
            <Checkbox
              label={t('exportEnv.includeValues')}
              description={t('exportEnv.includeValuesDescription')}
              checked={includeValues}
              onChange={e => setIncludeValues(e.target.checked)}
              disabled={isLoading}
            />
          </FormGroup>
          <FormGroup>
            <Checkbox
              label={t('exportEnv.includeFeatureFlags')}
              description={t('exportEnv.includeFeatureFlagsDescription')}
              checked={includeFeatureFlags}
              onChange={e => setIncludeFeatureFlags(e.target.checked)}
              disabled={isLoading}
            />
          </FormGroup>
        </Form>
      ) : (
        <CodeBlock code={exported} copyable header={`${serviceName}.env`} />
      )}
    </Modal>
  );
}
