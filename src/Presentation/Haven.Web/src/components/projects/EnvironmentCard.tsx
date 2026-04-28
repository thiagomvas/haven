import { useTranslation } from 'react-i18next'
import { EnvironmentDto } from '../../api/types'
import { Card, CardContent, CardHeader } from '../ui/Card'
import styles from './EnvironmentCard.module.css'

interface EnvironmentCardProps {
  environment: EnvironmentDto
  serviceCount?: number
  onClick?: (projectId: string, environmentId: string) => void
}

export function EnvironmentCard({
  environment,
  serviceCount = 0,
  onClick,
}: EnvironmentCardProps) {
  const { t } = useTranslation('projects')

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
        <h4 className={styles.title}>{environment.name}</h4>
        <p className={styles.network}>{environment.networkName}</p>
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
