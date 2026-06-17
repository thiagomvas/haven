import { useEffect, useState } from 'react';
import { environmentsApi, CloneEnvironmentInput } from '../../api/environments';
import { projectsApi } from '../../api/projects';
import { EnvironmentDto, ProjectDto } from '../../api/types';
import { Modal } from '../ui/Modal';
import { Form, FormGroup, FormLabel, FormInput } from '../ui/Form';
import { SelectInput } from '../ui/SelectInput';
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
  const [projects, setProjects] = useState<ProjectDto[]>([]);
  const [targetProjectId, setTargetProjectId] = useState(projectId);

  const form = useForm({
    initialValues: {
      newName: `${environment.name}-clone`,
      newAlias: '',
    },
    onSubmit: async values => {
      const input: CloneEnvironmentInput = {
        newName: values.newName.trim(),
        newAlias: values.newAlias.trim() || undefined,
        targetProjectId: targetProjectId !== projectId ? targetProjectId : undefined,
      };
      await environmentsApi.clone(projectId, environment.id, input);
    },
    onSuccess: () => {
      onClose();
      onSuccess?.();
    },
  });

  useEffect(() => {
    if (!isOpen) return;
    form.reset();
    setTargetProjectId(projectId);
    projectsApi.getAll().then(result => setProjects(result.items)).catch(() => {});
  }, [isOpen, environment.id]);

  const handleClose = () => {
    form.reset();
    onClose();
  };

  const projectOptions = projects.map(p => ({ value: p.id, label: p.name }));

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
            onClick={() => form.handleSubmit()}
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

        <FormGroup>
          <FormLabel htmlFor="clone-env-target-project">Target Project</FormLabel>
          <SelectInput
            options={projectOptions}
            value={targetProjectId}
            onChange={setTargetProjectId}
            disabled={form.isLoading || projects.length === 0}
          />
        </FormGroup>
      </Form>
    </Modal>
  );
}
