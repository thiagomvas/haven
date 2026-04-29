import { useEffect, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Plus, Edit2, Trash2 } from 'lucide-react'
import { projectsApi } from '../api/projects'
import { environmentsApi } from '../api/environments'
import { ProjectDto, EnvironmentDto } from '../api/types'
import { Tabs, TabItem } from '../components/ui/Tabs'
import { FeaturePanel } from '../components/ui/FeaturePanel'
import { EnvironmentCard } from '../components/projects/EnvironmentCard'
import { CreateEnvironmentModal } from '../components/projects/CreateEnvironmentModal'
import { CreateProjectModal } from '../components/projects/CreateProjectModal'
import { Button } from '../components/ui/Button'
import { Spinner } from '../components/ui/Spinner'
import styles from './ProjectDetailsPage.module.css'

export function ProjectDetailsPage() {
  const { projectId } = useParams<{ projectId: string }>()
  const navigate = useNavigate()
  const { t } = useTranslation('projects')

  const [project, setProject] = useState<ProjectDto | null>(null)
  const [environments, setEnvironments] = useState<EnvironmentDto[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [isCreateEnvModalOpen, setIsCreateEnvModalOpen] = useState(false)
  const [editingEnvironment, setEditingEnvironment] = useState<EnvironmentDto | null>(null)
  const [isEditProjectModalOpen, setIsEditProjectModalOpen] = useState(false)
  const [isDeleteConfirmOpen, setIsDeleteConfirmOpen] = useState(false)
  const [isDeleting, setIsDeleting] = useState(false)

  useEffect(() => {
    const loadProjectData = async () => {
      if (!projectId) return

      try {
        setLoading(true)
        setError(null)

        const [projectData, environmentsData] = await Promise.all([
          projectsApi.getById(projectId),
          environmentsApi.getByProjectId(projectId),
        ])

        if (!projectData) {
          setError('Project not found')
          return
        }

        setProject(projectData)
        setEnvironments(environmentsData || [])
      } catch (err) {
        setError(err instanceof Error ? err.message : t('error'))
      } finally {
        setLoading(false)
      }
    }

    loadProjectData()
  }, [projectId, t])

  const handleCreateEnvironmentSuccess = async () => {
    if (!projectId) return
    try {
      const environmentsData = await environmentsApi.getByProjectId(projectId)
      setEnvironments(environmentsData || [])
    } catch (err) {
      console.error('Failed to refresh environments', err)
    }
  }

  const handleEditProjectSuccess = async () => {
    if (!projectId) return
    try {
      const projectData = await projectsApi.getById(projectId)
      if (projectData) {
        setProject(projectData)
      }
    } catch (err) {
      console.error('Failed to refresh project', err)
    }
  }

  const handleDeleteProject = async () => {
    if (!projectId) return
    try {
      setIsDeleting(true)
      await projectsApi.delete(projectId)
      setIsDeleteConfirmOpen(false)
      navigate('/projects')
    } catch (err) {
      console.error('Failed to delete project', err)
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
          <p>{t('loading')}</p>
        </div>
      </div>
    )
  }

  if (error || !project) {
    return (
      <div className={styles.container}>
        <div className={styles.error}>
          <p>{error || t('notFound')}</p>
          <button onClick={() => navigate('/projects')}>
            {t('back')}
          </button>
        </div>
      </div>
    )
  }

  const tabs: TabItem[] = [
    {
      id: 'environments',
      label: t('environments'),
      content: (
        <div className={styles.environmentsTab}>
          {environments.length === 0 ? (
            <div className={styles.emptyState}>
              <p className={styles.emptyMessage}>{t('noEnvironments')}</p>
              <Button
                variant="primary"
                icon={<Plus size={20}  />}
                onClick={() => setIsCreateEnvModalOpen(true)}
              >
                Add Environment
              </Button>
            </div>
          ) : (
            <>
              <div className={styles.environmentsHeader}>
                <Button
                  variant="primary"
                  icon={<Plus size={20} />}
                  onClick={() => setIsCreateEnvModalOpen(true)}
                >
                  Add Environment
                </Button>
              </div>
              <div className={styles.grid}>
                {environments.map((env) => (
                  <EnvironmentCard
                    key={env.id}
                    environment={env}
                    serviceCount={env.serviceCount}
                    onClick={(projId, envId) =>
                      navigate(`/projects/${projId}/environments/${envId}/services`)
                    }
                    onEdit={(environment) => {
                      setEditingEnvironment(environment)
                      setIsCreateEnvModalOpen(true)
                    }}
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
      label: t('variables'),
      content: (
        <FeaturePanel
          title={t('variables')}
          description={t('variablesDescription')}
          empty
          emptyMessage={t('noVariables')}
        />
      ),
    },
    {
      id: 'settings',
      label: t('settings'),
      content: (
        <div className={styles.settingsTab}>
          <div className={styles.dangerZone}>
            <div className={styles.dangerZoneHeader}>
              <h3 className={styles.dangerZoneTitle}>{t('dangerZone') || 'Danger Zone'}</h3>
              <p className={styles.dangerZoneDescription}>
                {t('dangerZoneDescription') || 'Irreversible and destructive actions'}
              </p>
            </div>
            <div className={styles.dangerZoneContent}>
              <div className={styles.dangerAction}>
                <div className={styles.actionInfo}>
                  <h4 className={styles.actionTitle}>{t('deleteProject') || 'Delete Project'}</h4>
                  <p className={styles.actionDescription}>
                    {t('deleteProjectDescription') || 'Once you delete a project, there is no going back. Please be certain.'}
                  </p>
                </div>
                <Button
                  variant="danger"
                  icon={<Trash2 size={18} />}
                  onClick={() => setIsDeleteConfirmOpen(true)}
                  disabled={isDeleting}
                >
                  {t('delete') || 'Delete'}
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
          <button onClick={() => navigate('/projects')}>← {t('back')}</button>
        </div>
        <div className={styles.titleWithAction}>
          <div className={styles.title}>
            <h1>{project.name}</h1>
            {project.description && (
              <p className={styles.description}>{project.description}</p>
            )}
          </div>
          <button
            className={styles.editButton}
            onClick={() => setIsEditProjectModalOpen(true)}
            title={t('edit')}
            aria-label={`${t('edit')} ${project.name}`}
          >
            <Edit2 size={20} />
          </button>
        </div>
        <div className={styles.stats}>
          <div className={styles.statItem}>
            <span className={styles.statLabel}>{t('environments')}</span>
            <span className={styles.statValue}>{project.environmentCount}</span>
          </div>
          <div className={styles.statItem}>
            <span className={styles.statLabel}>{t('services')}</span>
            <span className={styles.statValue}>{project.serviceCount}</span>
          </div>
        </div>
      </div>

      <Tabs items={tabs} defaultTab="environments" />

      {projectId && (
        <>
          <CreateEnvironmentModal
            projectId={projectId}
            isOpen={isCreateEnvModalOpen}
            onClose={() => {
              setIsCreateEnvModalOpen(false)
              setEditingEnvironment(null)
            }}
            onSuccess={handleCreateEnvironmentSuccess}
            environment={editingEnvironment || undefined}
          />
          <CreateProjectModal
            isOpen={isEditProjectModalOpen}
            onClose={() => setIsEditProjectModalOpen(false)}
            onSuccess={handleEditProjectSuccess}
            project={project || undefined}
          />
        </>
      )}

      {isDeleteConfirmOpen && (
        <div className={styles.deleteConfirmOverlay}>
          <div className={styles.deleteConfirmDialog}>
            <h2 className={styles.deleteConfirmTitle}>
              {t('deleteProjectTitle') || 'Delete Project?'}
            </h2>
            <p className={styles.deleteConfirmMessage}>
              {t('deleteProjectMessage', { name: project?.name }) ||
                `Are you sure you want to delete "${project?.name}"? This action cannot be undone.`}
            </p>
            <div className={styles.deleteConfirmActions}>
              <Button
                variant="ghost"
                onClick={() => setIsDeleteConfirmOpen(false)}
                disabled={isDeleting}
              >
                {t('cancel') || 'Cancel'}
              </Button>
              <Button
                variant="danger"
                onClick={handleDeleteProject}
                isLoading={isDeleting}
              >
                {t('deleteProject') || 'Delete Project'}
              </Button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
