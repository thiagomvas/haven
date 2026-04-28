import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { projectsApi } from '../api/projects'
import { PagedResult, ProjectDto } from '../api/types'
import { ProjectCard } from '../components/projects/ProjectCard'
import { Spinner } from '../components/ui/Spinner'
import styles from './ProjectsPage.module.css'

const PAGE_SIZE = 12

export function ProjectsPage() {
  const { t } = useTranslation('projects')
  const [projects, setProjects] = useState<PagedResult<ProjectDto> | null>(null)
  const [currentPage, setCurrentPage] = useState(1)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

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


  if (loading) {
    return (
      <div className={styles.container}>
        <div className={styles.header}>
          <h1>{t('title')}</h1>
          <p className={styles.subtitle}>{t('subtitle')}</p>
        </div>
        <div className={styles.spinner}>
          <Spinner />
          <p>{t('loading')}</p>
        </div>
      </div>
    )
  }

  if (error) {
    return (
      <div className={styles.container}>
        <div className={styles.header}>
          <h1>{t('title')}</h1>
          <p className={styles.subtitle}>{t('subtitle')}</p>
        </div>
        <div className={styles.error}>
          <p>{error}</p>
        </div>
      </div>
    )
  }

  if (!projects || projects.items.length === 0) {
    return (
      <div className={styles.container}>
        <div className={styles.header}>
          <h1>{t('title')}</h1>
          <p className={styles.subtitle}>{t('subtitle')}</p>
        </div>
        <div className={styles.empty}>
          <p>{t('emptyState')}</p>
        </div>
      </div>
    )
  }

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <h1>{t('title')}</h1>
        <p className={styles.subtitle}>{t('subtitle')}</p>
      </div>

      <div className={styles.grid}>
        {projects.items.map((project) => (
          <ProjectCard key={project.id} project={project} />
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
    </div>
  )
}
