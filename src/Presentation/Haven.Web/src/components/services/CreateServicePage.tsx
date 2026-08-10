import { Check } from 'lucide-react';
import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate, useSearchParams } from 'react-router-dom';

import { EnvironmentDto } from '@/api/types';
import { ProjectDto } from '@/api/types';
import { ExposureMode } from '@/api/types';
import { DockerfileSource } from '@/api/types';
import { CreateServiceInput } from '@/api/types';
import { DockerfileConfig } from '@/api/types';
import { RestartPolicy } from '@/api/types';
import { ServiceType } from '@/api/types';
import { useNetworks } from '@/hooks/useNetworks';
import { useSetBreadcrumbs } from '@/hooks/useSetBreadcrumbs';
import styles from '@/styles/components/services/CreateServicePage.module.css';

import { environmentsApi } from '../../api/environments';
import { networksApi } from '../../api/networks';
import { projectsApi } from '../../api/projects';
import { servicesApi } from '../../api/services';
import { useGitCredentials } from '../../hooks/useGitCredentials';
import { Banner } from '../ui/Banner';
import { Button } from '../ui/Button';
import { Card, CardContent, CardFooter, CardHeader, CardTitle } from '../ui/Card';
import { Checkbox } from '../ui/Checkbox';
import { FormGroup, FormInput, FormLabel, FormSelect, FormTextarea } from '../ui/Form';
import { DockerfileConfigFields } from './DockerfileConfigFields';
import { DockerImageConfigFields } from './DockerImageConfigFields';
import { ExposureModePicker } from './ExposureModePicker';
import type { PortMapping } from './PortMappingsEditor';
import { PortMappingsEditor } from './PortMappingsEditor';
import { ServiceTypePicker } from './ServiceTypePicker';

