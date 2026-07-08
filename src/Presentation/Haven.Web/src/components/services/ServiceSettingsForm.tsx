import { Copy, Trash2 } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';

import { ExposureMode } from '@/api/types';
import { DockerfileSource } from '@/api/types';
import { DockerfileConfig } from '@/api/types';
import { DockerConfig } from '@/api/types';
import { ServiceDashboardDto } from '@/api/types';
import { RestartPolicy } from '@/api/types';
import { Row, Stack } from '@/components/layout';
import styles from '@/styles/components/services/ServiceSettingsForm.module.css';

import { servicesApi } from '../../api/services';
import { useGitCredentials } from '../../hooks/useGitCredentials';
import { Button } from '../ui/Button';
import { Card, CardContent, CardHeader, CardTitle } from '../ui/Card';
import { DangerZone } from '../ui/DangerZone';
import { FormGroup, FormInput, FormLabel } from '../ui/Form';
import { Label } from '../ui/Label';
import { CloneServiceModal } from './CloneServiceModal';
import { DockerfileConfigFields } from './DockerfileConfigFields';
import { DockerImageConfigFields } from './DockerImageConfigFields';
import { ExposureModePicker } from './ExposureModePicker';
import type { PortMapping } from './PortMappingsEditor';
import { PortMappingsEditor } from './PortMappingsEditor';

interface ServiceSettingsFormProps {
  projectId: string;
  environmentId: string;
  serviceId: string;
  service: ServiceDashboardDto;
  onSuccess?: () => void;
}

