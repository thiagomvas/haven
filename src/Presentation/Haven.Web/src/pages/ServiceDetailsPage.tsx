import { useEffect, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Play, Square, RotateCw, Trash2, Copy, Check } from 'lucide-react'
import { projectsApi } from '../api/projects'
import { environmentsApi } from '../api/environments'
import { servicesApi } from '../api/services'
import { ProjectDto, EnvironmentDto, ServiceDto } from '../api/types'
import { Tabs, TabItem } from '../components/ui/Tabs'
import { FeaturePanel } from '../components/ui/FeaturePanel'
import { DockerConfigForm } from '../components/projects/DockerConfigForm'
import { Button } from '../components/ui/Button'
import { Spinner } from '../components/ui/Spinner'
import { DockerConfig } from '../api/types'
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
  const [isDeleteConfirmOpen, setIsDeleteConfirmOpen] = useState(false)
  const [isDeleting, setIsDeleting] = useState(false)

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

  const handleDeleteService = async () => {
    // TODO: Implement service deletion when API endpoint exists
    setIsDeleteConfirmOpen(false)
  }

  const handleSaveConfiguration = async (config: DockerConfig) => {
    if (!projectId || !environmentId || !serviceId) return
    try {
      setActionLoading('saveConfig')
      await servicesApi.update(projectId, environmentId, serviceId, {
        dockerConfig: config,
      })
      // Refresh service data
      const updatedService = await servicesApi.getById(
        projectId,
        environmentId,
        serviceId,
      )
      setService(updatedService)
    } catch (err) {
      console.error('Failed to save configuration', err)
      setError(err instanceof Error ? err.message : t('error'))
    } finally {
      setActionLoading(null)
    }
  }

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

  if (error || !project || !environment || !service) {
    return (
      <div className={styles.container}>
        <div className={styles.error}>
          <p>{error || t('projects:notFound')}</p>
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
          {service.type === 'DockerImage' ? (
            <DockerConfigForm
              config={service.sourceConfig as DockerConfig | undefined}
              onSave={handleSaveConfiguration}
              isLoading={actionLoading === 'saveConfig'}
            />
          ) : (
            <FeaturePanel
              title={t('services:configuration')}
              description={`${service.type} configuration`}
              empty
              emptyMessage={t('services:noConfiguration')}
            />
          )}
        </div>
      ),
    },
    {
      id: 'environment',
      label: t('services:environment'),
      content: (
        <FeaturePanel
          title={t('services:environment')}
          description={t('services:environmentDescription')}
          empty
          emptyMessage={t('services:noEnvironment')}
        />
      ),
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
              title={t('services:deploy')}
            >
              {t('services:deploy')}
            </Button>
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
    </div>
  )
}
