import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import type { TFunction } from 'i18next'
import { Container, FileCode, Layers, Terminal, Check, Lock, Globe, Wifi } from 'lucide-react'
import { useSetBreadcrumbs } from '@/hooks/useSetBreadcrumbs'
import { servicesApi } from '../../api/services'
import { projectsApi } from '../../api/projects'
import { environmentsApi } from '../../api/environments'
import {
  CreateServiceInput,
  DockerfileConfig,
  DockerfileSource,
  ExposureMode,
  RestartPolicy,
  ServiceType,
  ProjectDto,
  EnvironmentDto,
} from '../../api/types'
import { useBranchAutocomplete } from '../../hooks/useBranchAutocomplete'
import { useGitCredentials } from '../../hooks/useGitCredentials'
import { BranchInput } from '../ui/BranchInput'
import { SelectInput } from '../ui/SelectInput'
import { Button } from '../ui/Button'
import styles from './CreateServicePage.module.css'

interface ServiceTypeOption {
  type: ServiceType
  label: string
  description: string
  icon: React.ReactNode
}

const getServiceTypeOptions = (t: TFunction<'services'>): ServiceTypeOption[] => [
  {
    type: 'DockerImage',
    label: t('createPage.dockerImageType'),
    description: t('createPage.dockerImageTypeDescription'),
    icon: <Container size={28} />,
  },
  {
    type: 'Dockerfile',
    label: t('createPage.dockerfileType'),
    description: t('createPage.dockerfileTypeDescription'),
    icon: <FileCode size={28} />,
  },
  {
    type: 'Compose',
    label: t('createPage.composeType'),
    description: t('createPage.composeTypeDescription'),
    icon: <Layers size={28} />,
  },
  {
    type: 'Process',
    label: t('createPage.processType'),
    description: t('createPage.processTypeDescription'),
    icon: <Terminal size={28} />,
  },
]

const getExposureModes = (t: TFunction<'services'>): Array<{ mode: ExposureMode; label: string; description: string; icon: React.ReactNode }> => [
  {
    mode: 'None',
    label: t('createPage.exposureNone'),
    description: t('createPage.exposureNoneDescription'),
    icon: <Lock size={20} />,
  },
  {
    mode: 'Internal',
    label: t('createPage.exposureInternal'),
    description: t('createPage.exposureInternalDescription'),
    icon: <Wifi size={20} />,
  },
  {
    mode: 'External',
    label: t('createPage.exposureExternal'),
    description: t('createPage.exposureExternalDescription'),
    icon: <Globe size={20} />,
  },
]
const RESTART_POLICIES: RestartPolicy[] = ['No', 'Always', 'UnlessStopped', 'OnFailure']

interface PortMapping {
  host: string
  container: string
}

