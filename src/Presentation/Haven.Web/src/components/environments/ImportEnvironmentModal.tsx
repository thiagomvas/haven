import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import styles from '@/styles/components/projects/CreateProjectModal.module.css';

import { environmentsApi } from '../../api/environments';
import { ManifestYamlInput } from '../importExport/ManifestYamlInput';
import { Button } from '../ui/Button';
import { Checkbox } from '../ui/Checkbox';
import { Form, FormGroup, FormInput, FormLabel } from '../ui/Form';
import { Modal } from '../ui/Modal';

interface ImportEnvironmentModalProps {
  onClose: () => void;
  projectId: string;
  onSuccess?: (environmentId: string) => void;
}

/** Only mounted by the caller while open, so each open starts from a clean form. */
export function ImportEnvironmentModal({
  onClose,
  projectId,
  onSuccess,
}: ImportEnvironmentModalProps) {
  const { t } = useTranslation(['environments', 'common']);
  const [yaml, setYaml] = useState('');
  const [name, setName] = useState('');
  const [alias, setAlias] = useState('');
  const [includeServices, setIncludeServices] = useState(true);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | undefined>(undefined);

  const handleImport = async () => {
    try {
      setIsLoading(true);
      setError(undefined);
      const environmentId = await environmentsApi.import(projectId, {
        yaml,
        name: name.trim() || undefined,
        alias: alias.trim() || undefined,
        includeServices,
      });
      onClose();
      onSuccess?.(environmentId);
    } catch (err) {
      setError(err instanceof Error ? err.message : t('import.error'));
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <Modal
      isOpen
      onClose={onClose}
      title={t('import.title')}
      description={t('import.description')}
      size="md"
      error={error}
      footer={
        <div className={styles.footer}>
          <Button variant="ghost" onClick={onClose} disabled={isLoading}>
            {t('common:actions.cancel')}
          </Button>
          <Button
            variant="primary"
            onClick={handleImport}
            isLoading={isLoading}
            disabled={!yaml.trim()}
          >
            {t('import.submit')}
          </Button>
        </div>
      }
    >
      <Form onSubmit={e => e.preventDefault()} isLoading={isLoading}>
        <ManifestYamlInput value={yaml} onChange={setYaml} disabled={isLoading} />

        <FormGroup>
          <FormLabel htmlFor="import-environment-name" optional>
            {t('import.nameOverride')}
          </FormLabel>
          <FormInput
            id="import-environment-name"
            type="text"
            placeholder={t('import.nameOverridePlaceholder')}
            value={name}
            onChange={e => setName(e.target.value)}
            disabled={isLoading}
          />
        </FormGroup>

        <FormGroup>
          <FormLabel htmlFor="import-environment-alias" optional>
            {t('import.aliasOverride')}
          </FormLabel>
          <FormInput
            id="import-environment-alias"
            type="text"
            placeholder={t('import.aliasOverridePlaceholder')}
            value={alias}
            onChange={e => setAlias(e.target.value.toLowerCase())}
            disabled={isLoading}
            maxLength={8}
          />
        </FormGroup>

        <Checkbox
          label={t('import.includeServices')}
          description={t('import.includeServicesDescription')}
          checked={includeServices}
          onChange={e => setIncludeServices(e.target.checked)}
          disabled={isLoading}
        />
      </Form>
    </Modal>
  );
}
