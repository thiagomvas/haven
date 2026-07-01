import { Copy, Trash2 } from 'lucide-react';
import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';

import { UpdateProjectInput } from '@/api/types/project.types';
import { ProjectDto } from '@/api/types/project.types';

import { projectsApi } from '../../api/projects';
import { useForm } from '../../hooks/useForm';
import { Button } from '../ui/Button';
import { DangerZone } from '../ui/DangerZone';
import { SettingsFormContainer, TextArea, TextInput } from '../ui/DetailsPageForm';
import { CloneProjectModal } from './CloneProjectModal';
import styles from '@/styles/components/projects/ProjectSettingsForm.module.css';

interface ProjectSettingsFormProps {
  project: ProjectDto;
  onSuccess?: () => void;
}

export function ProjectSettingsForm({ project, onSuccess }: ProjectSettingsFormProps) {
  const { t } = useTranslation(['projects', 'common']);
  const navigate = useNavigate();
  const [successMessage, setSuccessMessage] = useState(false);
  const [isCloneModalOpen, setIsCloneModalOpen] = useState(false);
  const [isDeleteConfirmOpen, setIsDeleteConfirmOpen] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);

  const form = useForm({
    initialValues: {
      name: project.name || '',
      alias: project.alias || '',
      description: project.description || '',
    },
    onSubmit: async values => {
      const input: UpdateProjectInput = {
        name: values.name.trim() || undefined,
        alias: values.alias.trim() || undefined,
        description: values.description.trim() || undefined,
      };
      await projectsApi.update(project.id, input);
    },
    onSuccess: () => {
      setSuccessMessage(true);
      setTimeout(() => setSuccessMessage(false), 3000);
      onSuccess?.();
    },
  });

  const handleDeleteProject = async () => {
    try {
      setIsDeleting(true);
      await projectsApi.delete(project.id);
      setIsDeleteConfirmOpen(false);
      navigate('/projects');
    } catch (err) {
      console.error('Failed to delete project', err);
    } finally {
      setIsDeleting(false);
    }
  };

  useEffect(() => {
    if (form.values.name !== project.name) {
      form.updateField('name', project.name || '');
    }
    if (form.values.alias !== (project.alias || '')) {
      form.updateField('alias', project.alias || '');
    }
    if (form.values.description !== project.description) {
      form.updateField('description', project.description || '');
    }
  }, [project.name, project.alias, project.description]);

  const isDirty =
    form.values.name !== project.name ||
    form.values.alias !== (project.alias || '') ||
    form.values.description !== project.description;

  return (
    <div className={styles.container}>
      {successMessage && (
        <div className={styles.success}>
          {t('projectUpdated') || 'Project updated successfully'}
        </div>
      )}
      {form.submitError && Object.keys(form.fieldErrors).length === 0 && (
        <div className={styles.error}>{form.submitError}</div>
      )}

      <form onSubmit={form.handleSubmit}>
        <SettingsFormContainer title={t('projectInfo') || 'Project Information'}>
          <TextInput
            id="project-name"
            label={t('projectName') || 'Project Name'}
            placeholder="e.g., my-app, api-service"
            value={form.values.name}
            onChange={e => form.updateField('name', e.target.value)}
            disabled={form.isLoading}
            maxLength={64}
            error={form.fieldErrors.name}
          />
          <TextInput
            id="project-alias"
            label="Alias"
            helperText="Used in Docker names (e.g. haven-myapp-...). 2–8 lowercase letters, digits, or hyphens."
            placeholder="e.g., myapp, backend"
            value={form.values.alias}
            onChange={e => form.updateField('alias', e.target.value.toLowerCase())}
            disabled={form.isLoading}
            maxLength={8}
            error={form.fieldErrors.alias}
          />
          <TextArea
            id="project-description"
            label={t('common:labels.description') || 'Description'}
            placeholder="Describe what this project does..."
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
            {t('save') || 'Save Changes'}
          </Button>
        </div>
      </form>

      <div className={styles.dangerAction} style={{ marginTop: 'var(--space-6)' }}>
        <div className={styles.actionInfo}>
          <h4 className={styles.actionTitle}>{t('clone.action')}</h4>
          <p className={styles.actionDescription}>{t('clone.actionDescription')}</p>
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
            <h4 className={styles.actionTitle}>{t('deleteProject') || 'Delete Project'}</h4>
            <p className={styles.actionDescription}>
              {t('deleteProjectDescription') ||
                'Once you delete a project, there is no going back. Please be certain.'}
            </p>
          </div>
          <Button
            variant="danger"
            icon={<Trash2 size={18} />}
            onClick={() => setIsDeleteConfirmOpen(true)}
            disabled={isDeleting}
          >
            {t('delete') || 'Delete'}
          </Button>
        </div>
      </DangerZone>

      <CloneProjectModal
        isOpen={isCloneModalOpen}
        onClose={() => setIsCloneModalOpen(false)}
        project={project}
      />

      {isDeleteConfirmOpen && (
        <div className={styles.deleteConfirmOverlay}>
          <div className={styles.deleteConfirmDialog}>
            <h2 className={styles.deleteConfirmTitle}>
              {t('deleteProjectTitle') || 'Delete Project?'}
            </h2>
            <p className={styles.deleteConfirmMessage}>
              {t('deleteProjectMessage', { name: project?.name }) ||
                `Are you sure you want to delete "${project?.name}"? This action cannot be undone.`}
            </p>
            <div className={styles.deleteConfirmActions}>
              <Button
                variant="ghost"
                onClick={() => setIsDeleteConfirmOpen(false)}
                disabled={isDeleting}
              >
                {t('cancel') || 'Cancel'}
              </Button>
              <Button variant="danger" onClick={handleDeleteProject} isLoading={isDeleting}>
                {t('deleteProject') || 'Delete Project'}
              </Button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
