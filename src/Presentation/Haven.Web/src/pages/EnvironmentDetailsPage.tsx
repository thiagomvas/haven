import { useEffect, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Plus } from 'lucide-react'
import { useSetBreadcrumbs } from '@/hooks/useSetBreadcrumbs'
import { usePermission } from '@/hooks/usePermission'
import { projectsApi } from '../api/projects'
import { environmentsApi } from '../api/environments'
import { servicesApi } from '../api/services'
import { ProjectDto, EnvironmentDto, ServiceDto, ServiceStatus } from '../api/types'
import { Tabs, TabItem } from '../components/ui/Tabs'
import { ServiceCard } from '../components/projects/ServiceCard'
import { EnvironmentSettingsForm } from '../components/environments/EnvironmentSettingsForm'
import { EnvironmentVariablesEditor } from '../components/environments/EnvironmentVariablesEditor'
import { Button } from '../components/ui/Button'
import { Spinner } from '../components/ui/Spinner'
import { serviceStatusHub } from '../lib/signalr/hubs'
import { useSubscribeToMultipleServices } from '../lib/signalr/useSubscribeToMultipleServices'
import styles from './EnvironmentDetailsPage.module.css'
import { ProjectAvatar } from '@/components/ui/ProjectAvatar'

export function EnvironmentDetailsPage() {
  const { projectId, environmentId } = useParams<{
    projectId: string
    environmentId: string
  }>()
  const navigate = useNavigate()
  const { t } = useTranslation(['projects', 'environments'])

  const [project, setProject] = useState<ProjectDto | null>(null)
  const [environment, setEnvironment] = useState<EnvironmentDto | null>(null)
  const [services, setServices] = useState<ServiceDto[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const canCreateService = usePermission('projects.create')
  const canUpdateEnvironment = usePermission('projects.create')
  const handleAddService = () => {
    navigate(`/services/create?projectId=${projectId}&environmentId=${environmentId}`)
  }

  useSetBreadcrumbs([
    { label: 'Projects', to: '/projects' },
    { label: project?.name ?? '…', to: projectId ? `/projects/${projectId}` : undefined },
    { label: environment?.name ?? '…' },
  ])

  useEffect(() => {
    const loadData = async () => {
      if (!projectId || !environmentId) return

      try {
        setLoading(true)
        setError(null)

        const [projectData, environmentData, servicesData] = await Promise.all([
          projectsApi.getById(projectId),
          environmentsApi.getById(projectId, environmentId),
          servicesApi.getByEnvironmentId(projectId, environmentId),
        ])

        if (!projectData) {
          setError('Project not found')
          return
        }

        if (!environmentData) {
          setError('Environment not found')
          return
        }

        setProject(projectData)
        setEnvironment(environmentData)
        setServices(servicesData || [])
      } catch (err) {
        setError(err instanceof Error ? err.message : t('error'))
      } finally {
        setLoading(false)
      }
    }

    loadData()
  }, [projectId, environmentId, t])

  useSubscribeToMultipleServices(
    serviceStatusHub,
    services.map((s) => s.id),
    (data) => {
      setServices((prevServices) =>
        prevServices.map((service) =>
          service.id === data.serviceId
            ? { ...service, status: data.newStatus as ServiceStatus }
            : service
        )
      )
    },
  )

  const handleEnvironmentUpdated = async () => {
    if (!projectId || !environmentId) return
    try {
      const environmentData = await environmentsApi.getById(projectId, environmentId)
      if (environmentData) {
        setEnvironment(environmentData)
      }
    } catch (err) {
      console.error('Failed to refresh environment', err)
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

  if (error || !project || !environment) {
    return (
      <div className={styles.container}>
        <div className={styles.error}>
          <p>{error || t('projects:notFound')}</p>
          <button onClick={() => navigate(`/projects/${projectId}`)}>
            {t('projects:back')}
          </button>
        </div>
      </div>
    )
  }

  const tabs: TabItem[] = [
    {
      id: 'services',
      label: t('environments:services'),
      content: (
        <div className={styles.servicesTab}>
          {services.length === 0 ? (
            <div className={styles.emptyState}>
              <p className={styles.emptyMessage}>{t('environments:noServices')}</p>
              {canCreateService && (
                <Button
                  variant="primary"
                  icon={<Plus size={20} />}
                  onClick={handleAddService}
                >
                  Add Service
                </Button>
              )}
            </div>
          ) : (
            <>
              {canCreateService && (
                <div className={styles.servicesHeader}>
                  <Button
                    variant="primary"
                    icon={<Plus size={20} />}
                    onClick={handleAddService}
                  >
                    Add Service
                  </Button>
                </div>
              )}
              <div className={styles.grid}>
                {services.map((service) => (
                  <ServiceCard
                    key={service.id}
                    service={service}
                    onClick={() =>
                      navigate(
                        `/projects/${projectId}/environments/${environmentId}/services/${service.id}`,
                      )
                    }
                  />
                ))}
              </div>
            </>
          )}
        </div>
      ),
    },
    {
      id: 'variables',
      label: t('environments:variables'),
      content:
        projectId && environmentId ? (
          <EnvironmentVariablesEditor
            projectId={projectId}
            environmentId={environmentId}
          />
        ) : null,
    },
    ...(canUpdateEnvironment ? [{
      id: 'configuration',
      label: t('environments:configuration'),
      content: projectId ? (
        <EnvironmentSettingsForm
          projectId={projectId}
          environment={environment}
          onSuccess={handleEnvironmentUpdated}
        />
      ) : null,
    }] : []),
  ]

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <div className={styles.title}>
          <h1>{environment.name}</h1>
          {environment.description && (
            <p className={styles.description}>{environment.description}</p>
          )}
        </div>
        <div className={styles.stats}>
          <div className={styles.statItem}>
            <span className={styles.statLabel}>{t('environments:services')}</span>
            <span className={styles.statValue}>{services.length}</span>
          </div>
          <div className={styles.statItem}>
            <span className={styles.statLabel}>{t('environments:network')}</span>
            <span className={styles.statValue}>{environment.networkName}</span>
          </div>
        </div>
      </div>

      <Tabs items={tabs} defaultTab="services" />
    </div>
  )
}
