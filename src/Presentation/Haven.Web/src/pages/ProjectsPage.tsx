import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Plus } from 'lucide-react'
import { projectsApi } from '../api/projects'
import { PagedResult, ProjectDto } from '../api/types'
import { ProjectCard } from '../components/projects/ProjectCard'
import { CreateProjectModal } from '../components/projects/CreateProjectModal'
import { Button } from '../components/ui/Button'
import { Spinner } from '../components/ui/Spinner'
import styles from './ProjectsPage.module.css'

const PAGE_SIZE = 12

export function ProjectsPage() {
  const navigate = useNavigate()
  const { t } = useTranslation('projects')
  const [projects, setProjects] = useState<PagedResult<ProjectDto> | null>(null)
  const [currentPage, setCurrentPage] = useState(1)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false)
  const [editingProject, setEditingProject] = useState<ProjectDto | null>(null)

  useEffect(() => {
    const loadProjects = async () => {
      try {
        setLoading(true)
        setError(null)
        const result = await projectsApi.getAll({
          pageNumber: currentPage,
          pageSize: PAGE_SIZE,
        })
        setProjects(result)
      } catch (err) {
        setError(err instanceof Error ? err.message : t('error'))
      } finally {
        setLoading(false)
      }
    }

    loadProjects()
  }, [currentPage, t])

  const handleCreateProjectSuccess = async (projectId: string) => {
    setCurrentPage(1)
    // Refresh projects list after create/edit
    try {
      const result = await projectsApi.getAll({
        pageNumber: 1,
        pageSize: PAGE_SIZE,
      })
      setProjects(result)
    } catch (err) {
      console.error('Failed to refresh projects', err)
    }
    // Only navigate if it was a create operation (not an edit)
    if (!editingProject) {
      navigate(`/projects/${projectId}`)
    }
  }

  if (loading) {
    return (
      <div className={styles.container}>
        <div className={styles.header}>
          <div className={styles.headerContent}>
            <h1>{t('title')}</h1>
            <p className={styles.subtitle}>{t('subtitle')}</p>
          </div>
          <Button
            variant="primary"
            icon={<Plus size={20}  />}
            onClick={() => setIsCreateModalOpen(true)}
            disabled
          >
            New Project
          </Button>
        </div>
        <div className={styles.spinner}>
          <Spinner />
          <p>{t('loading')}</p>
        </div>
        <CreateProjectModal
          isOpen={isCreateModalOpen}
          onClose={() => {
            setIsCreateModalOpen(false)
            setEditingProject(null)
          }}
          onSuccess={handleCreateProjectSuccess}
          project={editingProject || undefined}
        />
      </div>
    )
  }

  if (error) {
    return (
      <div className={styles.container}>
        <div className={styles.header}>
          <div className={styles.headerContent}>
            <h1>{t('title')}</h1>
            <p className={styles.subtitle}>{t('subtitle')}</p>
          </div>
          <Button
            variant="primary"
            icon={<Plus size={20}  />}
            onClick={() => setIsCreateModalOpen(true)}
          >
            New Project
          </Button>
        </div>
        <div className={styles.error}>
          <p>{error}</p>
        </div>
        <CreateProjectModal
          isOpen={isCreateModalOpen}
          onClose={() => {
            setIsCreateModalOpen(false)
            setEditingProject(null)
          }}
          onSuccess={handleCreateProjectSuccess}
          project={editingProject || undefined}
        />
      </div>
    )
  }

  if (!projects || projects.items.length === 0) {
    return (
      <div className={styles.container}>
        <div className={styles.header}>
          <div className={styles.headerContent}>
            <h1>{t('title')}</h1>
            <p className={styles.subtitle}>{t('subtitle')}</p>
          </div>
          <Button
            variant="primary"
            icon={<Plus size={20}  />}
            onClick={() => setIsCreateModalOpen(true)}
          >
            New Project
          </Button>
        </div>
        <div className={styles.empty}>
          <p>{t('emptyState')}</p>
        </div>
        <CreateProjectModal
          isOpen={isCreateModalOpen}
          onClose={() => {
            setIsCreateModalOpen(false)
            setEditingProject(null)
          }}
          onSuccess={handleCreateProjectSuccess}
          project={editingProject || undefined}
        />
      </div>
    )
  }

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <div className={styles.headerContent}>
          <h1>{t('title')}</h1>
          <p className={styles.subtitle}>{t('subtitle')}</p>
        </div>
        <Button
          variant="primary"
          icon={<Plus size={20}  />}
          onClick={() => setIsCreateModalOpen(true)}
        >
          New Project
        </Button>
      </div>

      <div className={styles.grid}>
        {projects.items.map((project) => (
          <ProjectCard
            key={project.id}
            project={project}
            onEdit={(project) => {
              setEditingProject(project)
              setIsCreateModalOpen(true)
            }}
          />
        ))}
      </div>

      {projects.totalPages > 1 && (
        <div className={styles.pagination}>
          <button
            className={styles.paginationButton}
            onClick={() => setCurrentPage((p) => Math.max(1, p - 1))}
            disabled={!projects.hasPreviousPage}
          >
            {t('previousPage')}
          </button>
          <span className={styles.paginationInfo}>
            {t('pageOf', {
              current: projects.pageNumber,
              total: projects.totalPages,
            })}
          </span>
          <button
            className={styles.paginationButton}
            onClick={() => setCurrentPage((p) => p + 1)}
            disabled={!projects.hasNextPage}
          >
            {t('nextPage')}
          </button>
        </div>
      )}

      <CreateProjectModal
        isOpen={isCreateModalOpen}
        onClose={() => {
          setIsCreateModalOpen(false)
          setEditingProject(null)
        }}
        onSuccess={handleCreateProjectSuccess}
        project={editingProject || undefined}
      />
    </div>
  )
}