function parsePortMappings(ports: string[]): PortMapping[] {
  return ports.map(p => {
    const parts = p.split(':');
    if (parts.length >= 3) {
      const [ip, host, container] = parts;
      return { ip, host: host ?? '', container: container ?? '' };
    }
    const [host, container] = parts;
    return { host: host ?? '', container: container ?? '' };
  });
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
  const [exposureMode, setExposureMode] = useState<ExposureMode>(service.exposureMode);
  const [isSavingBasic, setIsSavingBasic] = useState(false);
  const [basicError, setBasicError] = useState<string | null>(null);
  const [basicSuccess, setBasicSuccess] = useState(false);

  const getDockerImageDefaults = () => {
    const cfg = service.sourceConfig as DockerConfig | undefined;
    return {
      image: cfg?.image ?? '',
      restartPolicy: cfg?.restartPolicy ?? ('UnlessStopped' as RestartPolicy),
      portMappings: parsePortMappings(cfg?.ports ?? []),
    };
  };

  const [dockerImage, setDockerImage] = useState(getDockerImageDefaults().image);
  const [restartPolicy, setRestartPolicy] = useState<RestartPolicy>(
    getDockerImageDefaults().restartPolicy
  );
  const [portMappings, setPortMappings] = useState<PortMapping[]>(
    getDockerImageDefaults().portMappings
  );
  const [isSavingConfig, setIsSavingConfig] = useState(false);
  const [configError, setConfigError] = useState<string | null>(null);

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

  const [isCloneModalOpen, setIsCloneModalOpen] = useState(false);
  const [isDeleteConfirmOpen, setIsDeleteConfirmOpen] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);

  const [syncedServiceId, setSyncedServiceId] = useState(service.id);
  if (syncedServiceId !== service.id) {
    setSyncedServiceId(service.id);
    setName(service.name);
    setExposureMode(service.exposureMode);
    const defaults = getDockerImageDefaults();
    setDockerImage(defaults.image);
    setRestartPolicy(defaults.restartPolicy);
    setPortMappings(defaults.portMappings);
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

  const handleSaveDockerImage = async () => {
    const existingCfg = service.sourceConfig as DockerConfig | undefined;
    const config: DockerConfig = {
      image: dockerImage.trim(),
      ports: portMappings
        .filter(p => p.host.trim() && p.container.trim())
        .map(p =>
          p.ip?.trim()
            ? `${p.ip.trim()}:${p.host.trim()}:${p.container.trim()}`
            : `${p.host.trim()}:${p.container.trim()}`
        ),
      restartPolicy,
    };
    try {
      setIsSavingConfig(true);
      setConfigError(null);
      await servicesApi.update(projectId, environmentId, serviceId, { dockerConfig: config });
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
      await servicesApi.update(projectId, environmentId, serviceId, { dockerfileConfig: config });
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
      setIsDeleteConfirmOpen(false);
      await servicesApi.delete(projectId, environmentId, serviceId);
      navigate(`/projects/${projectId}/environments/${environmentId}`);
    } catch (err) {
      console.error('Failed to delete service', err);
    } finally {
      setIsDeleting(false);
    }
  };

  const { data: credentialsPage } = useGitCredentials({ pageNumber: 1, pageSize: 100 });
  const gitCredentials = credentialsPage?.items ?? [];

  const isLoading = isSavingBasic || isSavingConfig || isDeleting;

  return (
    <Stack gap="6">
      {basicSuccess && (
        <Label variant="success" size="sm">
          {t('services:serviceUpdated')}
        </Label>
      )}

      <Card>
        <CardHeader>
          <CardTitle>{t('services:serviceSettings')}</CardTitle>
        </CardHeader>
        <CardContent>
          <Stack gap="4">
            {basicError && (
              <Label variant="error" size="sm">
                {basicError}
              </Label>
            )}
            <FormGroup>
              <FormLabel htmlFor="serviceName">{t('services:name')}</FormLabel>
              <FormInput
                id="serviceName"
                type="text"
                value={name}
                onChange={e => setName(e.target.value)}
                placeholder={t('services:name')}
                disabled={isLoading}
              />
            </FormGroup>
            <FormGroup>
              <FormLabel>{t('services:exposure')}</FormLabel>
              <ExposureModePicker
                value={exposureMode}
                onChange={setExposureMode}
                disabled={isLoading}
              />
            </FormGroup>
            <Row justify="flex-end">
              <Button
                variant="primary"
                onClick={handleSaveBasic}
                isLoading={isSavingBasic}
                disabled={!isDirtyBasic || isLoading}
              >
                {t('projects:save')}
              </Button>
            </Row>
          </Stack>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>{t('services:dockerConfiguration')}</CardTitle>
        </CardHeader>
        <CardContent>
          <Stack gap="4">
            {configError && (
              <Label variant="error" size="sm">
                {configError}
              </Label>
            )}
            {service.type === 'DockerImage' ? (
              <>
                <DockerImageConfigFields
                  dockerImage={dockerImage}
                  onDockerImageChange={setDockerImage}
                  restartPolicy={restartPolicy}
                  onRestartPolicyChange={setRestartPolicy}
                  disabled={isLoading}
                />
                {exposureMode !== 'None' && (
                  <PortMappingsEditor
                    portMappings={portMappings}
                    onChange={setPortMappings}
                    disabled={isLoading}
                    showIpField={exposureMode === 'Custom'}
                  />
                )}
                <Row justify="flex-end">
                  <Button
                    variant="primary"
                    onClick={handleSaveDockerImage}
                    isLoading={isSavingConfig}
                    disabled={isLoading}
                  >
                    {t('projects:save')}
                  </Button>
                </Row>
              </>
            ) : service.type === 'Dockerfile' ? (
              <>
                <DockerfileConfigFields
                  source={dockerfileForm.source}
                  onSourceChange={src => setDockerfileForm(f => ({ ...f, source: src }))}
                  repository={dockerfileForm.repository}
                  onRepositoryChange={v => setDockerfileForm(f => ({ ...f, repository: v }))}
                  branch={dockerfileForm.branch}
                  onBranchChange={v => setDockerfileForm(f => ({ ...f, branch: v }))}
                  filePath={dockerfileForm.filePath}
                  onFilePathChange={v => setDockerfileForm(f => ({ ...f, filePath: v }))}
                  rawContent={dockerfileForm.content}
                  onRawContentChange={v => setDockerfileForm(f => ({ ...f, content: v }))}
                  gitCredentialId={dockerfileForm.gitCredentialId}
                  onGitCredentialIdChange={v =>
                    setDockerfileForm(f => ({ ...f, gitCredentialId: v }))
                  }
                  credentials={gitCredentials}
                  disabled={isLoading}
                />
                <Row justify="flex-end">
                  <Button
                    variant="primary"
                    onClick={handleSaveDockerfile}
                    isLoading={isSavingConfig}
                    disabled={isLoading}
                  >
                    {t('projects:save')}
                  </Button>
                </Row>
              </>
            ) : null}
          </Stack>
        </CardContent>
      </Card>

      <Row justify="space-between" align="center">
        <Stack gap="1">
          <Label variant="primary" size="lg" weight="bold">
            {t('services:clone.action')}
          </Label>
          <Label variant="secondary" size="sm">
            {t('services:clone.actionDescription')}
          </Label>
        </Stack>
        <Button
          variant="secondary"
          icon={<Copy size={18} />}
          onClick={() => setIsCloneModalOpen(true)}
          disabled={isLoading}
        >
          Clone
        </Button>
      </Row>

      <DangerZone>
        <Row justify="space-between" align="center">
          <Stack gap="1">
            <Label variant="primary" size="lg" weight="bold">
              {t('services:deleteService') || 'Delete Service'}
            </Label>
            <Label variant="secondary" size="sm">
              {t('services:deleteServiceDescription') ||
                'Once you delete a service, there is no going back. Please be certain.'}
            </Label>
          </Stack>
          <Button
            variant="danger"
            icon={<Trash2 size={18} />}
            onClick={() => setIsDeleteConfirmOpen(true)}
            disabled={isDeleting}
          >
            {t('projects:delete')}
          </Button>
        </Row>
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
            <Row gap="3" justify="flex-end">
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
            </Row>
          </div>
        </div>
      )}
    </Stack>
  );
}
