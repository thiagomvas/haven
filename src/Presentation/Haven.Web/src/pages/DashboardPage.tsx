import { AlertTriangle, HardDrive, Network, Plus, Rocket } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';

import type { ProjectDashboardDto } from '@/api/types';
import { Row, Spacer } from '@/components/layout';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/layout/Table';
import { PermissionGuard } from '@/components/PermissionGuard';
import { Badge } from '@/components/ui/Badge';
import { Banner } from '@/components/ui/Banner';
import { Button } from '@/components/ui/Button';
import { Card, CardContent, CardHeader } from '@/components/ui/Card';
import type { EnvironmentStatus } from '@/components/ui/EnvironmentStatusChip';
import { EnvironmentStatusChip } from '@/components/ui/EnvironmentStatusChip';
import { EventIcon } from '@/components/ui/EventIcon';
import { ProjectAvatar } from '@/components/ui/ProjectAvatar';
import { Spinner } from '@/components/ui/Spinner';
import { useDashboardOverview } from '@/hooks/useDashboard';
import { useEvents } from '@/hooks/useEvents';
import { usePermission } from '@/hooks/usePermission';
import { useProjectsDashboard } from '@/hooks/useProjects';
import { useSetBreadcrumbs } from '@/hooks/useSetBreadcrumbs';
import { formatRelative, getStatusColor } from '@/lib/utils';
import styles from '@/styles/pages/DashboardPage.module.css';

function getEnvironmentStatus(project: ProjectDashboardDto, envId: string): EnvironmentStatus {
  const env = project.environments.find(e => e.id === envId);
  if (!env) return 'empty';
  if (env.serviceStatistics.degraded > 0) return 'unhealthy';
  if (env.serviceStatistics.running === 0) return 'stopped';
  if (env.serviceStatistics.running === env.serviceStatistics.total) return 'running';
  return 'partial';
}

function getProjectServiceStatus(project: ProjectDashboardDto): EnvironmentStatus {
  if (project.serviceStatistics.total === 0) return 'empty';
  if (project.serviceStatistics.running === 0) return 'stopped';
  if (project.serviceStatistics.running === project.serviceStatistics.total) return 'running';
  return 'partial';
}

