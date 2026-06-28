import { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { projectsApi, CloneProjectInput } from '../../api/projects';
import { ProjectDto } from "@/api/types/project.types";
import { Modal } from '../ui/Modal';
import { Form, FormGroup, FormLabel, FormInput } from '../ui/Form';
import { Button } from '../ui/Button';
import { useForm } from '../../hooks/useForm';
import styles from './CreateProjectModal.module.css';

interface CloneProjectModalProps {
  isOpen: boolean;
  onClose: () => void;
  project: ProjectDto;
}

export function CloneProjectModal({ isOpen, onClose, project }: CloneProjectModalProps) {
  const { t } = useTranslation('projects');
  const { t: tCommon } = useTranslation('common');
  const navigate = useNavigate();

  const form = useForm({
    initialValues: {
      newName: `${project.name}-clone`,
      newAlias: '',
    },
    onSubmit: async values => {
      const input: CloneProjectInput = {
        newName: values.newName.trim(),
        newAlias: values.newAlias.trim() || undefined,
      };
      await projectsApi.clone(project.id, input);
    },
    onSuccess: () => {
      onClose();
      navigate('/projects');
    },
  });

  useEffect(() => {
    if (isOpen) {
      form.reset();
    }
  }, [isOpen, project.id]);

  const handleClose = () => {
    form.reset();
    onClose();
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={handleClose}
      title={t('clone.title')}
      description={t('clone.description', { name: project.name })}
      size="md"
      error={form.submitError}
      footer={
        <div className={styles.footer}>
          <Button variant="ghost" onClick={handleClose} disabled={form.isLoading}>
            {tCommon('actions.cancel')}
          </Button>
          <Button variant="primary" onClick={() => form.handleSubmit()} isLoading={form.isLoading}>
            {t('clone.submit')}
          </Button>
        </div>
      }
    >
      <Form onSubmit={form.handleSubmit} isLoading={form.isLoading}>
        <FormGroup>
          <FormLabel htmlFor="clone-project-name" required>
            {t('clone.newName')}
          </FormLabel>
          <FormInput
            id="clone-project-name"
            type="text"
            placeholder={t('clone.newNamePlaceholder')}
            value={form.values.newName}
            fieldName="newName"
            fieldErrors={form.fieldErrors}
            onChange={e => form.updateField('newName', e.target.value)}
            disabled={form.isLoading}
            maxLength={64}
          />
        </FormGroup>

        <FormGroup>
          <FormLabel htmlFor="clone-project-alias" required>
            {t('clone.alias')} <span className={styles.hint}>({t('clone.aliasHint')})</span>
          </FormLabel>
          <FormInput
            id="clone-project-alias"
            type="text"
            placeholder={t('clone.aliasPlaceholder')}
            value={form.values.newAlias}
            fieldName="newAlias"
            fieldErrors={form.fieldErrors}
            onChange={e => form.updateField('newAlias', e.target.value.toLowerCase())}
            disabled={form.isLoading}
            maxLength={8}
          />
        </FormGroup>
      </Form>
    </Modal>
  );
}
