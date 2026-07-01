import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';

import { EnvironmentDto } from '@/api/types/environment.types';
import { ProjectDto } from '@/api/types/project.types';
import { ServiceDashboardDto } from '@/api/types/service.types';

import { environmentsApi } from '../../api/environments';
import { projectsApi } from '../../api/projects';
import { CloneServiceInput, servicesApi } from '../../api/services';
import { useForm } from '../../hooks/useForm';
import styles from '@/styles/components/projects/CreateProjectModal.module.css';
import { Button } from '../ui/Button';
import { Form, FormGroup, FormInput, FormLabel } from '../ui/Form';
import { Modal } from '../ui/Modal';
import { SelectInput } from '../ui/SelectInput';

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
  const { t } = useTranslation('services');
  const { t: tCommon } = useTranslation('common');
  const [projects, setProjects] = useState<ProjectDto[]>([]);
  const [environments, setEnvironments] = useState<EnvironmentDto[]>([]);
  const [targetProjectId, setTargetProjectId] = useState(projectId);
  const [targetEnvironmentId, setTargetEnvironmentId] = useState(environmentId);

  const form = useForm({
    initialValues: {
      newName: `${service.name}-clone`,
      newAlias: '',
    },
    onSubmit: async values => {
      const isSameProject = targetProjectId === projectId;
      const isSameEnvironment = targetEnvironmentId === environmentId;
      const input: CloneServiceInput = {
        newName: values.newName.trim(),
        newAlias: values.newAlias.trim() || undefined,
        targetProjectId: !isSameProject ? targetProjectId : undefined,
        targetEnvironmentId: !isSameEnvironment || !isSameProject ? targetEnvironmentId : undefined,
      };
      await servicesApi.clone(projectId, environmentId, service.id, input);
    },
    onSuccess: () => {
      onClose();
      onSuccess?.();
    },
  });

  useEffect(() => {
    if (!isOpen) return;
    projectsApi
      .getAll()
      .then(result => setProjects(result.items))
      .catch(() => {});
  }, [isOpen, service.id]);

  useEffect(() => {
    if (!targetProjectId) return;
    environmentsApi
      .getByProjectId(targetProjectId)
      .then(envs => {
        setEnvironments(envs);
        if (targetProjectId === projectId) {
          setTargetEnvironmentId(environmentId);
        } else {
          setTargetEnvironmentId(envs[0]?.id ?? '');
        }
      })
      .catch(() => {});
  }, [targetProjectId]);

  const handleClose = () => {
    form.reset();
    setTargetProjectId(projectId);
    setTargetEnvironmentId(environmentId);
    onClose();
  };

  const projectOptions = projects.map(p => ({ value: p.id, label: p.name }));
  const environmentOptions = environments.map(e => ({ value: e.id, label: e.name }));

  return (
    <Modal
      isOpen={isOpen}
      onClose={handleClose}
      title={t('clone.title')}
      description={t('clone.description', { name: service.name })}
      size="md"
      error={form.submitError}
      footer={
        <div className={styles.footer}>
          <Button variant="ghost" onClick={handleClose} disabled={form.isLoading}>
            {tCommon('actions.cancel')}
          </Button>
          <Button
            variant="primary"
            onClick={() => form.handleSubmit()}
            isLoading={form.isLoading}
            disabled={!targetEnvironmentId}
          >
            {t('clone.submit')}
          </Button>
        </div>
      }
    >
      <Form onSubmit={form.handleSubmit} isLoading={form.isLoading}>
        <FormGroup>
          <FormLabel htmlFor="clone-service-name" required>
            {t('clone.newName')}
          </FormLabel>
          <FormInput
            id="clone-service-name"
            type="text"
            placeholder={t('clone.newNamePlaceholder')}
            value={form.values.newName}
            fieldName="newName"
            fieldErrors={form.fieldErrors}
            onChange={e => form.updateField('newName', e.target.value)}
            disabled={form.isLoading}
          />
        </FormGroup>

        <FormGroup>
          <FormLabel htmlFor="clone-service-alias" required>
            {t('clone.alias')}
          </FormLabel>
          <FormInput
            id="clone-service-alias"
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

        <FormGroup>
          <FormLabel htmlFor="clone-service-target-project">{t('clone.targetProject')}</FormLabel>
          <SelectInput
            options={projectOptions}
            value={targetProjectId}
            onChange={setTargetProjectId}
            disabled={form.isLoading || projects.length === 0}
          />
        </FormGroup>

        <FormGroup>
          <FormLabel htmlFor="clone-service-target-environment">
            {t('clone.targetEnvironment')}
          </FormLabel>
          <SelectInput
            options={environmentOptions}
            value={targetEnvironmentId}
            onChange={setTargetEnvironmentId}
            disabled={form.isLoading || environments.length === 0}
            placeholder={environments.length === 0 ? t('clone.noEnvironmentsAvailable') : undefined}
          />
        </FormGroup>
      </Form>
    </Modal>
  );
}
