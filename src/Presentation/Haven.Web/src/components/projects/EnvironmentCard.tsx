import { useTranslation } from 'react-i18next'
import { Edit2 } from 'lucide-react'
import { EnvironmentDto } from '../../api/types'
import { Card, CardContent, CardHeader } from '../ui/Card'
import styles from './EnvironmentCard.module.css'

interface EnvironmentCardProps {
  environment: EnvironmentDto
  serviceCount?: number
  onClick?: (projectId: string, environmentId: string) => void
  onEdit?: (environment: EnvironmentDto) => void
}

export function EnvironmentCard({
  environment,
  serviceCount = 0,
  onClick,
  onEdit,
}: EnvironmentCardProps) {
  const { t } = useTranslation('projects')

  const handleEdit = (e: React.MouseEvent) => {
    e.stopPropagation()
    onEdit?.(environment)
  }

  return (
    <Card
      className={styles.environmentCard}
      onClick={() => onClick?.(environment.projectId, environment.id)}
      role="button"
      tabIndex={0}
      onKeyDown={(e) => {
        if (e.key === 'Enter' || e.key === ' ') {
          onClick?.(environment.projectId, environment.id)
        }
      }}
    >
      <CardHeader>
        <div className={styles.headerWithAction}>
          <div>
            <h4 className={styles.title}>{environment.name}</h4>
            <p className={styles.network}>{environment.networkName}</p>
          </div>
          {onEdit && (
            <button
              className={styles.editButton}
              onClick={handleEdit}
              title={t('edit')}
              aria-label={`${t('edit')} ${environment.name}`}
            >
              <Edit2 size={18} />
            </button>
          )}
        </div>
      </CardHeader>
      <CardContent>
        <p className={styles.description}>
          {environment.description || t('noDescription')}
        </p>
        <div className={styles.stat}>
          <span className={styles.statLabel}>{t('services')}</span>
          <span className={styles.statValue}>{serviceCount}</span>
        </div>
      </CardContent>
    </Card>
  )
}