export function CreateServicePage() {
  const { t } = useTranslation('services')
  const navigate = useNavigate()

  useSetBreadcrumbs([
    { label: 'Services', to: '/dashboard' },
    { label: 'Create' },
  ])

  // State for project/environment selection
  const [projects, setProjects] = useState<ProjectDto[]>([])
  const [environments, setEnvironments] = useState<EnvironmentDto[]>([])
  const [selectedProjectId, setSelectedProjectId] = useState('')
  const [selectedEnvironmentId, setSelectedEnvironmentId] = useState('')
  const [projectsLoading, setProjectsLoading] = useState(true)

  // Form state
  const [selectedType, setSelectedType] = useState<ServiceType>('DockerImage')
  const [name, setName] = useState('')
  const [exposureMode, setExposureMode] = useState<ExposureMode>('None')

  // DockerImage fields
  const [dockerImage, setDockerImage] = useState('')
  const [portMappings, setPortMappings] = useState<PortMapping[]>([])
  const [restartPolicy, setRestartPolicy] = useState<RestartPolicy>('UnlessStopped')

  // Dockerfile fields
  const [dockerfileSource, setDockerfileSource] = useState<DockerfileSource>('Git')
  const [repository, setRepository] = useState('')
  const [branch, setBranch] = useState('')
  const [filePath, setFilePath] = useState('')
  const [rawContent, setRawContent] = useState('')
  const [gitCredentialId, setGitCredentialId] = useState<string | undefined>(undefined)

  // Environment variables
  const [envVarsText, setEnvVarsText] = useState('')

  // UI state
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [status, setStatus] = useState<'idle' | 'creating' | 'success' | 'error'>('idle')
  const [createdServiceId, setCreatedServiceId] = useState<string | null>(null)

  const { data: credentialsPage } = useGitCredentials({ pageNumber: 1, pageSize: 100 })
  const credentials = credentialsPage?.items ?? []

  const { branches, isLoading: branchesLoading } = useBranchAutocomplete(
    dockerfileSource === 'Git' ? repository : '',
    gitCredentialId,
  )

  const serviceTypeOptions = getServiceTypeOptions(t)
  const exposureModes = getExposureModes(t)

  // Load projects on mount
  useEffect(() => {
    const loadProjects = async () => {
      try {
        setProjectsLoading(true)
        const result = await projectsApi.getAll({ pageNumber: 1, pageSize: 100 })
        setProjects(result.items)
      } catch (err) {
        console.error('Failed to load projects', err)
      } finally {
        setProjectsLoading(false)
      }
    }

    loadProjects()
  }, [])

  // Load environments when project changes
  useEffect(() => {
    const loadEnvironments = async () => {
      if (!selectedProjectId) {
        setEnvironments([])
        setSelectedEnvironmentId('')
        return
      }
      try {
        const envs = await environmentsApi.getByProjectId(selectedProjectId)
        setEnvironments(envs)
        setSelectedEnvironmentId('')
      } catch (err) {
        console.error('Failed to load environments', err)
        setEnvironments([])
      }
    }

    loadEnvironments()
  }, [selectedProjectId])

  const isIdentityValid = () => {
    if (!name.trim()) return false
    if (!selectedProjectId || !selectedEnvironmentId) return false

    if (selectedType === 'DockerImage') {
      return !!dockerImage.trim()
    } else if (selectedType === 'Dockerfile') {
      if (dockerfileSource === 'Git') {
        return !!repository.trim() && !!branch.trim()
      } else {
        return !!rawContent.trim()
      }
    }
    return false
  }

  const handleSubmit = async () => {
    setError(null)

    if (!isIdentityValid()) {
      setError(t('createPage.fillRequiredFields'))
      return
    }

    if (!selectedProjectId || !selectedEnvironmentId) {
      setError(t('createPage.projectEnvironmentRequired'))
      return
    }

    let dockerfileConfig: DockerfileConfig | undefined
    if (selectedType === 'Dockerfile') {
      if (dockerfileSource === 'Git') {
        dockerfileConfig = {
          source: 'Git',
          repository: repository.trim(),
          branch: branch.trim(),
          filePath: filePath.trim() || undefined,
          gitCredentialId: gitCredentialId || undefined,
        }
      } else {
        dockerfileConfig = { source: 'Raw', content: rawContent.trim() }
      }
    }

    const input: CreateServiceInput = {
      name: name.trim(),
      type: selectedType,
      exposureMode,
      dockerConfig:
        selectedType === 'DockerImage'
          ? {
              image: dockerImage.trim(),
              ports: portMappings
                .filter((p) => p.host.trim() && p.container.trim())
                .map((p) => `${p.host.trim()}:${p.container.trim()}`),
              volumes: [],
              environmentVariables: [],
              restartPolicy,
            }
          : undefined,
      dockerfileConfig,
    }

    setIsLoading(true)
    setStatus('creating')
    try {
      const serviceId = await servicesApi.create(selectedProjectId, selectedEnvironmentId, input)
      setCreatedServiceId(serviceId)

      if (envVarsText.trim()) {
          await servicesApi.setEnvironmentVariables(selectedProjectId, selectedEnvironmentId, serviceId, envVarsText)
      }

      setStatus('success')
    } catch (err) {
      setError(err instanceof Error ? err.message : t('createPage.failedToCreate'))
      setStatus('error')
    } finally {
      setIsLoading(false)
    }
  }

  const handleViewService = () => {
    if (selectedProjectId && selectedEnvironmentId) {
      navigate(`/projects/${selectedProjectId}/environments/${selectedEnvironmentId}/services`)
    }
  }

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <h1>{t('createPage.title')}</h1>
        <p>{t('createPage.description')}</p>
      </div>

      {status !== 'idle' && (
        <div className={`${styles.statusBar} ${styles[`status${status.charAt(0).toUpperCase() + status.slice(1)}`]}`}>
          <div className={styles.statusContent}>
            <span className={styles.statusIndicator}>
              {status === 'creating' && <span className={styles.spinner} />}
              {status === 'success' && <Check size={16} />}
              {status === 'error' && <span>!</span>}
            </span>
            <span className={styles.statusText}>
              {status === 'creating' && t('createPage.creating')}
              {status === 'success' && t('createPage.createdSuccessfully')}
              {status === 'error' && t('createPage.failedToCreate')}
            </span>
          </div>
        </div>
      )}

      <div className={styles.content}>
        {error && <div className={styles.error}>{error}</div>}

        {status === 'success' ? (
          <div className={`${styles.card} ${styles.successCard}`}>
            <div className={styles.cardHeader}>
              <h2 className={styles.cardTitle}>{t('createPage.successTitle')}</h2>
              <p className={styles.cardDescription}>{t('createPage.successDescription')}</p>
            </div>

            <div className={styles.successContent}>
              <div className={styles.successIcon}>
                <Check size={40} />
              </div>
              <p className={styles.successMessage} dangerouslySetInnerHTML={{
                __html: t('createPage.successMessage').replace('{{name}}', `<strong>${name}</strong>`)
              }} />
            </div>

            <div className={styles.cardFooter}>
              <Button variant="primary" onClick={handleViewService}>
                {t('createPage.viewService')}
              </Button>
            </div>
          </div>
        ) : (
          <>
            {/* Card 1: Deployment Type */}
            <div className={styles.card}>
              <div className={styles.cardHeader}>
                <h2 className={styles.cardTitle}>{t('createPage.deploymentType')}</h2>
                <p className={styles.cardDescription}>{t('createPage.deploymentTypeDescription')}</p>
              </div>
              <div className={styles.typeGrid}>
                {serviceTypeOptions.map((opt) => (
                  <button
                    key={opt.type}
                    type="button"
                    className={`${styles.typeCard} ${selectedType === opt.type ? styles.selected : ''}`}
                    onClick={() => setSelectedType(opt.type)}
                    disabled={isLoading}
                  >
                    <div className={styles.typeIcon}>{opt.icon}</div>
                    <span className={styles.typeLabel}>{opt.label}</span>
                    <span className={styles.typeDesc}>{opt.description}</span>
                  </button>
                ))}
              </div>
            </div>

            {/* Card 2: Identity */}
            <div className={styles.card}>
              <div className={styles.cardHeader}>
                <h2 className={styles.cardTitle}>{t('createPage.serviceIdentity')}</h2>
                <p className={styles.cardDescription}>{t('createPage.serviceIdentityDescription')}</p>
              </div>

              <div className={styles.formSection}>
                <div className={styles.twoColumn}>
                  <div className={styles.formGroup}>
                    <label className={styles.label}>
                      {t('createPage.project')} <span className={styles.required}>{t('createPage.required')}</span>
                    </label>
                    <select
                      className={styles.input}
                      value={selectedProjectId}
                      onChange={(e) => setSelectedProjectId(e.target.value)}
                      disabled={isLoading || projectsLoading}
                    >
                      <option value="">{t('createPage.projectPlaceholder')}</option>
                      {projects.map((p) => (
                        <option key={p.id} value={p.id}>
                          {p.name}
                        </option>
                      ))}
                    </select>
                  </div>

                  <div className={styles.formGroup}>
                    <label className={styles.label}>
                      {t('createPage.environmentLabel')} <span className={styles.required}>{t('createPage.required')}</span>
                    </label>
                    <select
                      className={styles.input}
                      value={selectedEnvironmentId}
                      onChange={(e) => setSelectedEnvironmentId(e.target.value)}
                      disabled={isLoading || !selectedProjectId || environments.length === 0}
                    >
                      <option value="">{t('createPage.environmentPlaceholder')}</option>
                      {environments.map((e) => (
                        <option key={e.id} value={e.id}>
                          {e.name}
                        </option>
                      ))}
                    </select>
                  </div>
                </div>

                <div className={styles.formGroup}>
                  <label className={styles.label}>
                    {t('createPage.serviceName')} <span className={styles.required}>{t('createPage.required')}</span>
                  </label>
                  <input
                    type="text"
                    className={styles.input}
                    placeholder={t('createPage.serviceNamePlaceholder')}
                    value={name}
                    onChange={(e) => setName(e.target.value)}
                    disabled={isLoading}
                    maxLength={64}
                  />
                </div>

                {selectedType === 'DockerImage' && (
                  <div className={styles.configFields}>
                    <h3 className={styles.configTitle}>{t('createPage.dockerfileConfiguration')}</h3>
                    <div className={styles.formGroup}>
                      <label className={styles.label}>
                        {t('createPage.dockerImageLabel')} <span className={styles.required}>{t('createPage.required')}</span>
                      </label>
                      <input
                        type="text"
                        className={styles.input}
                        placeholder={t('createPage.dockerImagePlaceholder')}
                        value={dockerImage}
                        onChange={(e) => setDockerImage(e.target.value)}
                        disabled={isLoading}
                      />
                    </div>
                    <div className={styles.formGroup}>
                      <SelectInput
                        label={t('createPage.restartPolicy')}
                        value={restartPolicy}
                        onChange={(v) => setRestartPolicy(v as RestartPolicy)}
                        options={RESTART_POLICIES.map((p) => ({ value: p, label: p }))}
                        disabled={isLoading}
                      />
                    </div>
                  </div>
                )}

                {selectedType === 'Dockerfile' && (
                  <div className={styles.configFields}>
                    <h3 className={styles.configTitle}>{t('createPage.dockerfileConfiguration')}</h3>
                    <div className={styles.formGroup}>
                      <label className={styles.label}>{t('createPage.source')}</label>
                      <div className={styles.sourceToggle}>
                        <button
                          type="button"
                          className={`${styles.toggleButton} ${dockerfileSource === 'Git' ? styles.active : ''}`}
                          onClick={() => setDockerfileSource('Git')}
                          disabled={isLoading}
                        >
                          {t('createPage.gitRepository')}
                        </button>
                        <button
                          type="button"
                          className={`${styles.toggleButton} ${dockerfileSource === 'Raw' ? styles.active : ''}`}
                          onClick={() => setDockerfileSource('Raw')}
                          disabled={isLoading}
                        >
                          {t('createPage.rawContent')}
                        </button>
                      </div>
                    </div>

                    {dockerfileSource === 'Git' ? (
                      <>
                        <div className={styles.formGroup}>
                          <SelectInput
                            label={t('createPage.gitCredential')}
                            value={gitCredentialId ?? ''}
                            onChange={(v) => setGitCredentialId(v || undefined)}
                            options={credentials.map((c) => ({ value: c.id, label: c.displayName }))}
                            placeholder={t('createPage.gitCredentialPlaceholder')}
                            disabled={isLoading}
                          />
                        </div>
                        <div className={styles.formGroup}>
                          <label className={styles.label}>
                            {t('createPage.repositoryUrl')} <span className={styles.required}>{t('createPage.required')}</span>
                          </label>
                          <input
                            type="url"
                            className={styles.input}
                            placeholder={t('createPage.repositoryUrlPlaceholder')}
                            value={repository}
                            onChange={(e) => setRepository(e.target.value)}
                            disabled={isLoading}
                          />
                        </div>
                        <div className={styles.formGroup}>
                          <BranchInput
                            label={`${t('createPage.branch')} ${t('createPage.required')}`}
                            value={branch}
                            onChange={setBranch}
                            branches={branches}
                            isLoadingBranches={branchesLoading}
                            disabled={isLoading}
                          />
                        </div>
                        <div className={styles.formGroup}>
                          <div className={styles.labelWithHelp}>
                            <label className={styles.label}>{t('createPage.dockerfilePath')}</label>
                            <span className={styles.helpText}>{t('createPage.dockerfilePathHelp')}</span>
                          </div>
                          <input
                            type="text"
                            className={styles.input}
                            placeholder={t('createPage.dockerfilePathPlaceholder')}
                            value={filePath}
                            onChange={(e) => setFilePath(e.target.value)}
                            disabled={isLoading}
                          />
                        </div>
                      </>
                    ) : (
                      <div className={styles.formGroup}>
                        <label className={styles.label}>
                          {t('createPage.dockerfileContent')} <span className={styles.required}>{t('createPage.required')}</span>
                        </label>
                        <textarea
                          className={styles.dockerfileTextarea}
                          placeholder={t('createPage.dockerfileContentPlaceholder')}
                          value={rawContent}
                          onChange={(e) => setRawContent(e.target.value)}
                          disabled={isLoading}
                        />
                      </div>
                    )}
                  </div>
                )}
              </div>
            </div>

            {/* Card 3: Network & Exposure - Only for DockerImage and Dockerfile */}
            {(selectedType === 'DockerImage' || selectedType === 'Dockerfile') && (
              <div className={styles.card}>
                <div className={styles.cardHeader}>
                  <h2 className={styles.cardTitle}>{t('createPage.networkExposure')}</h2>
                  <p className={styles.cardDescription}>{t('createPage.networkExposureDescription')}</p>
                </div>

                <div className={styles.formSection}>
                  <div className={styles.formGroup}>
                    <label className={styles.label}>{t('createPage.exposureMode')}</label>
                    <div className={styles.exposureGrid}>
                      {exposureModes.map(({ mode, label, description, icon }) => (
                        <button
                          key={mode}
                          type="button"
                          className={`${styles.exposureCard} ${exposureMode === mode ? styles.selected : ''}`}
                          onClick={() => setExposureMode(mode)}
                          disabled={isLoading}
                        >
                          <div className={styles.exposureIcon}>{icon}</div>
                          <span className={styles.exposureLabel}>{label}</span>
                          <span className={styles.exposureDescription}>{description}</span>
                        </button>
                      ))}
                    </div>
                  </div>

                  {(exposureMode === 'Internal' || exposureMode === 'External') && selectedType === 'DockerImage' && (
                    <div className={styles.formGroup}>
                      <div className={styles.labelWithHelp}>
                        <label className={styles.label}>{t('createPage.portMappings')}</label>
                        <span className={styles.helpText}>{t('createPage.portMappingsHelp')}</span>
                      </div>
                      <div className={styles.portsContainer}>
                        {portMappings.length === 0 ? (
                          <p className={styles.emptyState}>{t('createPage.noPortMappings')}</p>
                        ) : (
                          portMappings.map((port, idx) => (
                            <div key={idx} className={styles.portRow}>
                              <input
                                type="text"
                                className={styles.portInput}
                                placeholder={t('createPage.hostPortPlaceholder')}
                                value={port.host}
                                onChange={(e) => {
                                  const updated = [...portMappings]
                                  updated[idx].host = e.target.value
                                  setPortMappings(updated)
                                }}
                                disabled={isLoading}
                              />
                              <span className={styles.portSeparator}>:</span>
                              <input
                                type="text"
                                className={styles.portInput}
                                placeholder={t('createPage.containerPortPlaceholder')}
                                value={port.container}
                                onChange={(e) => {
                                  const updated = [...portMappings]
                                  updated[idx].container = e.target.value
                                  setPortMappings(updated)
                                }}
                                disabled={isLoading}
                              />
                              <button
                                type="button"
                                className={styles.portRemove}
                                onClick={() => setPortMappings(portMappings.filter((_, i) => i !== idx))}
                                disabled={isLoading}
                              >
                                ×
                              </button>
                            </div>
                          ))
                        )}
                      </div>

                      <button
                        type="button"
                        className={styles.addPortButton}
                        onClick={() => setPortMappings([...portMappings, { host: '', container: '' }])}
                        disabled={isLoading}
                      >
                        {t('createPage.addPort')}
                      </button>
                    </div>
                  )}
                </div>
              </div>
            )}

            {/* Card 4: Environment Variables */}
            <div className={styles.card}>
              <div className={styles.cardHeader}>
                <h2 className={styles.cardTitle}>{t('createPage.serviceVariables')}</h2>
                <p className={styles.cardDescription}>{t('createPage.serviceVariablesDescription')}</p>
              </div>

              <div className={styles.formSection}>
                <div className={styles.formGroup}>
                  <div className={styles.labelWithHelp}>
                    <label className={styles.label}>{t('createPage.variables')}</label>
                    <span className={styles.helpText}>{t('createPage.variablesHelp')}</span>
                  </div>
                  <textarea
                    className={styles.textarea}
                    placeholder={t('createPage.variablesPlaceholder')}
                    value={envVarsText}
                    onChange={(e) => setEnvVarsText(e.target.value)}
                    disabled={isLoading}
                    rows={8}
                  />
                </div>
              </div>
            </div>

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
  )
}