export function DashboardPage() {
  const { t } = useTranslation('dashboard');
  const { t: tCommon } = useTranslation('common');
  const navigate = useNavigate();
  const { data: projectsData, isLoading: projectsLoading } = useProjectsDashboard({
    pageSize: 100,
  });
  const { data: eventsData, isLoading: eventsLoading } = useEvents({
    pageSize: 5,
  });
  const { data: overviewData, isLoading: overviewLoading } = useDashboardOverview();

  const canViewProjects = usePermission('projects.read');
  const canViewEvents = usePermission('projects.read');
  const canCreateProject = usePermission('projects.create');
  const canCreateService = usePermission('projects.create');

  useSetBreadcrumbs([{ label: 'Dashboard' }]);

  const handleRowClick = (projectId: string) => {
    navigate(`/projects/${projectId}`);
  };

  return (
    <div className={styles.container}>
      <div className={styles.mainGrid}>
        {/* Left Column */}
        <div className={styles.leftColumn}>
          {canViewProjects && (
            <Card>
              <CardHeader>
                <h2 className={styles.sectionTitle}>{t('overview.sectionTitle')}</h2>
              </CardHeader>
              <CardContent className={styles.overviewContent}>
                {overviewLoading ? (
                  <div className={styles.loadingContainer}>
                    <Spinner size="lg" />
                  </div>
                ) : (
                  <>
                    <div className={styles.overviewStatsRow}>
                      <div className={styles.overviewStat}>
                        <div className={styles.statValue}>{overviewData?.totalProjects ?? 0}</div>
                        <div className={styles.statLabel}>{t('stats.totalProjects')}</div>
                      </div>
                      <div className={styles.overviewStat}>
                        <div className={styles.statValue}>
                          {overviewData?.totalEnvironments ?? 0}
                        </div>
                        <div className={styles.statLabel}>
                          <Network size={12} /> {t('stats.totalEnvironments')}
                        </div>
                      </div>
                    </div>

                    <div className={styles.overviewSection}>
                      <div className={styles.overviewSectionLabel}>
                        <HardDrive size={14} /> {t('overview.serviceStatus')}
                      </div>
                      <div className={styles.serviceStatusBadges}>
                        <Badge variant="success">
                          {tCommon('health.running')} {overviewData?.serviceStatistics.running ?? 0}
                        </Badge>
                        {!!overviewData?.serviceStatistics.degraded && (
                          <Badge variant="warning">
                            {tCommon('health.degraded')} {overviewData.serviceStatistics.degraded}
                          </Badge>
                        )}
                        {!!overviewData?.serviceStatistics.stopped && (
                          <Badge variant="danger">
                            {tCommon('health.stopped')} {overviewData.serviceStatistics.stopped}
                          </Badge>
                        )}
                        {!!overviewData?.serviceStatistics.deploying && (
                          <Badge variant="default">
                            {tCommon('health.deploying')} {overviewData.serviceStatistics.deploying}
                          </Badge>
                        )}
                      </div>
                    </div>

                    {overviewData?.attentionEnvironment ? (
                      <div className={styles.attentionBanner}>
                        <AlertTriangle size={16} />
                        <span>
                          <strong>{overviewData.attentionEnvironment.projectName}</strong> /{' '}
                          {overviewData.attentionEnvironment.environmentName}:{' '}
                          {t('overview.attentionAffected', {
                            count: overviewData.attentionEnvironment.affectedServiceCount,
                          })}
                        </span>
                      </div>
                    ) : (
                      <div className={styles.allHealthyBanner}>{t('overview.allHealthy')}</div>
                    )}

                    <Row className={styles.overviewFooterRow}>
                      <span>
                        <Rocket size={12} /> {t('overview.deploysLast24h')}:{' '}
                        {overviewData?.deploymentsLast24h ?? 0}
                      </span>
                      <Spacer direction="horizontal" expand />
                      <span>
                        {t('overview.lastDeploy')}:{' '}
                        {overviewData?.lastDeployment
                          ? formatRelative(overviewData.lastDeployment.deployedAt, tCommon)
                          : t('overview.noDeploys')}
                      </span>
                    </Row>
                  </>
                )}
              </CardContent>
            </Card>
          )}
          {canViewProjects && (
            <Card>
              <CardHeader>
                <Row>
                  <h2 className={styles.sectionTitle}>{tCommon('labels.projects')}</h2>
                  <Spacer direction="horizontal" expand />
                  <PermissionGuard permission="projects.create">
                    <Button
                      variant="outline"
                      size="sm"
                      align="center"
                      icon={<Plus size={16} />}
                      title={tCommon('actions.create')}
                      onClick={() => navigate('/projects/create')}
                    >
                      {tCommon('actions.create')}
                    </Button>
                  </PermissionGuard>
                </Row>
              </CardHeader>
              <CardContent className={styles.tableContent}>
                {projectsLoading ? (
                  <div className={styles.loadingContainer}>
                    <Spinner size="md" />
                  </div>
                ) : projectsData?.items?.length ? (
                  <Table className={styles.projectsTable}>
                    <TableHead>
                      <TableRow isHeader>
                        <TableHeader>{tCommon('labels.project')}</TableHeader>
                        <TableHeader>{tCommon('labels.environments')}</TableHeader>
                        <TableHeader>{tCommon('labels.services')}</TableHeader>
                        <TableHeader>{t('lastDeploy')}</TableHeader>
                      </TableRow>
                    </TableHead>
                    <TableBody>
                      {projectsData.items.map(project => (
                        <TableRow
                          key={project.id}
                          className={styles.tableRow}
                          onRowClick={() => handleRowClick(project.id)}
                        >
                          <TableCell className={styles.projectCell}>
                            <ProjectAvatar
                              name={project.name}
                              description={project.description}
                              showText={true}
                            />
                          </TableCell>
                          <TableCell className={styles.environmentsCell}>
                            <div className={styles.environmentsList}>
                              {project.environments.map(env => (
                                <EnvironmentStatusChip
                                  key={env.id}
                                  name={env.name}
                                  status={getEnvironmentStatus(project, env.id)}
                                />
                              ))}
                            </div>
                          </TableCell>
                          <TableCell className={styles.servicesCell}>
                            <span className={styles[getProjectServiceStatus(project)]}>
                              {project.serviceStatistics.running}
                            </span>
                            /{project.serviceStatistics.total}
                          </TableCell>
                          <TableCell className={styles.deployCell}>
                            <div className={styles.deployContent}>
                              <span>
                                {project.lastDeployedAt
                                  ? formatRelative(project.lastDeployedAt, tCommon)
                                  : '—'}
                              </span>
                              <span className={styles.clickIndicator}>→</span>
                            </div>
                          </TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                ) : (
                  <p className={styles.emptyState}>{t('noProjects')}</p>
                )}
              </CardContent>
            </Card>
          )}
        </div>

        {/* Right Column (Sidebar) */}
        <div className={styles.rightColumn}>
          {(canCreateProject || canCreateService) && (
            <Card>
              <CardHeader>
                <h2 className={styles.sectionTitle}>{t('quickactions')}</h2>
              </CardHeader>
              <CardContent className={styles.quickActionsContent}>
                <div className={styles.quickActionsGrid}>
                  <PermissionGuard permission="projects.create">
                    <Button
                      variant="primary"
                      size="md"
                      align="left"
                      icon={<Plus size={20} />}
                      title={t('createService')}
                      onClick={() => navigate('/services/create')}
                    >
                      {t('createService')}
                    </Button>
                  </PermissionGuard>
                </div>
              </CardContent>
            </Card>
          )}
          {/* Recent Events */}
          {canViewEvents && (
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
                    {eventsData.items.map(event => (
                      <div key={event.id} className={styles.eventItem}>
                        <div className={styles.eventType}>
                          <EventIcon type={event.eventType} />
                        </div>
                        <div className={styles.eventMessage}>{event.message}</div>
                        <div className={styles.eventTime}>
                          {formatRelative(event.triggeredAt, tCommon)}
                        </div>
                      </div>
                    ))}
                  </div>
                ) : (
                  <p className={styles.emptyState}>{t('recentEvents.empty')}</p>
                )}
              </CardContent>
            </Card>
          )}
        </div>
      </div>
    </div>
  );
}
