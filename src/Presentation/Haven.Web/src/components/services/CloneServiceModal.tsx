import { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { servicesApi, CloneServiceInput } from '../../api/services';
import { ServiceDashboardDto } from '../../api/types';
import { Modal } from '../ui/Modal';
import { Form, FormGroup, FormLabel, FormInput } from '../ui/Form';
import { Button } from '../ui/Button';
import { useForm } from '../../hooks/useForm';
import styles from '../projects/CreateProjectModal.module.css';

interface CloneServiceModalProps {
  isOpen: boolean;
  onClose: () => void;
  projectId: string;
  environmentId: string;
  service: ServiceDashboardDto;
  onSuccess?: () => void;
}

export function CloneServiceModal({
  isOpen,
  onClose,
  projectId,
  environmentId,
  service,
  onSuccess,
}: CloneServiceModalProps) {
  const navigate = useNavigate();

  const form = useForm({
    initialValues: {
      newName: `${service.name}-clone`,
      newAlias: '',
    },
    onSubmit: async values => {
      const input: CloneServiceInput = {
        newName: values.newName.trim(),
        newAlias: values.newAlias.trim() || undefined,
      };
      await servicesApi.clone(projectId, environmentId, service.id, input);
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
  }, [isOpen, service.id]);

  const handleClose = () => {
    form.reset();
    onClose();
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={handleClose}
      title="Clone Service"
      description={`Create an exact copy of "${service.name}" including its configuration, environment variables, and feature flags.`}
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
            Clone Service
          </Button>
        </div>
      }
    >
      <Form onSubmit={form.handleSubmit} isLoading={form.isLoading}>
        <FormGroup>
          <FormLabel htmlFor="clone-service-name" required>
            New Service Name
          </FormLabel>
          <FormInput
            id="clone-service-name"
            type="text"
            placeholder="e.g., my-service-clone"
            value={form.values.newName}
            fieldName="newName"
            fieldErrors={form.fieldErrors}
            onChange={e => form.updateField('newName', e.target.value)}
            disabled={form.isLoading}
          />
        </FormGroup>

        <FormGroup>
          <FormLabel htmlFor="clone-service-alias">
            Alias <span className={styles.hint}>(leave blank to inherit from source)</span>
          </FormLabel>
          <FormInput
            id="clone-service-alias"
            type="text"
            placeholder="e.g., mysvc2"
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
