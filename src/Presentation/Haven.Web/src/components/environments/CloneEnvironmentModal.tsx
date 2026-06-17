import { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { environmentsApi, CloneEnvironmentInput } from '../../api/environments';
import { EnvironmentDto } from '../../api/types';
import { Modal } from '../ui/Modal';
import { Form, FormGroup, FormLabel, FormInput } from '../ui/Form';
import { Button } from '../ui/Button';
import { useForm } from '../../hooks/useForm';
import styles from '../projects/CreateProjectModal.module.css';

interface CloneEnvironmentModalProps {
  isOpen: boolean;
  onClose: () => void;
  projectId: string;
  environment: EnvironmentDto;
  onSuccess?: () => void;
}

export function CloneEnvironmentModal({
  isOpen,
  onClose,
  projectId,
  environment,
  onSuccess,
}: CloneEnvironmentModalProps) {
  const navigate = useNavigate();

  const form = useForm({
    initialValues: {
      newName: `${environment.name}-clone`,
      newAlias: '',
    },
    onSubmit: async values => {
      const input: CloneEnvironmentInput = {
        newName: values.newName.trim(),
        newAlias: values.newAlias.trim() || undefined,
      };
      await environmentsApi.clone(projectId, environment.id, input);
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
  }, [isOpen, environment.id]);

  const handleClose = () => {
    form.reset();
    onClose();
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={handleClose}
      title="Clone Environment"
      description={`Create an exact copy of "${environment.name}" including all services and environment variables.`}
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
            Clone Environment
          </Button>
        </div>
      }
    >
      <Form onSubmit={form.handleSubmit} isLoading={form.isLoading}>
        <FormGroup>
          <FormLabel htmlFor="clone-env-name" required>
            New Environment Name
          </FormLabel>
          <FormInput
            id="clone-env-name"
            type="text"
            placeholder="e.g., staging-clone"
            value={form.values.newName}
            fieldName="newName"
            fieldErrors={form.fieldErrors}
            onChange={e => form.updateField('newName', e.target.value)}
            disabled={form.isLoading}
            maxLength={64}
          />
        </FormGroup>

        <FormGroup>
          <FormLabel htmlFor="clone-env-alias">
            Alias{' '}
            <span className={styles.hint}>
              (leave blank to inherit, used in Docker network names)
            </span>
          </FormLabel>
          <FormInput
            id="clone-env-alias"
            type="text"
            placeholder="e.g., stg2 (2–8 chars)"
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
