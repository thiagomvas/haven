import { useEffect, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Plus, Edit2, Trash2 } from 'lucide-react'
import { projectsApi } from '../api/projects'
import { environmentsApi } from '../api/environments'
import { servicesApi } from '../api/services'
import { ProjectDto, EnvironmentDto, ServiceDto } from '../api/types'
import { Tabs, TabItem } from '../components/ui/Tabs'
import { FeaturePanel } from '../components/ui/FeaturePanel'
import { ServiceCard } from '../components/projects/ServiceCard'
import { CreateEnvironmentModal } from '../components/projects/CreateEnvironmentModal'
import { Button } from '../components/ui/Button'
import { Spinner } from '../components/ui/Spinner'
import styles from './EnvironmentDetailsPage.module.css'

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
  const [isEditModalOpen, setIsEditModalOpen] = useState(false)
  const [isDeleteConfirmOpen, setIsDeleteConfirmOpen] = useState(false)
  const [isDeleting, setIsDeleting] = useState(false)

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

  const handleEditSuccess = async () => {
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

  const handleDeleteEnvironment = async () => {
    if (!projectId || !environmentId) return
    try {
      setIsDeleting(true)
      await environmentsApi.delete(projectId, environmentId)
      setIsDeleteConfirmOpen(false)
      navigate(`/projects/${projectId}`)
    } catch (err) {
      console.error('Failed to delete environment', err)
      setError(err instanceof Error ? err.message : t('error'))
    } finally {
      setIsDeleting(false)
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
              <Button
                variant="primary"
                icon={<Plus size={20} />}
                onClick={() => {}}
              >
                Add Service
              </Button>
            </div>
          ) : (
            <>
              <div className={styles.servicesHeader}>
                <Button
                  variant="primary"
                  icon={<Plus size={20} />}
                  onClick={() => {}}
                >
                  Add Service
                </Button>
              </div>
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
      content: (
        <FeaturePanel
          title={t('environments:variables')}
          description={t('environments:variablesDescription')}
          empty
          emptyMessage={t('environments:noVariables')}
        />
      ),
    },
    {
      id: 'configuration',
      label: t('environments:configuration'),
      content: (
        <div className={styles.configurationTab}>
          <FeaturePanel
            title={t('environments:configuration')}
            description={t('environments:configurationDescription')}
            empty
            emptyMessage={t('environments:noConfiguration')}
          />
          <div className={styles.dangerZone}>
            <div className={styles.dangerZoneHeader}>
              <h3 className={styles.dangerZoneTitle}>
                {t('projects:dangerZone') || 'Danger Zone'}
              </h3>
              <p className={styles.dangerZoneDescription}>
                {t('projects:dangerZoneDescription') ||
                  'Irreversible and destructive actions'}
              </p>
            </div>
            <div className={styles.dangerZoneContent}>
              <div className={styles.dangerAction}>
                <div className={styles.actionInfo}>
                  <h4 className={styles.actionTitle}>
                    {t('environments:deleteEnvironment') || 'Delete Environment'}
                  </h4>
                  <p className={styles.actionDescription}>
                    {t('environments:deleteEnvironmentDescription') ||
                      'Once you delete an environment, there is no going back. Please be certain.'}
                  </p>
                </div>
                <Button
                  variant="danger"
                  icon={<Trash2 size={18} />}
                  onClick={() => setIsDeleteConfirmOpen(true)}
                  disabled={isDeleting}
                >
                  {t('projects:delete') || 'Delete'}
                </Button>
              </div>
            </div>
          </div>
        </div>
      ),
    },
  ]

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <div className={styles.back}>
          <button onClick={() => navigate(`/projects/${projectId}`)}>
            ← {t('projects:back')}
          </button>
        </div>
        <div className={styles.titleWithAction}>
          <div className={styles.title}>
            <h1>{environment.name}</h1>
            {environment.description && (
              <p className={styles.description}>{environment.description}</p>
            )}
          </div>
          <button
            className={styles.editButton}
            onClick={() => setIsEditModalOpen(true)}
            title={t('projects:edit')}
            aria-label={`${t('projects:edit')} ${environment.name}`}
          >
            <Edit2 size={20} />
          </button>
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

      {projectId && environmentId && (
        <>
          <CreateEnvironmentModal
            projectId={projectId}
            isOpen={isEditModalOpen}
            onClose={() => setIsEditModalOpen(false)}
            onSuccess={handleEditSuccess}
            environment={environment}
          />
        </>
      )}

      {isDeleteConfirmOpen && (
        <div className={styles.deleteConfirmOverlay}>
          <div className={styles.deleteConfirmDialog}>
            <h2 className={styles.deleteConfirmTitle}>
              {t('environments:deleteEnvironmentTitle') ||
                'Delete Environment?'}
            </h2>
            <p className={styles.deleteConfirmMessage}>
              {t('environments:deleteEnvironmentMessage', {
                name: environment?.name,
              }) ||
                `Are you sure you want to delete "${environment?.name}"? This action cannot be undone.`}
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
                onClick={handleDeleteEnvironment}
                isLoading={isDeleting}
              >
                {t('environments:deleteEnvironment') || 'Delete Environment'}
              </Button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
