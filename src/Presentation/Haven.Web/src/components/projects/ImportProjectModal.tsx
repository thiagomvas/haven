import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import styles from '@/styles/components/projects/CreateProjectModal.module.css';

import { projectsApi } from '../../api/projects';
import { ManifestYamlInput } from '../importExport/ManifestYamlInput';
import { Button } from '../ui/Button';
import { Checkbox } from '../ui/Checkbox';
import { Form, FormGroup, FormInput, FormLabel } from '../ui/Form';
import { Modal } from '../ui/Modal';

interface ImportProjectModalProps {
  onClose: () => void;
  onSuccess: (projectId: string) => void;
}

/** Only mounted by the caller while open, so each open starts from a clean form. */
export function ImportProjectModal({ onClose, onSuccess }: ImportProjectModalProps) {
  const { t } = useTranslation(['projects', 'common']);
  const [yaml, setYaml] = useState('');
  const [name, setName] = useState('');
  const [alias, setAlias] = useState('');
  const [includeEnvironments, setIncludeEnvironments] = useState(true);
  const [includeServices, setIncludeServices] = useState(true);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | undefined>(undefined);

  const handleToggleEnvironments = (checked: boolean) => {
    setIncludeEnvironments(checked);
    if (!checked) setIncludeServices(false);
  };

  const handleImport = async () => {
    try {
      setIsLoading(true);
      setError(undefined);
      const projectId = await projectsApi.import({
        yaml,
        name: name.trim() || undefined,
        alias: alias.trim() || undefined,
        includeEnvironments,
        includeServices: includeEnvironments && includeServices,
      });
      onClose();
      onSuccess(projectId);
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
          <FormLabel htmlFor="import-project-name" optional>
            {t('import.nameOverride')}
          </FormLabel>
          <FormInput
            id="import-project-name"
            type="text"
            placeholder={t('import.nameOverridePlaceholder')}
            value={name}
            onChange={e => setName(e.target.value)}
            disabled={isLoading}
            maxLength={64}
          />
        </FormGroup>

        <FormGroup>
          <FormLabel htmlFor="import-project-alias" optional>
            {t('import.aliasOverride')}
          </FormLabel>
          <FormInput
            id="import-project-alias"
            type="text"
            placeholder={t('import.aliasOverridePlaceholder')}
            value={alias}
            onChange={e => setAlias(e.target.value.toLowerCase())}
            disabled={isLoading}
            maxLength={8}
          />
        </FormGroup>

        <div style={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-2)' }}>
          <Checkbox
            label={t('import.includeEnvironments')}
            description={t('import.includeEnvironmentsDescription')}
            checked={includeEnvironments}
            onChange={e => handleToggleEnvironments(e.target.checked)}
            disabled={isLoading}
          />
          <Checkbox
            label={t('import.includeServices')}
            description={t('import.includeServicesDescription')}
            checked={includeEnvironments && includeServices}
            disabled={!includeEnvironments || isLoading}
            onChange={e => setIncludeServices(e.target.checked)}
          />
        </div>
      </Form>
    </Modal>
  );
}
