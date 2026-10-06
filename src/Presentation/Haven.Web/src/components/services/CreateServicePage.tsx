import { Check } from 'lucide-react';
import { useEffect, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate, useSearchParams } from 'react-router-dom';

import { EnvironmentDto } from '@/api/types';
import { ProjectDto } from '@/api/types';
import { ExposureMode } from '@/api/types';
import { DockerfileSource } from '@/api/types';
import { CreateServiceInput } from '@/api/types';
import { DockerfileConfig } from '@/api/types';
import { RestartPolicy } from '@/api/types';
import { ServiceTemplateDto } from '@/api/types';
import { ServiceTemplateSummaryDto } from '@/api/types';
import { ServiceType } from '@/api/types';
import { useNetworks } from '@/hooks/useNetworks';
import { useServiceTemplate } from '@/hooks/useServiceTemplates';
import { useSetBreadcrumbs } from '@/hooks/useSetBreadcrumbs';
import { mergeEnvFiles } from '@/lib/envFile';
import styles from '@/styles/components/services/CreateServicePage.module.css';

import { environmentsApi } from '../../api/environments';
import { networksApi } from '../../api/networks';
import { projectsApi } from '../../api/projects';
import { registryDomainsApi } from '../../api/registryDomains';
import { servicesApi } from '../../api/services';
import { serviceTemplatesApi } from '../../api/serviceTemplates';
import { useGitCredentials } from '../../hooks/useGitCredentials';
import { Banner } from '../ui/Banner';
import { Button } from '../ui/Button';
import { Card, CardContent, CardFooter, CardHeader, CardTitle } from '../ui/Card';
import { Checkbox } from '../ui/Checkbox';
import { FormGroup, FormInput, FormLabel, FormTextarea } from '../ui/Form';
import type { ProgressStep } from '../ui/ProgressSteps';
import { ProgressSteps } from '../ui/ProgressSteps';
import { SelectInput } from '../ui/SelectInput';
import { YamlTextEditor } from '../ui/YamlTextEditor';
import { CommandArgsEditor } from './CommandArgsEditor';
import { DockerfileConfigFields } from './DockerfileConfigFields';
import { DockerImageConfigFields } from './DockerImageConfigFields';
import type { DraftDomain } from './DomainsEditor';
import { DomainsEditor } from './DomainsEditor';
import { ExposureModePicker } from './ExposureModePicker';
import type { PortMapping } from './PortMappingsEditor';
import { PortMappingsEditor } from './PortMappingsEditor';
import { ServiceTypePicker } from './ServiceTypePicker';
import { TemplateInputField } from './TemplateInputField';
import { TemplatePickerModal } from './TemplatePickerModal';

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

  // Template fields
  const [selectedTemplateSummary, setSelectedTemplateSummary] =
    useState<ServiceTemplateSummaryDto | null>(null);
  const [templateInputValues, setTemplateInputValues] = useState<Record<string, string>>({});
  const [isTemplateModalOpen, setIsTemplateModalOpen] = useState(false);
  const { data: selectedTemplate, isLoading: isTemplateLoading } = useServiceTemplate(
    selectedTemplateSummary?.id
  );
  const [isImportMode, setIsImportMode] = useState(false);
  const [manifestText, setManifestText] = useState('');
  const sourceMode: 'custom' | 'template' = selectedTemplateSummary ? 'template' : 'custom';

  // DockerImage fields
  const [dockerImage, setDockerImage] = useState('');
  const [portMappings, setPortMappings] = useState<PortMapping[]>([]);
  const [restartPolicy, setRestartPolicy] = useState<RestartPolicy>('UnlessStopped');
  const [commandArgs, setCommandArgs] = useState<string[]>([]);

  // Dockerfile fields
  const [dockerfileSource, setDockerfileSource] = useState<DockerfileSource>('Git');
  const [repository, setRepository] = useState('');
  const [branch, setBranch] = useState('');
  const [filePath, setFilePath] = useState('');
  const [buildContext, setBuildContext] = useState('');
  const [rawContent, setRawContent] = useState('');
  const [gitCredentialId, setGitCredentialId] = useState<string | undefined>(undefined);

  // Environment variables
  const [envVarsText, setEnvVarsText] = useState('');

  // Shared networks
  const [selectedNetworkIds, setSelectedNetworkIds] = useState<string[]>([]);

  // Domains
  const [draftDomains, setDraftDomains] = useState<DraftDomain[]>([]);

  // UI state
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [steps, setSteps] = useState<ProgressStep[]>([]);
  const [status, setStatus] = useState<'idle' | 'creating' | 'success' | 'error'>('idle');
  const [createdServiceId, setCreatedServiceId] = useState<string | null>(null);

  const errorRef = useRef<HTMLDivElement>(null);

  // Bring the error banner into view so the user notices the failure
  useEffect(() => {
    if (error) {
      errorRef.current?.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }
  }, [error]);

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

    if (sourceMode === 'template') {
      if (!selectedTemplate) return false;
      return selectedTemplate.inputs.every(field => {
        if (field.defaultValue) return true;
        return !!templateInputValues[field.key]?.trim();
      });
    }

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

  const handleSelectType = (type: ServiceType) => {
    setSelectedType(type);
    setSelectedTemplateSummary(null);
    setIsImportMode(false);
  };

  const handleSelectTemplate = (template: ServiceTemplateSummaryDto) => {
    setIsImportMode(false);
    setSelectedTemplateSummary(template);
    setTemplateInputValues({});
    if (!name.trim() || name === selectedTemplateSummary?.name) {
      setName(template.name);
    }
  };

  const handleSelectManifest = () => {
    setSelectedTemplateSummary(null);
    setIsImportMode(true);
  };

  const isManifestValid = !!selectedProjectId && !!selectedEnvironmentId && !!manifestText.trim();

  const handleImportManifest = async () => {
    setError(null);
    if (!isManifestValid) {
      setError(t('createPage.fillRequiredFields'));
      return;
    }

    setIsLoading(true);
    setStatus('creating');
    try {
      const serviceId = await servicesApi.importFromManifest(selectedEnvironmentId, manifestText);
      setCreatedServiceId(serviceId);
      try {
        const service = await servicesApi.getById(
          selectedProjectId,
          selectedEnvironmentId,
          serviceId
        );
        setName(service.name);
      } catch (err) {
        console.error('Failed to load imported service', err);
      }
      setStatus('success');
    } catch (err) {
      setError(err instanceof Error ? err.message : t('createPage.failedToCreate'));
      setStatus('error');
    } finally {
      setIsLoading(false);
    }
  };

  const buildPorts = () =>
    portMappings
      .filter(p => p.host.trim() && p.container.trim())
      .map(p =>
        p.ip?.trim()
          ? `${p.ip.trim()}:${p.host.trim()}:${p.container.trim()}`
          : `${p.host.trim()}:${p.container.trim()}`
      );

  const updateStep = (id: string, patch: Partial<ProgressStep>) =>
    setSteps(prev => prev.map(step => (step.id === id ? { ...step, ...patch } : step)));

  /**
   * Runs one tracked step; failures are recorded on the step instead of aborting the flow.
   * Resolves to the error message on failure, or null on success.
   */
  const runStep = async (id: string, action: () => Promise<void>): Promise<string | null> => {
    updateStep(id, { status: 'running', message: t('createPage.steps.running') });
    try {
      await action();
      updateStep(id, { status: 'success', message: t('createPage.steps.succeeded') });
      return null;
    } catch (err) {
      console.error(`Step ${id} failed`, err);
      const reason = err instanceof Error ? err.message : t('createPage.steps.unknownError');
      updateStep(id, {
        status: 'failed',
        message: t('createPage.steps.failed', { error: reason }),
      });
      return reason;
    }
  };

  const pendingStep = (id: string, label: string): ProgressStep => ({
    id,
    label,
    status: 'pending',
    message: t('createPage.steps.pending'),
  });

  const buildStepPlan = (): ProgressStep[] => [
    pendingStep('service', t('createPage.steps.serviceCreated')),
    ...(envVarsText.trim() ? [pendingStep('env', t('createPage.steps.envVarsRegistered'))] : []),
    ...selectedNetworkIds.map(id =>
      pendingStep(
        `network-${id}`,
        t('createPage.steps.networkAssigned', {
          name: sharedNetworks?.find(n => n.id === id)?.name ?? id,
        })
      )
    ),
    ...draftDomains.flatMap(d => [
      pendingStep(`domain-${d.id}`, t('createPage.steps.domainAdded', { hostname: d.hostname })),
      ...(d.certificateId && d.tlsMode === 'Custom'
        ? [
            pendingStep(
              `cert-${d.id}`,
              t('createPage.steps.certificateAttached', { hostname: d.hostname })
            ),
          ]
        : []),
    ]),
  ];

  /**
   * Runs the whole creation flow as a list of visible steps. Only a failure of the first step
   * (creating the service) aborts; later steps fail independently since the service exists.
   */
  const runCreation = async (
    createService: () => Promise<string>,
    registerEnvVars: (serviceId: string) => Promise<void>
  ) => {
    setIsLoading(true);
    setStatus('creating');
    setSteps(buildStepPlan());

    try {
      let serviceId = '';
      const createError = await runStep('service', async () => {
        serviceId = await createService();
      });
      if (createError !== null) {
        setSteps([]);
        setError(createError);
        setStatus('error');
        return;
      }
      setCreatedServiceId(serviceId);

      if (envVarsText.trim()) {
        await runStep('env', () => registerEnvVars(serviceId));
      }

      for (const networkId of selectedNetworkIds) {
        await runStep(`network-${networkId}`, async () => {
          await networksApi.assignService(networkId, serviceId);
        });
      }

      for (const domain of draftDomains) {
        let domainId = '';
        const addError = await runStep(`domain-${domain.id}`, async () => {
          domainId = await registryDomainsApi.add(serviceId, {
            hostname: domain.hostname.trim(),
            containerPort: domain.containerPort,
            tlsMode: domain.tlsMode,
            internalBasePath: domain.internalBasePath?.trim() || undefined,
          });
        });

        if (domain.certificateId && domain.tlsMode === 'Custom') {
          if (addError === null) {
            await runStep(`cert-${domain.id}`, async () => {
              await registryDomainsApi.attachCertificate(serviceId, domainId, {
                certificateId: domain.certificateId!,
              });
            });
          } else {
            updateStep(`cert-${domain.id}`, {
              status: 'skipped',
              message: t('createPage.steps.skippedDomainFailed'),
            });
          }
        }
      }

      setStatus('success');
    } finally {
      setIsLoading(false);
    }
  };

  const handleSubmitFromTemplate = (template: ServiceTemplateDto) =>
    runCreation(
      () =>
        serviceTemplatesApi.createService(selectedProjectId, selectedEnvironmentId, template.id, {
          name: name.trim(),
          alias: alias.trim() || undefined,
          inputValues: templateInputValues,
          exposureMode,
          ports: buildPorts(),
        }),
      async serviceId => {
        // The template already persisted its own resolved variables (and secrets); fetch
        // them and merge in the user's additions so we don't wipe template defaults.
        const existingEnv = await servicesApi.getEnvironmentVariables(
          selectedProjectId,
          selectedEnvironmentId,
          serviceId
        );
        await servicesApi.setEnvironmentVariables(
          selectedProjectId,
          selectedEnvironmentId,
          serviceId,
          mergeEnvFiles(existingEnv, envVarsText)
        );
      }
    );

  const handleSubmit = async () => {
    setError(null);
    setSteps([]);

    if (!isIdentityValid()) {
      setError(t('createPage.fillRequiredFields'));
      return;
    }

    if (!selectedProjectId || !selectedEnvironmentId) {
      setError(t('createPage.projectEnvironmentRequired'));
      return;
    }

    if (sourceMode === 'template') {
      if (!selectedTemplate) return;
      await handleSubmitFromTemplate(selectedTemplate);
      return;
    }

    const ports = buildPorts();
    const filteredCommandArgs = commandArgs.filter(a => a.trim());

    let dockerfileConfig: DockerfileConfig | undefined;
    if (selectedType === 'Dockerfile') {
      if (dockerfileSource === 'Git') {
        dockerfileConfig = {
          source: 'Git',
          repository: repository.trim(),
          branch: branch.trim(),
          filePath: filePath.trim() || undefined,
          buildContext: buildContext.trim() || undefined,
          gitCredentialId: gitCredentialId || undefined,
          ports,
          commandArgs: filteredCommandArgs,
          restartPolicy,
        };
      } else {
        dockerfileConfig = {
          source: 'Raw',
          content: rawContent.trim(),
          ports,
          commandArgs: filteredCommandArgs,
          restartPolicy,
        };
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
              ports,
              commandArgs: filteredCommandArgs,
              restartPolicy,
            }
          : undefined,
      dockerfileConfig,
    };

    await runCreation(
      () => servicesApi.create(selectedProjectId, selectedEnvironmentId, input),
      async serviceId => {
        await servicesApi.setEnvironmentVariables(
          selectedProjectId,
          selectedEnvironmentId,
          serviceId,
          envVarsText
        );
      }
    );
  };

  const handleViewService = () => {
    if (selectedProjectId && selectedEnvironmentId && createdServiceId) {
      navigate(
        `/projects/${selectedProjectId}/environments/${selectedEnvironmentId}/services/${createdServiceId}`
      );
    }
  };

  const hasFailedSteps = steps.some(step => step.status === 'failed');
  const showProgressCard = status === 'success' || (status === 'creating' && steps.length > 0);

  const projectEnvSelects = (
    <div className={styles.twoColumn}>
      <FormGroup>
        <SelectInput
          label={t('createPage.project')}
          required
          value={selectedProjectId}
          onChange={setSelectedProjectId}
          options={projects.map(p => ({ value: p.id, label: p.name }))}
          placeholder={t('createPage.projectPlaceholder')}
          disabled={isLoading || projectsLoading || !!projectIdParam}
        />
      </FormGroup>

      <FormGroup>
        <SelectInput
          label={t('createPage.environmentLabel')}
          required
          value={selectedEnvironmentId}
          onChange={setSelectedEnvironmentId}
          options={environments.map(e => ({ value: e.id, label: e.name }))}
          placeholder={t('createPage.environmentPlaceholder')}
          disabled={
            isLoading || !selectedProjectId || environments.length === 0 || !!environmentIdParam
          }
        />
      </FormGroup>
    </div>
  );

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <h1>{t('createPage.title')}</h1>
        <p>{t('createPage.description')}</p>
      </div>

      <div className={styles.content}>
        {status === 'creating' && <Banner variant="info" title={t('createPage.creating')} />}
        {status === 'success' && !hasFailedSteps && (
          <Banner variant="success" title={t('createPage.createdSuccessfully')} />
        )}
        {status === 'success' && hasFailedSteps && (
          <Banner
            variant="warning"
            title={t('createPage.createdWithErrors')}
            description={t('createPage.createdWithErrorsDescription')}
          />
        )}
        {error && (
          <div ref={errorRef}>
            <Banner variant="error" description={error} />
          </div>
        )}

        {showProgressCard ? (
          <Card className={styles.successCard}>
            <CardHeader>
              <CardTitle>{t('createPage.successTitle')}</CardTitle>
              <p className={styles.cardDescription}>{t('createPage.successDescription')}</p>
            </CardHeader>

            <div className={styles.successContent}>
              {steps.length > 0 ? (
                <ProgressSteps steps={steps} />
              ) : (
                <div className={styles.successIcon}>
                  <Check size={40} />
                </div>
              )}
              {status === 'success' && !hasFailedSteps && (
                <p
                  className={styles.successMessage}
                  dangerouslySetInnerHTML={{
                    __html: t('createPage.successMessage').replace(
                      '{{name}}',
                      `<strong>${name}</strong>`
                    ),
                  }}
                />
              )}
            </div>

            <CardFooter>
              <Button variant="primary" onClick={handleViewService} disabled={status !== 'success'}>
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
                  onChange={handleSelectType}
                  disabled={isLoading}
                  selectedTemplate={selectedTemplateSummary}
                  onPickTemplate={() => setIsTemplateModalOpen(true)}
                  manifestSelected={isImportMode}
                  onPickManifest={handleSelectManifest}
                />
              </CardContent>
            </Card>

            <TemplatePickerModal
              isOpen={isTemplateModalOpen}
              onClose={() => setIsTemplateModalOpen(false)}
              onSelect={handleSelectTemplate}
              selectedTemplateId={selectedTemplateSummary?.id}
            />

            {isImportMode ? (
              <>
                <Card>
                  <CardHeader>
                    <CardTitle>{t('createPage.importFromManifest')}</CardTitle>
                    <p className={styles.cardDescription}>
                      {t('createPage.importManifestDescription')}
                    </p>
                  </CardHeader>

                  <CardContent>
                    <div className={styles.formSection}>
                      {projectEnvSelects}

                      <FormGroup>
                        <FormLabel htmlFor="rawManifest" required>
                          {t('createPage.rawManifest')}
                        </FormLabel>
                        <YamlTextEditor
                          id="rawManifest"
                          placeholder={t('createPage.rawManifestPlaceholder')}
                          value={manifestText}
                          onChange={setManifestText}
                          disabled={isLoading}
                        />
                      </FormGroup>
                    </div>
                  </CardContent>
                </Card>

                <div className={styles.submitSection}>
                  <Button variant="secondary" onClick={() => navigate(-1)} disabled={isLoading}>
                    {t('createPage.cancel')}
                  </Button>
                  <Button
                    variant="primary"
                    onClick={handleImportManifest}
                    isLoading={isLoading}
                    disabled={!isManifestValid}
                  >
                    {t('createPage.importButton')}
                  </Button>
                </div>
              </>
            ) : (
              <>
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
                      {projectEnvSelects}

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

                      {sourceMode === 'template' && (
                        <>
                          {isTemplateLoading && <p>Loading template…</p>}
                          {selectedTemplate && (
                            <div key={selectedTemplate.id}>
                              {selectedTemplate.inputs.map(field => (
                                <TemplateInputField
                                  key={field.key}
                                  field={field}
                                  onChange={value =>
                                    setTemplateInputValues(prev => ({
                                      ...prev,
                                      [field.key]: value,
                                    }))
                                  }
                                  disabled={isLoading}
                                />
                              ))}
                            </div>
                          )}
                          {!selectedTemplateSummary && (
                            <p className={styles.cardDescription}>
                              Choose a template above to configure its settings.
                            </p>
                          )}
                        </>
                      )}

                      {sourceMode === 'custom' && selectedType === 'DockerImage' && (
                        <DockerImageConfigFields
                          dockerImage={dockerImage}
                          onDockerImageChange={setDockerImage}
                          restartPolicy={restartPolicy}
                          onRestartPolicyChange={setRestartPolicy}
                          disabled={isLoading}
                        />
                      )}

                      {sourceMode === 'custom' && selectedType === 'Dockerfile' && (
                        <DockerfileConfigFields
                          source={dockerfileSource}
                          onSourceChange={setDockerfileSource}
                          repository={repository}
                          onRepositoryChange={setRepository}
                          branch={branch}
                          onBranchChange={setBranch}
                          filePath={filePath}
                          onFilePathChange={setFilePath}
                          buildContext={buildContext}
                          onBuildContextChange={setBuildContext}
                          rawContent={rawContent}
                          onRawContentChange={setRawContent}
                          gitCredentialId={gitCredentialId}
                          onGitCredentialIdChange={setGitCredentialId}
                          credentials={credentials}
                          restartPolicy={restartPolicy}
                          onRestartPolicyChange={setRestartPolicy}
                          disabled={isLoading}
                        />
                      )}

                      {sourceMode === 'custom' &&
                        (selectedType === 'DockerImage' || selectedType === 'Dockerfile') && (
                          <CommandArgsEditor
                            commandArgs={commandArgs}
                            onChange={setCommandArgs}
                            disabled={isLoading}
                          />
                        )}
                    </div>
                  </CardContent>
                </Card>

                {/* Card 3: Network & Exposure - for template services and custom DockerImage/Dockerfile */}
                {(sourceMode === 'template' ||
                  selectedType === 'DockerImage' ||
                  selectedType === 'Dockerfile') && (
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
                          exposureMode === 'Custom') && (
                          <PortMappingsEditor
                            portMappings={portMappings}
                            onChange={setPortMappings}
                            disabled={isLoading}
                            showIpField={exposureMode === 'Custom'}
                          />
                        )}

                        <DomainsEditor
                          draftDomains={draftDomains}
                          onDraftDomainsChange={setDraftDomains}
                        />

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
                      {sourceMode === 'template'
                        ? "Additional variables, merged with the template's own variables. A key here overrides the template's value for that key."
                        : t('createPage.serviceVariablesDescription')}
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
          </>
        )}
      </div>
    </div>
  );
}
