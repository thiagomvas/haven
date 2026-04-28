import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { ProjectDto } from '../../api/types'
import { Card, CardContent, CardHeader } from '../ui/Card'
import styles from './ProjectCard.module.css'

interface ProjectCardProps {
  project: ProjectDto
}

export function ProjectCard({ project }: ProjectCardProps) {
  const navigate = useNavigate()
  const { t } = useTranslation('projects')

  const handleClick = () => {
    navigate(`/projects/${project.id}`)
  }

  return (
    <Card
      className={styles.projectCard}
      onClick={handleClick}
      role="button"
      tabIndex={0}
      onKeyDown={(e) => {
        if (e.key === 'Enter' || e.key === ' ') {
          handleClick()
        }
      }}
    >
      <CardHeader>
        <h3 className={styles.title}>{project.name}</h3>
      </CardHeader>
      <CardContent>
        <p className={styles.description}>
          {project.description || t('noDescription')}
        </p>
        <div className={styles.stats}>
          <div className={styles.stat}>
            <span className={styles.statLabel}>{t('environments')}</span>
            <span className={styles.statValue}>{project.environmentCount}</span>
          </div>
          <div className={styles.stat}>
            <span className={styles.statLabel}>{t('services')}</span>
            <span className={styles.statValue}>{project.serviceCount}</span>
          </div>
        </div>
      </CardContent>
    </Card>
  )
}
