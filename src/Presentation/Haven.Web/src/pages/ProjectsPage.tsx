import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Plus } from 'lucide-react'
import { projectsApi } from '../api/projects'
import { PagedResult, ProjectDashboardDto } from '../api/types'
import { ProjectsList } from '../components/projects/ProjectsList'
import { CreateProjectModal } from '../components/projects/CreateProjectModal'
import { Button } from '../components/ui/Button'
import { PermissionGuard } from '@/components/PermissionGuard'
import { useSetBreadcrumbs } from '@/hooks/useSetBreadcrumbs'
import styles from './ProjectsPage.module.css'

const PAGE_SIZE = 12

export function ProjectsPage() {
  const navigate = useNavigate()
  const { t } = useTranslation('projects')
  const [projects, setProjects] = useState<PagedResult<ProjectDashboardDto> | null>(null)
  const [currentPage, setCurrentPage] = useState(1)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false)
  const [editingProject, setEditingProject] = useState<ProjectDashboardDto | null>(null)

  useSetBreadcrumbs([{ label: 'Projects' }])

  useEffect(() => {
    const loadProjects = async () => {
      try {
        setLoading(true)
        setError(null)
        const result = await projectsApi.getDashboard({
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

  const handleEditProjectSuccess = async (_projectId: string) => {
    setCurrentPage(1)
    try {
      const result = await projectsApi.getDashboard({
        pageNumber: 1,
        pageSize: PAGE_SIZE,
      })
      setProjects(result)
    } catch (err) {
      console.error('Failed to refresh projects', err)
    }
  }

  const handleRowClick = (projectId: string) => {
    navigate(`/projects/${projectId}`)
  }

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <div className={styles.headerContent}>
          <h1>{t('title')}</h1>
          <p className={styles.subtitle}>{t('subtitle')}</p>
        </div>
        <PermissionGuard permission="projects.create">
          <Button
            variant="primary"
            icon={<Plus size={20} />}
            onClick={() => navigate('/projects/create')}
            disabled={loading}
          >
            New Project
          </Button>
        </PermissionGuard>
      </div>

      {error && <div className={styles.error}>{error}</div>}

      {!error && (
        <ProjectsList
          projects={projects?.items || []}
          onRowClick={handleRowClick}
          onEdit={(project) => {
            setEditingProject(project)
            setIsCreateModalOpen(true)
          }}
          isLoading={loading}
        />
      )}

      {!error && projects && projects.totalPages > 1 && (
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
        onSuccess={handleEditProjectSuccess}
        project={editingProject}
      />
    </div>
  )
}
