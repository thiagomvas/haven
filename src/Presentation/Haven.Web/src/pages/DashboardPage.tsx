import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { Plus, Network, HardDrive } from 'lucide-react';
import { useProjectsDashboard } from '@/hooks/useProjects';
import { useEvents } from '@/hooks/useEvents';
import { useSetBreadcrumbs } from '@/hooks/useSetBreadcrumbs';
import { Card, CardContent, CardHeader } from '@/components/ui/Card';
import { Badge } from '@/components/ui/Badge';
import { Spinner } from '@/components/ui/Spinner';
import { Button } from '@/components/ui/Button';
import { formatRelative, getStatusColor } from '@/lib/utils';
import styles from './DashboardPage.module.css';
import { EventIcon } from '@/components/ui/EventIcon';
import { EnvironmentStatusChip } from '@/components/ui/EnvironmentStatusChip';
import { ProjectAvatar } from '@/components/ui/ProjectAvatar';
import {
  Table,
  TableHead,
  TableBody,
  TableRow,
  TableHeader,
  TableCell,
} from '@/components/layout/Table';
import type { ProjectDashboardDto } from '@/api/types';
import type { EnvironmentStatus } from '@/components/ui/EnvironmentStatusChip';
import { Row, Spacer } from '@/components/layout';
import { PermissionGuard } from '@/components/PermissionGuard';
import { usePermission } from '@/hooks/usePermission';

function getEnvironmentStatus(project: ProjectDashboardDto, envId: string): EnvironmentStatus {
  const env = project.environments.find(e => e.id === envId);
  if (!env) return 'empty';
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
              <CardContent className={styles.statCard}>
                <div className={styles.statLabel}>{t('stats.totalProjects')}</div>
                {projectsLoading ? (
                  <Spinner size="lg" />
                ) : (
                  <div className={styles.statValue}>{projectsData?.totalCount ?? 0}</div>
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
