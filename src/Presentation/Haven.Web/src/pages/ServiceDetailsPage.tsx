import { useEffect, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Play, Square, RotateCw, Trash2, Copy, Check, RefreshCw } from 'lucide-react'
import { projectsApi } from '../api/projects'
import { environmentsApi } from '../api/environments'
import { servicesApi } from '../api/services'
import { ProjectDto, EnvironmentDto, ServiceDto, DockerConfig, DockerfileConfig, DockerfileSource } from '../api/types'
import { Tabs, TabItem } from '../components/ui/Tabs'
import { FeaturePanel } from '../components/ui/FeaturePanel'
import { DockerConfigForm } from '../components/projects/DockerConfigForm'
import { SettingsFormContainer, TextInput, Select } from '../components/ui/DetailsPageForm'
import { ServiceVariablesEditor } from '../components/services/ServiceVariablesEditor'
import { FeatureFlagsEditor } from '../components/services/FeatureFlagsEditor'
import { Button } from '../components/ui/Button'
import { Spinner } from '../components/ui/Spinner'
import { useBranchAutocomplete } from '../hooks/useBranchAutocomplete'
import { BranchInput } from '../components/ui/BranchInput'
import styles from './ServiceDetailsPage.module.css'

export function ServiceDetailsPage() {
  const { projectId, environmentId, serviceId } = useParams<{
    projectId: string
    environmentId: string
    serviceId: string
  }>()
  const navigate = useNavigate()
  const { t } = useTranslation(['projects', 'services'])

  const [project, setProject] = useState<ProjectDto | null>(null)
  const [environment, setEnvironment] = useState<EnvironmentDto | null>(null)
  const [service, setService] = useState<ServiceDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [actionLoading, setActionLoading] = useState<string | null>(null)
  const [copied, setCopied] = useState(false)
  const [copiedWebhook, setCopiedWebhook] = useState(false)
  const [isDeleteConfirmOpen, setIsDeleteConfirmOpen] = useState(false)
  const [isDeleting, setIsDeleting] = useState(false)
  const [isRegenerateConfirmOpen, setIsRegenerateConfirmOpen] = useState(false)
  const [editForm, setEditForm] = useState({ name: '', exposureMode: '' })
  const [dockerfileForm, setDockerfileForm] = useState<{
    source: DockerfileSource
    repository: string
    branch: string
    filePath: string
    content: string
  }>({ source: 'Git', repository: '', branch: '', filePath: '', content: '' })

  useEffect(() => {
    const loadData = async () => {
      if (!projectId || !environmentId || !serviceId) return

      try {
        setLoading(true)
        setError(null)

        const [projectData, environmentData, serviceData] = await Promise.all([
          projectsApi.getById(projectId),
          environmentsApi.getById(projectId, environmentId),
          servicesApi.getById(projectId, environmentId, serviceId),
        ])

        if (!projectData) {
          setError('Project not found')
          return
        }

        if (!environmentData) {
          setError('Environment not found')
          return
        }

        if (!serviceData) {
          setError('Service not found')
          return
        }

        setProject(projectData)
        setEnvironment(environmentData)
        setService(serviceData)
      } catch (err) {
        setError(err instanceof Error ? err.message : t('error'))
      } finally {
        setLoading(false)
      }
    }

    loadData()
  }, [projectId, environmentId, serviceId, t])

  const handleDeploy = async () => {
    if (!projectId || !environmentId || !serviceId) return
    try {
      setActionLoading('deploy')
      await servicesApi.deploy(projectId, environmentId, serviceId)
      // Refresh service data
      const updatedService = await servicesApi.getById(
        projectId,
        environmentId,
        serviceId,
      )
      setService(updatedService)
    } catch (err) {
      console.error('Failed to deploy service', err)
      setError(err instanceof Error ? err.message : t('error'))
    } finally {
      setActionLoading(null)
    }
  }

  const handleRestart = async () => {
    if (!projectId || !environmentId || !serviceId) return
    try {
      setActionLoading('restart')
      await servicesApi.restart(projectId, environmentId, serviceId)
      // Refresh service data
      const updatedService = await servicesApi.getById(
        projectId,
        environmentId,
        serviceId,
      )
      setService(updatedService)
    } catch (err) {
      console.error('Failed to restart service', err)
      setError(err instanceof Error ? err.message : t('error'))
    } finally {
      setActionLoading(null)
    }
  }

  const handleStop = async () => {
    if (!projectId || !environmentId || !serviceId) return
    try {
      setActionLoading('stop')
      await servicesApi.stop(projectId, environmentId, serviceId)
      // Refresh service data
      const updatedService = await servicesApi.getById(
        projectId,
        environmentId,
        serviceId,
      )
      setService(updatedService)
    } catch (err) {
      console.error('Failed to stop service', err)
      setError(err instanceof Error ? err.message : t('error'))
    } finally {
      setActionLoading(null)
    }
  }

  const handleCopyId = () => {
    if (service?.id) {
      navigator.clipboard.writeText(service.id)
      setCopied(true)
      setTimeout(() => setCopied(false), 2000)
    }
  }

  const getWebhookUrl = () => {
    if (!service?.webhookUrl) return ''
    const origin = window.location.origin
    return `${origin}/${service.webhookUrl.replace(/^\/+/, '')}`
  }

  const handleCopyWebhookUrl = () => {
    const webhookUrl = getWebhookUrl()
    if (webhookUrl) {
      navigator.clipboard.writeText(webhookUrl)
      setCopiedWebhook(true)
      setTimeout(() => setCopiedWebhook(false), 2000)
    }
  }

  const handleRegenerateToken = () => {
    setIsRegenerateConfirmOpen(true)
  }

  const handleRegenerateTokenConfirm = async () => {
    if (!projectId || !environmentId || !serviceId) return
    try {
      setActionLoading('regenerateToken')
      const newToken = await servicesApi.regenerateToken(projectId, environmentId, serviceId)
      // Refresh service data
      const updatedService = await servicesApi.getById(
        projectId,
        environmentId,
        serviceId,
      )
      setService(updatedService)
      setIsRegenerateConfirmOpen(false)
    } catch (err) {
      console.error('Failed to regenerate token', err)
      setError(err instanceof Error ? err.message : t('error'))
    } finally {
      setActionLoading(null)
    }
  }

  const handleDeleteService = async () => {
    // TODO: Implement service deletion when API endpoint exists
    setIsDeleteConfirmOpen(false)
  }

  useEffect(() => {
    if (service) {
      setEditForm({
        name: service.name,
        exposureMode: service.exposureMode,
      })

      if (service.type === 'Dockerfile') {
        const cfg = service.sourceConfig as DockerfileConfig | undefined
        setDockerfileForm({
          source: cfg?.source ?? 'Git',
          repository: cfg?.repository ?? '',
          branch: cfg?.branch ?? '',
          filePath: cfg?.filePath ?? '',
          content: cfg?.content ?? '',
        })
      }
    }
  }, [service?.id])

  const handleSaveEdit = async () => {
    if (!projectId || !environmentId || !serviceId) return
    try {
      setActionLoading('edit')
      await servicesApi.update(projectId, environmentId, serviceId, {
        name: editForm.name,
        exposureMode: editForm.exposureMode,
      })
      const updatedService = await servicesApi.getById(
        projectId,
        environmentId,
        serviceId,
      )
      setService(updatedService)
    } catch (err) {
      console.error('Failed to save service', err)
      setError(err instanceof Error ? err.message : t('error'))
    } finally {
      setActionLoading(null)
    }
  }

  const handleSaveConfiguration = async (config: DockerConfig) => {
    if (!projectId || !environmentId || !serviceId) return
    try {
      setActionLoading('saveConfig')
      await servicesApi.update(projectId, environmentId, serviceId, {
        dockerConfig: config,
      })
      const updatedService = await servicesApi.getById(projectId, environmentId, serviceId)
      setService(updatedService)
    } catch (err) {
      console.error('Failed to save configuration', err)
      setError(err instanceof Error ? err.message : t('error'))
    } finally {
      setActionLoading(null)
    }
  }

  const handleSaveDockerfileConfiguration = async () => {
    if (!projectId || !environmentId || !serviceId) return
    const config: DockerfileConfig =
      dockerfileForm.source === 'Git'
        ? {
            source: 'Git',
            repository: dockerfileForm.repository.trim(),
            branch: dockerfileForm.branch.trim(),
            filePath: dockerfileForm.filePath.trim() || undefined,
          }
        : {
            source: 'Raw',
            content: dockerfileForm.content.trim(),
          }
    try {
      setActionLoading('saveConfig')
      await servicesApi.update(projectId, environmentId, serviceId, {
        dockerfileConfig: config,
      })
      const updatedService = await servicesApi.getById(projectId, environmentId, serviceId)
      setService(updatedService)
    } catch (err) {
      console.error('Failed to save dockerfile configuration', err)
      setError(err instanceof Error ? err.message : t('error'))
    } finally {
      setActionLoading(null)
    }
  }

  const { branches: remoteBranches, isLoading: branchesLoading } = useBranchAutocomplete(
    service?.type === 'Dockerfile' && dockerfileForm.source === 'Git' ? dockerfileForm.repository : '',
  )

  if (loading) {
    return (
      <div className={styles.container}>
        <div className={styles.spinner}>
          <Spinner />
          <p>{t('projects:loading')}</p>
        </div>
      </div>
    )
  }

  if (!project || !environment || !service) {
    return (
      <div className={styles.container}>
        <div className={styles.error}>
          <p>{t('projects:notFound')}</p>
          <button
            onClick={() =>
              navigate(`/projects/${projectId}/environments/${environmentId}`)
            }
          >
            {t('projects:back')}
          </button>
        </div>
      </div>
    )
  }

  const tabs: TabItem[] = [
    {
      id: 'overview',
      label: t('services:overview'),
      content: (
        <div className={styles.overviewTab}>
          <div className={styles.webhookSection}>
            <h3 className={styles.webhookLabel}>Webhook URL</h3>
            <div className={styles.webhookDisplayContainer}>
              <code className={styles.webhookDisplay}>{getWebhookUrl()}</code>
              <button
                className={styles.copyButton}
                onClick={handleCopyWebhookUrl}
                title="Copy webhook URL"
                disabled={actionLoading !== null}
              >
                {copiedWebhook ? <Check size={18} /> : <Copy size={18} />}
              </button>
              <button
                className={styles.copyButton}
                onClick={handleRegenerateToken}
                title="Regenerate token"
                disabled={actionLoading !== null}
              >
                {actionLoading === 'regenerateToken' ? <RefreshCw size={18} className={styles.spinning} /> : <RefreshCw size={18} />}
              </button>
            </div>
          </div>
          <div className={styles.infoGrid}>
            <div className={styles.infoCard}>
              <h3 className={styles.infoLabel}>{t('services:type')}</h3>
              <p className={styles.infoValue}>{service.type}</p>
            </div>
            <div className={styles.infoCard}>
              <h3 className={styles.infoLabel}>{t('services:status')}</h3>
              <p className={styles.infoValue}>{service.status}</p>
            </div>
            <div className={styles.infoCard}>
              <h3 className={styles.infoLabel}>{t('services:exposure')}</h3>
              <p className={styles.infoValue}>{service.exposureMode}</p>
            </div>
            <div className={styles.infoCard}>
              <h3 className={styles.infoLabel}>{t('services:id')}</h3>
              <div className={styles.idContainer}>
                <code className={styles.idValue}>{service.id}</code>
                <button
                  className={styles.copyButton}
                  onClick={handleCopyId}
                  title={t('services:copy')}
                >
                  {copied ? <Check size={16} /> : <Copy size={16} />}
                </button>
              </div>
            </div>
            <div className={styles.infoCard}>
              <h3 className={styles.infoLabel}>{t('services:created')}</h3>
              <p className={styles.infoValue}>
                {new Date(service.createdAt).toLocaleDateString()}
              </p>
            </div>
            <div className={styles.infoCard}>
              <h3 className={styles.infoLabel}>{t('services:updated')}</h3>
              <p className={styles.infoValue}>
                {new Date(service.updatedAt).toLocaleDateString()}
              </p>
            </div>
          </div>
        </div>
      ),
    },
    {
      id: 'configuration',
      label: t('services:configuration'),
      content: (
        <div className={styles.configSection}>
          <SettingsFormContainer title={t('services:serviceSettings')}>
            <TextInput
              label={t('services:name')}
              value={editForm.name}
              onChange={(e) =>
                setEditForm({ ...editForm, name: e.target.value })
              }
              placeholder={t('services:name')}
              disabled={actionLoading !== null}
            />
            <Select
              label={t('services:exposure')}
              value={editForm.exposureMode}
              onChange={(e) =>
                setEditForm({ ...editForm, exposureMode: e.target.value })
              }
              disabled={actionLoading !== null}
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
              onClick={handleSaveEdit}
              isLoading={actionLoading === 'edit'}
              disabled={actionLoading !== null}
            >
              {t('projects:save')}
            </Button>
          </div>

          <div className={styles.dockerConfigSection}>
            <h3 className={styles.sectionTitle}>{t('services:dockerConfiguration')}</h3>
            {service.type === 'DockerImage' ? (
              <DockerConfigForm
                config={service.sourceConfig as DockerConfig | undefined}
                onSave={handleSaveConfiguration}
                isLoading={actionLoading === 'saveConfig'}
              />
            ) : service.type === 'Dockerfile' ? (
              <div className={styles.dockerfileConfigForm}>
                <div className={styles.dockerfileToggle}>
                  {(['Git', 'Raw'] as DockerfileSource[]).map((src) => (
                    <button
                      key={src}
                      type="button"
                      className={`${styles.dockerfileToggleBtn} ${dockerfileForm.source === src ? styles.dockerfileToggleActive : ''}`}
                      onClick={() => setDockerfileForm((f) => ({ ...f, source: src }))}
                      disabled={actionLoading !== null}
                    >
                      {src === 'Git' ? 'Git Repository' : 'Raw Content'}
                    </button>
                  ))}
                </div>

                {dockerfileForm.source === 'Git' ? (
                  <>
                    <TextInput
                      label="Repository URL"
                      value={dockerfileForm.repository}
                      onChange={(e) => setDockerfileForm((f) => ({ ...f, repository: e.target.value }))}
                      placeholder="https://github.com/org/repo"
                      disabled={actionLoading !== null}
                    />
                    <BranchInput
                      label="Branch"
                      value={dockerfileForm.branch}
                      onChange={(val) => setDockerfileForm((f) => ({ ...f, branch: val }))}
                      branches={remoteBranches}
                      isLoadingBranches={branchesLoading}
                      disabled={actionLoading !== null}
                    />
                    <TextInput
                      label="Dockerfile Path (optional)"
                      value={dockerfileForm.filePath}
                      onChange={(e) => setDockerfileForm((f) => ({ ...f, filePath: e.target.value }))}
                      placeholder="e.g., docker/Dockerfile"
                      disabled={actionLoading !== null}
                    />
                  </>
                ) : (
                  <div className={styles.dockerfileContentGroup}>
                    <label className={styles.dockerfileLabel}>Dockerfile Content</label>
                    <textarea
                      className={styles.dockerfileTextarea}
                      value={dockerfileForm.content}
                      onChange={(e) => setDockerfileForm((f) => ({ ...f, content: e.target.value }))}
                      placeholder={'FROM node:20-alpine\nWORKDIR /app\nCOPY . .\nRUN npm install\nCMD ["node", "index.js"]'}
                      disabled={actionLoading !== null}
                    />
                  </div>
                )}

                <div className={styles.buttonContainer}>
                  <Button
                    variant="primary"
                    onClick={handleSaveDockerfileConfiguration}
                    isLoading={actionLoading === 'saveConfig'}
                    disabled={actionLoading !== null}
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
        </div>
      ),
    },
    {
      id: 'environment',
      label: t('services:environment'),
      content:
        projectId && environmentId && serviceId ? (
          <ServiceVariablesEditor
            projectId={projectId}
            environmentId={environmentId}
            serviceId={serviceId}
          />
        ) : null,
    },
    {
      id: 'featureFlags',
      label: t('services:featureFlags') || 'Feature Flags',
      content:
        projectId && environmentId && serviceId ? (
          <FeatureFlagsEditor
            projectId={projectId}
            environmentId={environmentId}
            serviceId={serviceId}
          />
        ) : null,
    },
    {
      id: 'logs',
      label: t('services:logs'),
      content: (
        <FeaturePanel
          title={t('services:logs')}
          description={t('services:logsDescription')}
          empty
          emptyMessage={t('services:noLogs')}
        />
      ),
    },
  ]

  return (
    <div className={styles.container}>
      {error && (
        <div className={styles.errorBanner}>
          <div className={styles.errorBannerContent}>
            <p>{error}</p>
            <button
              className={styles.errorBannerClose}
              onClick={() => setError(null)}
              title={t('projects:close')}
            >
              ✕
            </button>
          </div>
        </div>
      )}
      <div className={styles.header}>
        <div className={styles.back}>
          <button
            onClick={() =>
              navigate(`/projects/${projectId}/environments/${environmentId}`)
            }
          >
            ← {t('projects:back')}
          </button>
        </div>
        <div className={styles.titleSection}>
          <div className={styles.title}>
            <h1>{service.name}</h1>
            <p className={styles.breadcrumb}>
              {project.name} → {environment.name}
            </p>
          </div>
          <div className={styles.actions}>
            <Button
              variant="secondary"
              icon={<Play size={18} />}
              onClick={handleDeploy}
              disabled={actionLoading !== null}
              isLoading={actionLoading === 'deploy'}
              title={service.status === 'Running' ? t('services:redeploy') : t('services:deploy')}
            >
              {service.status === 'Running' ? t('services:redeploy') : t('services:deploy')}
            </Button>
            {service.status === 'Running' && (
              <Button
                variant="secondary"
                icon={<RotateCw size={18} />}
                onClick={handleRestart}
                disabled={actionLoading !== null}
                isLoading={actionLoading === 'restart'}
                title={t('services:restart')}
              >
                {t('services:restart')}
              </Button>
            )}
            {service.status === 'Running' && (
              <Button
                variant="secondary"
                icon={<Square size={18} />}
                onClick={handleStop}
                disabled={actionLoading !== null}
                isLoading={actionLoading === 'stop'}
                title={t('services:stop')}
              >
                {t('services:stop')}
              </Button>
            )}
            <Button
              variant="danger"
              icon={<Trash2 size={18} />}
              onClick={() => setIsDeleteConfirmOpen(true)}
              title={t('services:delete')}
            >
              {t('services:delete')}
            </Button>
          </div>
        </div>
        <div className={styles.stats}>
          <div className={styles.statItem}>
            <span className={styles.statLabel}>{t('services:type')}</span>
            <span className={styles.statValue}>{service.type}</span>
          </div>
          <div className={styles.statItem}>
            <span className={styles.statLabel}>{t('services:status')}</span>
            <span className={`${styles.statValue} ${styles[`status${service.status}`]}`}>
              {service.status}
            </span>
          </div>
          <div className={styles.statItem}>
            <span className={styles.statLabel}>{t('services:exposure')}</span>
            <span className={styles.statValue}>{service.exposureMode}</span>
          </div>
        </div>
      </div>

      <Tabs items={tabs} defaultTab="overview" />

      {isDeleteConfirmOpen && (
        <div className={styles.deleteConfirmOverlay}>
          <div className={styles.deleteConfirmDialog}>
            <h2 className={styles.deleteConfirmTitle}>
              {t('services:deleteServiceTitle') || 'Delete Service?'}
            </h2>
            <p className={styles.deleteConfirmMessage}>
              {t('services:deleteServiceMessage', { name: service?.name }) ||
                `Are you sure you want to delete "${service?.name}"? This action cannot be undone.`}
            </p>
            <div className={styles.deleteConfirmActions}>
              <Button
                variant="ghost"
                onClick={() => setIsDeleteConfirmOpen(false)}
                disabled={isDeleting}
              >
                {t('projects:cancel') || 'Cancel'}
              </Button>
              <Button
                variant="danger"
                onClick={handleDeleteService}
                isLoading={isDeleting}
              >
                {t('services:delete') || 'Delete Service'}
              </Button>
            </div>
          </div>
        </div>
      )}

      {isRegenerateConfirmOpen && (
        <div className={styles.deleteConfirmOverlay}>
          <div className={styles.deleteConfirmDialog}>
            <h2 className={styles.deleteConfirmTitle}>
              {t('services:regenerateTokenTitle') || 'Regenerate Token?'}
            </h2>
            <p className={styles.deleteConfirmMessage}>
              {t('services:regenerateTokenWarning') ||
                'Regenerating the token will invalidate the current webhook URL and may break CI/CD pipelines that depend on it. Make sure to update any external systems using this URL.'}
            </p>
            <div className={styles.deleteConfirmActions}>
              <Button
                variant="ghost"
                onClick={() => setIsRegenerateConfirmOpen(false)}
                disabled={actionLoading === 'regenerateToken'}
              >
                {t('projects:cancel') || 'Cancel'}
              </Button>
              <Button
                variant="danger"
                onClick={handleRegenerateTokenConfirm}
                isLoading={actionLoading === 'regenerateToken'}
              >
                {t('services:regenerateToken') || 'Regenerate Token'}
              </Button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
