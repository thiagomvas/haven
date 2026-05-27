import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import { Plus, Network, HardDrive } from 'lucide-react'
import { useProjectsDashboard } from '@/hooks/useProjects'
import { useEvents } from '@/hooks/useEvents'
import { Card, CardContent, CardHeader } from '@/components/ui/Card'
import { Badge } from '@/components/ui/Badge'
import { Spinner } from '@/components/ui/Spinner'
import { Button } from '@/components/ui/Button'
import { formatRelative, getStatusColor } from '@/lib/utils'
import styles from './DashboardPage.module.css'
import { EventIcon } from '@/components/ui/EventIcon'
import { EnvironmentStatusChip } from '@/components/ui/EnvironmentStatusChip'
import { ProjectAvatar } from '@/components/ui/ProjectAvatar'
import type { ProjectDashboardDto } from '@/api/types'
import type { EnvironmentStatus } from '@/components/ui/EnvironmentStatusChip'

function getEnvironmentStatus(
  project: ProjectDashboardDto,
  envId: string
): EnvironmentStatus {
  const env = project.environments.find((e) => e.id === envId)
  if (!env) return 'empty'
  if (env.serviceStatistics.running === 0) return 'stopped'
  if (env.serviceStatistics.running === env.serviceStatistics.total) return 'running'
  return 'partial'
}

function getProjectServiceStatus(project: ProjectDashboardDto): EnvironmentStatus {
  if (project.serviceStatistics.total === 0) return 'empty'
  if (project.serviceStatistics.running === 0) return 'stopped'
  if (project.serviceStatistics.running === project.serviceStatistics.total) return 'running'
  return 'partial'
}

export function DashboardPage() {
  const { t } = useTranslation('dashboard')
  const { t: tCommon } = useTranslation('common')
  const navigate = useNavigate()
  const { data: projectsData, isLoading: projectsLoading } =
    useProjectsDashboard({ pageSize: 100 })
  const { data: eventsData, isLoading: eventsLoading } =
    useEvents({ pageSize: 5 })

  const handleRowClick = (projectId: string) => {
    navigate(`/projects/${projectId}`)
  }

  return (
    <div className={styles.container}>
      <div className={styles.mainGrid}>
        {/* Left Column */}
        <div className={styles.leftColumn}>
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
          <Card>
            <CardHeader>
              <h2 className={styles.sectionTitle}>{tCommon('labels.projects')}</h2>
            </CardHeader>
            <CardContent className={styles.tableContent}>
              {projectsLoading ? (
                <div className={styles.loadingContainer}>
                  <Spinner size="md" />
                </div>
              ) : projectsData?.items?.length ? (
                <table className={styles.projectsTable}>
                  <thead>
                    <tr>
                      <th>{tCommon('labels.project')}</th>
                      <th>{tCommon('labels.environments')}</th>
                      <th>{tCommon('labels.services')}</th>
                      <th>{t('lastDeploy')}</th>
                    </tr>
                  </thead>
                  <tbody>
                    {projectsData.items.map((project) => (
                      <tr
                        key={project.id}
                        className={styles.tableRow}
                        onClick={() => handleRowClick(project.id)}
                      >
                        <td className={styles.projectCell}>
                          <ProjectAvatar
                            name={project.name}
                            description={project.description}
                            showText={true}
                          />
                        </td>
                        <td className={styles.environmentsCell}>
                          <div className={styles.environmentsList}>
                            {project.environments.map((env) => (
                              <EnvironmentStatusChip
                                key={env.id}
                                name={env.name}
                                status={getEnvironmentStatus(project, env.id)}
                              />
                            ))}
                          </div>
                        </td>
                        <td className={styles.servicesCell}>
                          <span className={styles[getProjectServiceStatus(project)]}>
                            {project.serviceStatistics.running}
                          </span>
                          /{project.serviceStatistics.total}
                        </td>
                        <td className={styles.deployCell}>
                          <div className={styles.deployContent}>
                            <span>
                              {project.lastDeployedAt
                                ? formatRelative(
                                    project.lastDeployedAt,
                                    tCommon
                                  )
                                : '—'}
                            </span>
                            <span className={styles.clickIndicator}>→</span>
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              ) : (
                <p className={styles.emptyState}>{t('noProjects')}</p>
              )}
            </CardContent>
          </Card>
        </div>

        {/* Right Column (Sidebar) */}
        <div className={styles.rightColumn}>
          <Card>
            <CardHeader>
              <h2 className={styles.sectionTitle}>{t('quickactions')}</h2>
            </CardHeader>
            <CardContent className={styles.quickActionsContent}>
              <div className={styles.quickActionsGrid}>
                <Button
                  variant="ghost"
                  size="md"
                  icon={<Plus size={20} />}
                  className={styles.quickActionButton}
                  title="Create Service"
                  onClick={() => navigate('/services/create')}
                >
                  Create Service
                </Button>
                <Button
                  variant="ghost"
                  size="md"
                  icon={<Network size={20} />}
                  className={styles.quickActionButton}
                  title="Create Shared Network"
                >
                  Create Shared Network
                </Button>
                <Button
                  variant="ghost"
                  size="md"
                  icon={<HardDrive size={20} />}
                  className={styles.quickActionButton}
                  title="Perform Backup"
                >
                  Perform Backup
                </Button>
              </div>
            </CardContent>
          </Card>
          {/* Recent Events */}
          <Card>
            <CardHeader>
              <h2 className={styles.sectionTitle}>
                {t('recentEvents.sectionTitle')}
              </h2>
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
                        <EventIcon type={event.eventType} />
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
      </div>
    </div>
  )
}
