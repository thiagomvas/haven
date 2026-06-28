import { useEffect } from 'react';

import { UpdateEnvironmentInput } from '@/api/types/environment.types';
import { CreateEnvironmentInput } from '@/api/types/environment.types';
import { EnvironmentDto } from '@/api/types/environment.types';

import { environmentsApi } from '../../api/environments';
import { useForm } from '../../hooks/useForm';
import { Button } from '../ui/Button';
import { Form, FormGroup, FormInput, FormLabel, FormTextarea } from '../ui/Form';
import { Modal } from '../ui/Modal';
import styles from './CreateEnvironmentModal.module.css';

interface CreateEnvironmentModalProps {
  projectId: string;
  isOpen: boolean;
  onClose: () => void;
  onSuccess?: () => void;
  environment?: EnvironmentDto;
}

export function CreateEnvironmentModal({
  projectId,
  isOpen,
  onClose,
  onSuccess,
  environment,
}: CreateEnvironmentModalProps) {
  const isEditMode = !!environment;

  const form = useForm({
    initialValues: {
      name: environment?.name || '',
      alias: environment?.alias || '',
      description: environment?.description || '',
    },
    onSubmit: async values => {
      if (isEditMode && environment) {
        const input: UpdateEnvironmentInput = {
          name: values.name.trim() || undefined,
          alias: values.alias.trim() || undefined,
          description: values.description.trim() || undefined,
        };
        await environmentsApi.update(projectId, environment.id, input);
      } else {
        const input: CreateEnvironmentInput = {
          name: values.name.trim(),
          alias: values.alias.trim() || undefined,
          description: values.description.trim() || undefined,
        };
        await environmentsApi.create(projectId, input);
      }
    },
    onSuccess: () => {
      onClose();
      onSuccess?.();
    },
  });

  useEffect(() => {
    if (isOpen) {
      form.reset();
    }
  }, [environment, isOpen]);

  const handleClose = () => {
    form.reset();
    onClose();
  };

  const title = isEditMode ? 'Edit Environment' : 'Create Environment';
  const description = isEditMode
    ? 'Update the deployment environment details'
    : 'Add a new deployment environment for your project';
  const submitLabel = isEditMode ? 'Save Changes' : 'Create Environment';

  return (
    <Modal
      isOpen={isOpen}
      onClose={handleClose}
      title={title}
      description={description}
      size="md"
      error={form.submitError}
      footer={
        <div className={styles.footer}>
          <Button variant="ghost" onClick={handleClose} disabled={form.isLoading}>
            Cancel
          </Button>
          <Button
            variant="primary"
            onClick={() => {
              const formEl = document.querySelector('form') as HTMLFormElement;
              formEl?.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
            }}
            isLoading={form.isLoading}
          >
            {submitLabel}
          </Button>
        </div>
      }
    >
      <Form onSubmit={form.handleSubmit} isLoading={form.isLoading}>
        <FormGroup>
          <FormLabel htmlFor="env-name" required>
            Environment Name
          </FormLabel>
          <FormInput
            id="env-name"
            type="text"
            placeholder="e.g., development, staging, production"
            value={form.values.name}
            fieldName="name"
            fieldErrors={form.fieldErrors}
            onChange={e => form.updateField('name', e.target.value)}
            disabled={form.isLoading}
            maxLength={64}
          />
        </FormGroup>

        <FormGroup>
          <FormLabel htmlFor="env-alias">
            Alias{' '}
            <span className={styles.hint}>
              (used in Docker names, e.g. <code>haven-...-dev-...</code>)
            </span>
          </FormLabel>
          <FormInput
            id="env-alias"
            type="text"
            placeholder="e.g., dev, prod, stg (2–8 chars)"
            value={form.values.alias}
            fieldName="alias"
            fieldErrors={form.fieldErrors}
            onChange={e => form.updateField('alias', e.target.value.toLowerCase())}
            disabled={form.isLoading}
            maxLength={8}
          />
        </FormGroup>

        <FormGroup>
          <FormLabel htmlFor="env-description">Description</FormLabel>
          <FormTextarea
            id="env-description"
            placeholder="Describe the purpose of this environment..."
            value={form.values.description}
            fieldName="description"
            fieldErrors={form.fieldErrors}
            onChange={e => form.updateField('description', e.target.value)}
            disabled={form.isLoading}
            maxLength={250}
          />
          <span className={styles.charCount}>{form.values.description.length}/250</span>
        </FormGroup>
      </Form>
    </Modal>
  );
}