export function CreateServicePage() {
  const { t } = useTranslation('services');
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();

  const projectIdParam = searchParams.get('projectId');
  const environmentIdParam = searchParams.get('environmentId');

  useSetBreadcrumbs([{ label: 'Services', to: '/dashboard' }, { label: 'Create' }]);

  // State for project/environment selection
  const [projects, setProjects] = useState<ProjectDto[]>([]);
  const [environments, setEnvironments] = useState<EnvironmentDto[]>([]);
  const [selectedProjectId, setSelectedProjectId] = useState(projectIdParam || '');
  const [selectedEnvironmentId, setSelectedEnvironmentId] = useState(environmentIdParam || '');
  const [projectsLoading, setProjectsLoading] = useState(true);

  // Form state
  const [selectedType, setSelectedType] = useState<ServiceType>('DockerImage');
  const [name, setName] = useState('');
  const [alias, setAlias] = useState('');
  const [exposureMode, setExposureMode] = useState<ExposureMode>('None');

  // DockerImage fields
  const [dockerImage, setDockerImage] = useState('');
  const [portMappings, setPortMappings] = useState<PortMapping[]>([]);
  const [restartPolicy, setRestartPolicy] = useState<RestartPolicy>('UnlessStopped');

  // Dockerfile fields
  const [dockerfileSource, setDockerfileSource] = useState<DockerfileSource>('Git');
  const [repository, setRepository] = useState('');
  const [branch, setBranch] = useState('');
  const [filePath, setFilePath] = useState('');
  const [rawContent, setRawContent] = useState('');
  const [gitCredentialId, setGitCredentialId] = useState<string | undefined>(undefined);

  // Environment variables
  const [envVarsText, setEnvVarsText] = useState('');

  // Shared networks
  const [selectedNetworkIds, setSelectedNetworkIds] = useState<string[]>([]);

  // UI state
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [networkWarning, setNetworkWarning] = useState<string | null>(null);
  const [status, setStatus] = useState<'idle' | 'creating' | 'success' | 'error'>('idle');
  const [createdServiceId, setCreatedServiceId] = useState<string | null>(null);

  const { data: credentialsPage } = useGitCredentials({ pageNumber: 1, pageSize: 100 });
  const credentials = credentialsPage?.items ?? [];

  const { data: sharedNetworks } = useNetworks({ type: 'Shared' });

  const toggleNetworkSelection = (networkId: string) => {
    setSelectedNetworkIds(prev =>
      prev.includes(networkId) ? prev.filter(id => id !== networkId) : [...prev, networkId]
    );
  };

  // Load projects on mount
  useEffect(() => {
    const loadProjects = async () => {
      try {
        setProjectsLoading(true);
        const result = await projectsApi.getAll({ pageNumber: 1, pageSize: 100 });
        setProjects(result.items);
      } catch (err) {
        console.error('Failed to load projects', err);
      } finally {
        setProjectsLoading(false);
      }
    };

    loadProjects();
  }, []);

  // Load environments when project changes
  useEffect(() => {
    const loadEnvironments = async () => {
      if (!selectedProjectId) {
        setEnvironments([]);
        setSelectedEnvironmentId('');
        return;
      }
      try {
        const envs = await environmentsApi.getByProjectId(selectedProjectId);
        setEnvironments(envs);
        if (!environmentIdParam) {
          setSelectedEnvironmentId('');
        }
      } catch (err) {
        console.error('Failed to load environments', err);
        setEnvironments([]);
      }
    };

    loadEnvironments();
  }, [selectedProjectId]);

  const isIdentityValid = () => {
    if (!name.trim()) return false;
    if (!selectedProjectId || !selectedEnvironmentId) return false;

    if (selectedType === 'DockerImage') {
      return !!dockerImage.trim();
    } else if (selectedType === 'Dockerfile') {
      if (dockerfileSource === 'Git') {
        return !!repository.trim() && !!branch.trim();
      } else {
        return !!rawContent.trim();
      }
    }
    return false;
  };

  const handleSubmit = async () => {
    setError(null);
    setNetworkWarning(null);

    if (!isIdentityValid()) {
      setError(t('createPage.fillRequiredFields'));
      return;
    }

    if (!selectedProjectId || !selectedEnvironmentId) {
      setError(t('createPage.projectEnvironmentRequired'));
      return;
    }

    let dockerfileConfig: DockerfileConfig | undefined;
    if (selectedType === 'Dockerfile') {
      if (dockerfileSource === 'Git') {
        dockerfileConfig = {
          source: 'Git',
          repository: repository.trim(),
          branch: branch.trim(),
          filePath: filePath.trim() || undefined,
          gitCredentialId: gitCredentialId || undefined,
        };
      } else {
        dockerfileConfig = { source: 'Raw', content: rawContent.trim() };
      }
    }

    const input: CreateServiceInput = {
      name: name.trim(),
      alias: alias.trim() || undefined,
      type: selectedType,
      exposureMode,
      dockerConfig:
        selectedType === 'DockerImage'
          ? {
              image: dockerImage.trim(),
              ports: portMappings
                .filter(p => p.host.trim() && p.container.trim())
                .map(p =>
                  p.ip?.trim()
                    ? `${p.ip.trim()}:${p.host.trim()}:${p.container.trim()}`
                    : `${p.host.trim()}:${p.container.trim()}`
                ),
              restartPolicy,
            }
          : undefined,
      dockerfileConfig,
    };

    setIsLoading(true);
    setStatus('creating');
    try {
      const serviceId = await servicesApi.create(selectedProjectId, selectedEnvironmentId, input);
      setCreatedServiceId(serviceId);

      if (envVarsText.trim()) {
        await servicesApi.setEnvironmentVariables(
          selectedProjectId,
          selectedEnvironmentId,
          serviceId,
          envVarsText
        );
      }

      if (selectedNetworkIds.length > 0) {
        const results = await Promise.allSettled(
          selectedNetworkIds.map(networkId => networksApi.assignService(networkId, serviceId))
        );
        const failedCount = results.filter(r => r.status === 'rejected').length;
        if (failedCount > 0) {
          results
            .filter((r): r is PromiseRejectedResult => r.status === 'rejected')
            .forEach(r => console.error('Failed to assign service to network', r.reason));
          setNetworkWarning(
            failedCount === selectedNetworkIds.length
              ? t('createPage.sharedNetworkAssignFailed')
              : t('createPage.sharedNetworkAssignPartialFailed', { count: failedCount })
          );
        }
      }

      setStatus('success');
    } catch (err) {
      setError(err instanceof Error ? err.message : t('createPage.failedToCreate'));
      setStatus('error');
    } finally {
      setIsLoading(false);
    }
  };

  const handleViewService = () => {
    if (selectedProjectId && selectedEnvironmentId && createdServiceId) {
      navigate(
        `/projects/${selectedProjectId}/environments/${selectedEnvironmentId}/services/${createdServiceId}`
      );
    }
  };

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <h1>{t('createPage.title')}</h1>
        <p>{t('createPage.description')}</p>
      </div>

      <div className={styles.content}>
        {status === 'creating' && <Banner variant="info" title={t('createPage.creating')} />}
        {status === 'success' && (
          <Banner variant="success" title={t('createPage.createdSuccessfully')} />
        )}
        {status === 'success' && networkWarning && (
          <Banner variant="warning" description={networkWarning} />
        )}
        {error && <Banner variant="error" description={error} />}

        {status === 'success' ? (
          <Card className={styles.successCard}>
            <CardHeader>
              <CardTitle>{t('createPage.successTitle')}</CardTitle>
              <p className={styles.cardDescription}>{t('createPage.successDescription')}</p>
            </CardHeader>

            <div className={styles.successContent}>
              <div className={styles.successIcon}>
                <Check size={40} />
              </div>
              <p
                className={styles.successMessage}
                dangerouslySetInnerHTML={{
                  __html: t('createPage.successMessage').replace(
                    '{{name}}',
                    `<strong>${name}</strong>`
                  ),
                }}
              />
            </div>

            <CardFooter>
              <Button variant="primary" onClick={handleViewService}>
                {t('createPage.viewService')}
              </Button>
            </CardFooter>
          </Card>
        ) : (
          <>
            {/* Card 1: Deployment Type */}
            <Card>
              <CardHeader>
                <CardTitle>{t('createPage.deploymentType')}</CardTitle>
                <p className={styles.cardDescription}>
                  {t('createPage.deploymentTypeDescription')}
                </p>
              </CardHeader>
              <CardContent>
                <ServiceTypePicker
                  value={selectedType}
                  onChange={setSelectedType}
                  disabled={isLoading}
                />
              </CardContent>
            </Card>

            {/* Card 2: Identity */}
            <Card>
              <CardHeader>
                <CardTitle>{t('createPage.serviceIdentity')}</CardTitle>
                <p className={styles.cardDescription}>
                  {t('createPage.serviceIdentityDescription')}
                </p>
              </CardHeader>

              <CardContent>
                <div className={styles.formSection}>
                  <div className={styles.twoColumn}>
                    <FormGroup>
                      <FormLabel htmlFor="project" required>
                        {t('createPage.project')}
                      </FormLabel>
                      <FormSelect
                        id="project"
                        value={selectedProjectId}
                        onChange={e => setSelectedProjectId(e.target.value)}
                        disabled={isLoading || projectsLoading || !!projectIdParam}
                        style={{ backgroundColor: 'var(--color-surface-2)' }}
                      >
                        <option value="">{t('createPage.projectPlaceholder')}</option>
                        {projects.map(p => (
                          <option key={p.id} value={p.id}>
                            {p.name}
                          </option>
                        ))}
                      </FormSelect>
                    </FormGroup>

                    <FormGroup>
                      <FormLabel htmlFor="environment" required>
                        {t('createPage.environmentLabel')}
                      </FormLabel>
                      <FormSelect
                        id="environment"
                        value={selectedEnvironmentId}
                        onChange={e => setSelectedEnvironmentId(e.target.value)}
                        disabled={
                          isLoading ||
                          !selectedProjectId ||
                          environments.length === 0 ||
                          !!environmentIdParam
                        }
                        style={{ backgroundColor: 'var(--color-surface-2)' }}
                      >
                        <option value="">{t('createPage.environmentPlaceholder')}</option>
                        {environments.map(e => (
                          <option key={e.id} value={e.id}>
                            {e.name}
                          </option>
                        ))}
                      </FormSelect>
                    </FormGroup>
                  </div>

                  <FormGroup>
                    <FormLabel htmlFor="serviceName" required>
                      {t('createPage.serviceName')}
                    </FormLabel>
                    <FormInput
                      id="serviceName"
                      type="text"
                      placeholder={t('createPage.serviceNamePlaceholder')}
                      value={name}
                      onChange={e => setName(e.target.value)}
                      disabled={isLoading}
                      maxLength={64}
                      style={{ backgroundColor: 'var(--color-surface-2)' }}
                    />
                  </FormGroup>

                  <FormGroup>
                    <FormLabel htmlFor="serviceAlias">
                      Alias{' '}
                      <span
                        style={{
                          fontSize: 'var(--text-xs)',
                          color: 'var(--color-text-secondary)',
                          fontWeight: 'normal',
                        }}
                      >
                        — used in Docker names (2–8 chars)
                      </span>
                    </FormLabel>
                    <FormInput
                      id="serviceAlias"
                      type="text"
                      placeholder="e.g., api, web, db"
                      value={alias}
                      onChange={e => setAlias(e.target.value.toLowerCase())}
                      disabled={isLoading}
                      maxLength={8}
                      style={{ backgroundColor: 'var(--color-surface-2)' }}
                    />
                  </FormGroup>

                  {selectedType === 'DockerImage' && (
                    <DockerImageConfigFields
                      dockerImage={dockerImage}
                      onDockerImageChange={setDockerImage}
                      restartPolicy={restartPolicy}
                      onRestartPolicyChange={setRestartPolicy}
                      disabled={isLoading}
                    />
                  )}

                  {selectedType === 'Dockerfile' && (
                    <DockerfileConfigFields
                      source={dockerfileSource}
                      onSourceChange={setDockerfileSource}
                      repository={repository}
                      onRepositoryChange={setRepository}
                      branch={branch}
                      onBranchChange={setBranch}
                      filePath={filePath}
                      onFilePathChange={setFilePath}
                      rawContent={rawContent}
                      onRawContentChange={setRawContent}
                      gitCredentialId={gitCredentialId}
                      onGitCredentialIdChange={setGitCredentialId}
                      credentials={credentials}
                      disabled={isLoading}
                    />
                  )}
                </div>
              </CardContent>
            </Card>

            {/* Card 3: Network & Exposure - Only for DockerImage and Dockerfile */}
            {(selectedType === 'DockerImage' || selectedType === 'Dockerfile') && (
              <Card>
                <CardHeader>
                  <CardTitle>{t('createPage.networkExposure')}</CardTitle>
                  <p className={styles.cardDescription}>
                    {t('createPage.networkExposureDescription')}
                  </p>
                </CardHeader>

                <CardContent>
                  <div className={styles.formSection}>
                    <FormGroup>
                      <FormLabel htmlFor="exposure">{t('createPage.exposureMode')}</FormLabel>
                      <ExposureModePicker
                        value={exposureMode}
                        onChange={setExposureMode}
                        disabled={isLoading}
                      />
                    </FormGroup>

                    {(exposureMode === 'Internal' ||
                      exposureMode === 'External' ||
                      exposureMode === 'Custom') &&
                      selectedType === 'DockerImage' && (
                        <PortMappingsEditor
                          portMappings={portMappings}
                          onChange={setPortMappings}
                          disabled={isLoading}
                          showIpField={exposureMode === 'Custom'}
                        />
                      )}

                    {sharedNetworks && sharedNetworks.length > 0 && (
                      <FormGroup>
                        <div className={styles.labelWithHelp}>
                          <FormLabel>{t('createPage.sharedNetwork')}</FormLabel>
                          <span className={styles.helpText}>
                            {t('createPage.sharedNetworkHelp')}
                          </span>
                        </div>
                        <div className={styles.sharedNetworkList}>
                          {sharedNetworks.map(network => (
                            <Checkbox
                              key={network.id}
                              label={network.name}
                              checked={selectedNetworkIds.includes(network.id)}
                              onChange={() => toggleNetworkSelection(network.id)}
                              disabled={isLoading}
                            />
                          ))}
                        </div>
                      </FormGroup>
                    )}
                  </div>
                </CardContent>
              </Card>
            )}

            {/* Card 4: Environment Variables */}
            <Card>
              <CardHeader>
                <CardTitle>{t('createPage.serviceVariables')}</CardTitle>
                <p className={styles.cardDescription}>
                  {t('createPage.serviceVariablesDescription')}
                </p>
              </CardHeader>

              <CardContent>
                <div className={styles.formSection}>
                  <FormGroup>
                    <div className={styles.labelWithHelp}>
                      <FormLabel htmlFor="serviceVars">{t('createPage.variables')}</FormLabel>
                      <span className={styles.helpText}>{t('createPage.variablesHelp')}</span>
                    </div>
                    <FormTextarea
                      id="serviceVars"
                      placeholder={t('createPage.variablesPlaceholder')}
                      value={envVarsText}
                      onChange={e => setEnvVarsText(e.target.value)}
                      disabled={isLoading}
                      rows={8}
                      style={{ backgroundColor: 'var(--color-surface-2)' }}
                    />
                  </FormGroup>
                </div>
              </CardContent>
            </Card>

            {/* Submit Section */}
            <div className={styles.submitSection}>
              <Button variant="secondary" onClick={() => navigate(-1)} disabled={isLoading}>
                {t('createPage.cancel')}
              </Button>
              <Button
                variant="primary"
                onClick={handleSubmit}
                isLoading={isLoading}
                disabled={!isIdentityValid()}
              >
                {t('createPage.createButton')}
              </Button>
            </div>
          </>
        )}
      </div>
    </div>
  );
}
