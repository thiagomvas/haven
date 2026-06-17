import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { Copy, Trash2 } from 'lucide-react';
import { servicesApi } from '../../api/services';
import {
  ServiceDashboardDto,
  DockerConfig,
  DockerfileConfig,
  DockerfileSource,
  ExposureMode,
} from '../../api/types';
import { DockerConfigForm } from '../projects/DockerConfigForm';
import { SettingsFormContainer, TextInput, Select } from '../ui/DetailsPageForm';
import { Button } from '../ui/Button';
import { BranchInput } from '../ui/BranchInput';
import { SelectInput } from '../ui/SelectInput';
import { FeaturePanel } from '../ui/FeaturePanel';
import { DangerZone } from '../ui/DangerZone';
import { useBranchAutocomplete } from '../../hooks/useBranchAutocomplete';
import { useGitCredentials } from '../../hooks/useGitCredentials';
import { CloneServiceModal } from './CloneServiceModal';
import styles from './ServiceSettingsForm.module.css';

interface ServiceSettingsFormProps {
  projectId: string;
  environmentId: string;
  serviceId: string;
  service: ServiceDashboardDto;
  onSuccess?: () => void;
}

export function ServiceSettingsForm({
  projectId,
  environmentId,
  serviceId,
  service,
  onSuccess,
}: ServiceSettingsFormProps) {
  const { t } = useTranslation(['projects', 'services', 'common']);
  const navigate = useNavigate();

  const [name, setName] = useState(service.name);
  const [exposureMode, setExposureMode] = useState(service.exposureMode);
  const [isSavingBasic, setIsSavingBasic] = useState(false);
  const [basicError, setBasicError] = useState<string | null>(null);
  const [basicSuccess, setBasicSuccess] = useState(false);

  const [dockerfileForm, setDockerfileForm] = useState<{
    source: DockerfileSource;
    repository: string;
    branch: string;
    filePath: string;
    content: string;
    gitCredentialId?: string;
  }>(() => {
    if (service.type === 'Dockerfile') {
      const cfg = service.sourceConfig as DockerfileConfig | undefined;
      return {
        source: cfg?.source ?? 'Git',
        repository: cfg?.repository ?? '',
        branch: cfg?.branch ?? '',
        filePath: cfg?.filePath ?? '',
        content: cfg?.content ?? '',
        gitCredentialId: cfg?.gitCredentialId,
      };
    }
    return { source: 'Git', repository: '', branch: '', filePath: '', content: '' };
  });
  const [isSavingConfig, setIsSavingConfig] = useState(false);
  const [configError, setConfigError] = useState<string | null>(null);

  const [isCloneModalOpen, setIsCloneModalOpen] = useState(false);
  const [isDeleteConfirmOpen, setIsDeleteConfirmOpen] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);

  const [syncedServiceId, setSyncedServiceId] = useState(service.id);
  if (syncedServiceId !== service.id) {
    setSyncedServiceId(service.id);
    setName(service.name);
    setExposureMode(service.exposureMode);
    if (service.type === 'Dockerfile') {
      const cfg = service.sourceConfig as DockerfileConfig | undefined;
      setDockerfileForm({
        source: cfg?.source ?? 'Git',
        repository: cfg?.repository ?? '',
        branch: cfg?.branch ?? '',
        filePath: cfg?.filePath ?? '',
        content: cfg?.content ?? '',
        gitCredentialId: cfg?.gitCredentialId,
      });
    }
  }

  const isDirtyBasic = name !== service.name || exposureMode !== service.exposureMode;

  const handleSaveBasic = async () => {
    try {
      setIsSavingBasic(true);
      setBasicError(null);
      await servicesApi.update(projectId, environmentId, serviceId, {
        name: name.trim(),
        exposureMode,
      });
      setBasicSuccess(true);
      setTimeout(() => setBasicSuccess(false), 3000);
      onSuccess?.();
    } catch (err) {
      setBasicError(err instanceof Error ? err.message : t('projects:error'));
    } finally {
      setIsSavingBasic(false);
    }
  };

  const handleSaveDockerImage = async (config: DockerConfig) => {
    try {
      setIsSavingConfig(true);
      setConfigError(null);
      await servicesApi.update(projectId, environmentId, serviceId, {
        dockerConfig: config,
      });
      onSuccess?.();
    } catch (err) {
      setConfigError(err instanceof Error ? err.message : t('projects:error'));
    } finally {
      setIsSavingConfig(false);
    }
  };

  const handleSaveDockerfile = async () => {
    const config: DockerfileConfig =
      dockerfileForm.source === 'Git'
        ? {
            source: 'Git',
            repository: dockerfileForm.repository.trim(),
            branch: dockerfileForm.branch.trim(),
            filePath: dockerfileForm.filePath.trim() || undefined,
            gitCredentialId: dockerfileForm.gitCredentialId || undefined,
          }
        : {
            source: 'Raw',
            content: dockerfileForm.content.trim(),
          };
    try {
      setIsSavingConfig(true);
      setConfigError(null);
      await servicesApi.update(projectId, environmentId, serviceId, {
        dockerfileConfig: config,
      });
      onSuccess?.();
    } catch (err) {
      setConfigError(err instanceof Error ? err.message : t('projects:error'));
    } finally {
      setIsSavingConfig(false);
    }
  };

  const handleDeleteService = async () => {
    try {
      setIsDeleting(true);
      // TODO: Implement service deletion when API endpoint exists
      setIsDeleteConfirmOpen(false);
      navigate(`/projects/${projectId}/environments/${environmentId}`);
    } catch (err) {
      console.error('Failed to delete service', err);
    } finally {
      setIsDeleting(false);
    }
  };

  const { data: credentialsPage } = useGitCredentials({ pageNumber: 1, pageSize: 100 });
  const gitCredentials = credentialsPage?.items ?? [];

  const { branches: remoteBranches, isLoading: branchesLoading } = useBranchAutocomplete(
    service.type === 'Dockerfile' && dockerfileForm.source === 'Git'
      ? dockerfileForm.repository
      : '',
    dockerfileForm.gitCredentialId
  );

  const isLoading = isSavingBasic || isSavingConfig || isDeleting;

  return (
    <div className={styles.container}>
      {basicSuccess && <div className={styles.success}>{t('services:serviceUpdated')}</div>}
      {basicError && <div className={styles.error}>{basicError}</div>}

      <SettingsFormContainer title={t('services:serviceSettings')}>
        <TextInput
          label={t('services:name')}
          value={name}
          onChange={e => setName(e.target.value)}
          placeholder={t('services:name')}
          disabled={isLoading}
        />
        <Select
          label={t('services:exposure')}
          value={exposureMode}
          onChange={e => setExposureMode(e.target.value as ExposureMode)}
          disabled={isLoading}
          options={[
            { value: 'None', label: 'None' },
            { value: 'Internal', label: 'Internal' },
            { value: 'External', label: 'External' },
          ]}
        />
      </SettingsFormContainer>

      <div className={styles.buttonContainer}>
        <Button
          variant="primary"
          onClick={handleSaveBasic}
          isLoading={isSavingBasic}
          disabled={!isDirtyBasic || isLoading}
        >
          {t('projects:save')}
        </Button>
      </div>

      <div className={styles.dockerConfigSection}>
        <h3 className={styles.sectionTitle}>{t('services:dockerConfiguration')}</h3>
        {configError && <div className={styles.error}>{configError}</div>}

        {service.type === 'DockerImage' ? (
          <DockerConfigForm
            config={service.sourceConfig as DockerConfig | undefined}
            onSave={handleSaveDockerImage}
            isLoading={isSavingConfig}
          />
        ) : service.type === 'Dockerfile' ? (
          <div className={styles.dockerfileConfigForm}>
            <div className={styles.dockerfileToggle}>
              {(['Git', 'Raw'] as DockerfileSource[]).map(src => (
                <button
                  key={src}
                  type="button"
                  className={`${styles.dockerfileToggleBtn} ${dockerfileForm.source === src ? styles.dockerfileToggleActive : ''}`}
                  onClick={() => setDockerfileForm(f => ({ ...f, source: src }))}
                  disabled={isLoading}
                >
                  {src === 'Git' ? 'Git Repository' : 'Raw Content'}
                </button>
              ))}
            </div>

            {dockerfileForm.source === 'Git' ? (
              <>
                <SelectInput
                  label="Git Credential"
                  value={dockerfileForm.gitCredentialId ?? ''}
                  onChange={v =>
                    setDockerfileForm(f => ({ ...f, gitCredentialId: v || undefined }))
                  }
                  options={gitCredentials.map(c => ({ value: c.id, label: c.displayName }))}
                  placeholder="None (public repository)"
                  disabled={isLoading}
                />
                <TextInput
                  label="Repository URL"
                  value={dockerfileForm.repository}
                  onChange={e => setDockerfileForm(f => ({ ...f, repository: e.target.value }))}
                  placeholder="https://github.com/org/repo"
                  disabled={isLoading}
                />
                <BranchInput
                  label="Branch"
                  value={dockerfileForm.branch}
                  onChange={val => setDockerfileForm(f => ({ ...f, branch: val }))}
                  branches={remoteBranches}
                  isLoadingBranches={branchesLoading}
                  disabled={isLoading}
                />
                <TextInput
                  label="Dockerfile Path (optional)"
                  value={dockerfileForm.filePath}
                  onChange={e => setDockerfileForm(f => ({ ...f, filePath: e.target.value }))}
                  placeholder="e.g., docker/Dockerfile"
                  disabled={isLoading}
                />
              </>
            ) : (
              <div className={styles.dockerfileContentGroup}>
                <label className={styles.dockerfileLabel}>Dockerfile Content</label>
                <textarea
                  className={styles.dockerfileTextarea}
                  value={dockerfileForm.content}
                  onChange={e => setDockerfileForm(f => ({ ...f, content: e.target.value }))}
                  placeholder={
                    'FROM node:20-alpine\nWORKDIR /app\nCOPY . .\nRUN npm install\nCMD ["node", "index.js"]'
                  }
                  disabled={isLoading}
                />
              </div>
            )}

            <div className={styles.buttonContainer}>
              <Button
                variant="primary"
                onClick={handleSaveDockerfile}
                isLoading={isSavingConfig}
                disabled={isLoading}
              >
                {t('projects:save')}
              </Button>
            </div>
          </div>
        ) : (
          <FeaturePanel
            title={t('services:configuration')}
            description={`${service.type} configuration`}
            empty
            emptyMessage={t('services:noConfiguration')}
          />
        )}
      </div>

      <div className={styles.dangerAction} style={{ marginTop: 'var(--space-6)' }}>
        <div className={styles.actionInfo}>
          <h4 className={styles.actionTitle}>Clone Service</h4>
          <p className={styles.actionDescription}>
            Create an exact copy of this service including its configuration, environment variables, and feature flags.
          </p>
        </div>
        <Button
          variant="secondary"
          icon={<Copy size={18} />}
          onClick={() => setIsCloneModalOpen(true)}
          disabled={isLoading}
        >
          Clone
        </Button>
      </div>

      <DangerZone>
        <div className={styles.dangerAction}>
          <div className={styles.actionInfo}>
            <h4 className={styles.actionTitle}>
              {t('services:deleteService') || 'Delete Service'}
            </h4>
            <p className={styles.actionDescription}>
              {t('services:deleteServiceDescription') ||
                'Once you delete a service, there is no going back. Please be certain.'}
            </p>
          </div>
          <Button
            variant="danger"
            icon={<Trash2 size={18} />}
            onClick={() => setIsDeleteConfirmOpen(true)}
            disabled={isDeleting}
          >
            {t('projects:delete')}
          </Button>
        </div>
      </DangerZone>

      <CloneServiceModal
        isOpen={isCloneModalOpen}
        onClose={() => setIsCloneModalOpen(false)}
        projectId={projectId}
        environmentId={environmentId}
        service={service}
        onSuccess={onSuccess}
      />

      {isDeleteConfirmOpen && (
        <div className={styles.deleteConfirmOverlay}>
          <div className={styles.deleteConfirmDialog}>
            <h2 className={styles.deleteConfirmTitle}>{t('services:deleteServiceTitle')}</h2>
            <p className={styles.deleteConfirmMessage}>
              {t('services:deleteServiceMessage', { name: service.name })}
            </p>
            <div className={styles.deleteConfirmActions}>
              <Button
                variant="ghost"
                onClick={() => setIsDeleteConfirmOpen(false)}
                disabled={isDeleting}
              >
                {t('projects:cancel')}
              </Button>
              <Button variant="danger" onClick={handleDeleteService} isLoading={isDeleting}>
                {t('services:delete')}
              </Button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
