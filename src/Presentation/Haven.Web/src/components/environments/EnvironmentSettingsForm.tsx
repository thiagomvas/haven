import { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { Copy, Trash2 } from 'lucide-react';
import { environmentsApi } from '../../api/environments';
import { EnvironmentDto } from '@/api/types/environment.types';
import { Button } from '../ui/Button';
import { DangerZone } from '../ui/DangerZone';
import { SettingsFormContainer, TextInput, TextArea } from '../ui/DetailsPageForm';
import { useForm } from '../../hooks/useForm';
import { CloneEnvironmentModal } from './CloneEnvironmentModal';
import styles from './EnvironmentSettingsForm.module.css';

interface EnvironmentSettingsFormProps {
  projectId: string;
  environment: EnvironmentDto;
  onSuccess?: () => void;
}

export function EnvironmentSettingsForm({
  projectId,
  environment,
  onSuccess,
}: EnvironmentSettingsFormProps) {
  const { t } = useTranslation(['projects', 'environments', 'common']);
  const navigate = useNavigate();
  const [successMessage, setSuccessMessage] = useState(false);
  const [isCloneModalOpen, setIsCloneModalOpen] = useState(false);
  const [isDeleteConfirmOpen, setIsDeleteConfirmOpen] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);

  const form = useForm({
    initialValues: {
      name: environment.name || '',
      alias: environment.alias || '',
      description: environment.description || '',
    },
    onSubmit: async values => {
      await environmentsApi.update(projectId, environment.id, {
        name: values.name.trim() || undefined,
        alias: values.alias.trim() || undefined,
        description: values.description.trim() || undefined,
      });
    },
    onSuccess: () => {
      setSuccessMessage(true);
      setTimeout(() => setSuccessMessage(false), 3000);
      onSuccess?.();
    },
  });

  const handleDeleteEnvironment = async () => {
    try {
      setIsDeleting(true);
      await environmentsApi.delete(projectId, environment.id);
      setIsDeleteConfirmOpen(false);
      navigate(`/projects/${projectId}`);
    } catch (err) {
      console.error('Failed to delete environment', err);
    } finally {
      setIsDeleting(false);
    }
  };

  // Sync form values when environment data changes (after update)
  // This is needed because useForm.handleSubmit resets values to stale initialValues
  useEffect(() => {
    if (form.values.name !== environment.name) {
      form.updateField('name', environment.name || '');
    }
    if (form.values.alias !== (environment.alias || '')) {
      form.updateField('alias', environment.alias || '');
    }
    if (form.values.description !== environment.description) {
      form.updateField('description', environment.description || '');
    }
  }, [environment.name, environment.alias, environment.description]);

  const isDirty =
    form.values.name !== environment.name ||
    form.values.alias !== (environment.alias || '') ||
    form.values.description !== environment.description;

  return (
    <div className={styles.container}>
      {successMessage && (
        <div className={styles.success}>
          {t('environments:environmentUpdated') || 'Environment updated successfully'}
        </div>
      )}
      {form.submitError && Object.keys(form.fieldErrors).length === 0 && (
        <div className={styles.error}>{form.submitError}</div>
      )}

      <form onSubmit={form.handleSubmit}>
        <SettingsFormContainer
          title={t('environments:environmentInfo') || 'Environment Information'}
        >
          <TextInput
            id="environment-name"
            label={t('environments:name') || 'Name'}
            placeholder="e.g., development, staging, production"
            value={form.values.name}
            onChange={e => form.updateField('name', e.target.value)}
            disabled={form.isLoading}
            maxLength={64}
            error={form.fieldErrors.name}
          />
          <TextInput
            id="environment-alias"
            label="Alias"
            helperText="Used in Docker names (e.g. haven-...-dev). 2–8 lowercase letters, digits, or hyphens."
            placeholder="e.g., dev, prod, stg"
            value={form.values.alias}
            onChange={e => form.updateField('alias', e.target.value.toLowerCase())}
            disabled={form.isLoading}
            maxLength={8}
            error={form.fieldErrors.alias}
          />
          <TextArea
            id="environment-description"
            label={t('common:labels.description') || 'Description'}
            placeholder="Describe this environment..."
            value={form.values.description}
            onChange={e => form.updateField('description', e.target.value)}
            disabled={form.isLoading}
            maxLength={250}
            characterLimit={250}
            error={form.fieldErrors.description}
          />
        </SettingsFormContainer>

        <div className={styles.buttonContainer}>
          <Button
            variant="primary"
            onClick={() => {
              const formEl = document.querySelector('form') as HTMLFormElement;
              formEl?.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
            }}
            disabled={!isDirty || form.isLoading}
            isLoading={form.isLoading}
          >
            {t('projects:save') || 'Save Changes'}
          </Button>
        </div>
      </form>

      <div className={styles.dangerAction} style={{ marginTop: 'var(--space-6)' }}>
        <div className={styles.actionInfo}>
          <h4 className={styles.actionTitle}>{t('environments:clone.action')}</h4>
          <p className={styles.actionDescription}>{t('environments:clone.actionDescription')}</p>
        </div>
        <Button
          variant="secondary"
          icon={<Copy size={18} />}
          onClick={() => setIsCloneModalOpen(true)}
        >
          Clone
        </Button>
      </div>

      <DangerZone>
        <div className={styles.dangerAction}>
          <div className={styles.actionInfo}>
            <h4 className={styles.actionTitle}>
              {t('environments:deleteEnvironment') || 'Delete Environment'}
            </h4>
            <p className={styles.actionDescription}>
              {t('environments:deleteEnvironmentDescription') ||
                'Once you delete an environment, there is no going back. Please be certain.'}
            </p>
          </div>
          <Button
            variant="danger"
            icon={<Trash2 size={18} />}
            onClick={() => setIsDeleteConfirmOpen(true)}
            disabled={isDeleting}
          >
            {t('projects:delete') || 'Delete'}
          </Button>
        </div>
      </DangerZone>

      <CloneEnvironmentModal
        isOpen={isCloneModalOpen}
        onClose={() => setIsCloneModalOpen(false)}
        projectId={projectId}
        environment={environment}
        onSuccess={onSuccess}
      />

      {isDeleteConfirmOpen && (
        <div className={styles.deleteConfirmOverlay}>
          <div className={styles.deleteConfirmDialog}>
            <h2 className={styles.deleteConfirmTitle}>
              {t('environments:deleteEnvironmentTitle') || 'Delete Environment?'}
            </h2>
            <p className={styles.deleteConfirmMessage}>
              {t('environments:deleteEnvironmentMessage', { name: environment?.name }) ||
                `Are you sure you want to delete "${environment?.name}"? This action cannot be undone.`}
            </p>
            <div className={styles.deleteConfirmActions}>
              <Button
                variant="ghost"
                onClick={() => setIsDeleteConfirmOpen(false)}
                disabled={isDeleting}
              >
                {t('projects:cancel') || 'Cancel'}
              </Button>
              <Button variant="danger" onClick={handleDeleteEnvironment} isLoading={isDeleting}>
                {t('environments:deleteEnvironment') || 'Delete Environment'}
              </Button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
