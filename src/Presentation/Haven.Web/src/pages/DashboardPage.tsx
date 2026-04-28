import { useTranslation } from 'react-i18next'
import { useProjects } from '@/hooks/useProjects'
import { useEvents } from '@/hooks/useEvents'
import { Card, CardContent, CardHeader } from '@/components/ui/Card'
import { Badge } from '@/components/ui/Badge'
import { Spinner } from '@/components/ui/Spinner'
import { formatRelative, getStatusColor } from '@/lib/utils'
import styles from './DashboardPage.module.css'

export function DashboardPage() {
  const { t } = useTranslation('dashboard')
  const { t: tCommon } = useTranslation('common')
  const { data: projectsData, isLoading: projectsLoading } =
    useProjects({ pageSize: 100 })
  const { data: eventsData, isLoading: eventsLoading } =
    useEvents({ pageSize: 5 })

  return (
    <div className={styles.container}>
      <div className={styles.grid}>
        {/* Project Count Card */}
        <Card>
          <CardContent className={styles.statCard}>
            <div className={styles.statLabel}>{t('stats.totalProjects')}</div>
            {projectsLoading ? (
              <Spinner size="lg" />
            ) : (
              <div className={styles.statValue}>
                {projectsData?.totalCount ?? 0}
              </div>
            )}
          </CardContent>
        </Card>

        {/* Empty card for balance */}
        <Card>
          <CardContent className={styles.statCard}>
            <div className={styles.statLabel}>{t('stats.totalEnvironments')}</div>
            <div className={styles.statValue}>{t('stats.environmentsPlaceholder')}</div>
          </CardContent>
        </Card>
      </div>

      {/* Recent Events */}
      <Card>
        <CardHeader>
          <h2 className={styles.sectionTitle}>{t('recentEvents.sectionTitle')}</h2>
        </CardHeader>
        <CardContent className={styles.eventsList}>
          {eventsLoading ? (
            <div className={styles.loadingContainer}>
              <Spinner size="md" />
            </div>
          ) : eventsData?.items?.length ? (
            <div className={styles.events}>
              {eventsData.items.map((event) => (
                <div key={event.id} className={styles.eventItem}>
                  <div className={styles.eventType}>
                    <Badge variant="default">
                      {event.eventType}
                    </Badge>
                  </div>
                  <div className={styles.eventMessage}>
                    {event.message}
                  </div>
                  <div className={styles.eventTime}>
                    {formatRelative(event.triggeredAt, tCommon)}
                  </div>
                </div>
              ))}
            </div>
          ) : (
            <p className={styles.emptyState}>
              {t('recentEvents.empty')}
            </p>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
