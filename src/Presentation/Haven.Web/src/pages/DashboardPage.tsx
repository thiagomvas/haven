import { Plus } from 'lucide-react';
import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';

import type { EnvironmentDashboardDto, ProjectDashboardDto } from '@/api/types';
import { DeploymentList } from '@/components/dashboard/DeploymentList';
import { HostMetricTile } from '@/components/dashboard/HostMetricTile';
import { IncidentList } from '@/components/dashboard/IncidentList';
import { ProjectStatusCard } from '@/components/dashboard/ProjectStatusCard';
import { SampleDataTag } from '@/components/dashboard/SampleDataTag';
import { PermissionGuard } from '@/components/PermissionGuard';
import { Banner } from '@/components/ui/Banner';
import { Button } from '@/components/ui/Button';
import { EventIcon } from '@/components/ui/EventIcon';
import { FilterPillGroup } from '@/components/ui/FilterPillGroup';
import { Panel } from '@/components/ui/Panel';
import { Spinner } from '@/components/ui/Spinner';
import { StatGrid } from '@/components/ui/StatGrid';
import { StatusMark } from '@/components/ui/StatusMark';
import {
  useDashboardAttention,
  useDashboardOverview,
  useRecentDeployments,
} from '@/hooks/useDashboard';
import { useDashboardInsights } from '@/hooks/useDashboardInsights';
import { useEvents } from '@/hooks/useEvents';
import { usePermission } from '@/hooks/usePermission';
import { useProjectsDashboard } from '@/hooks/useProjects';
import { useSetBreadcrumbs } from '@/hooks/useSetBreadcrumbs';
import { formatDuration, formatRelative } from '@/lib/utils';
import styles from '@/styles/pages/DashboardPage.module.css';

type EnvFilter = 'all' | 'attention';

/** Host resource usage is built but hidden until the host metrics endpoint exists. */
const SHOW_HOST_METRICS = false;

interface ProjectGroup {
  project: ProjectDashboardDto;
  environments: EnvironmentDashboardDto[];
}

function needsAttention(env: EnvironmentDashboardDto): boolean {
  const s = env.serviceStatistics;
  return s.degraded > 0 || (s.total > 0 && s.running === 0);
}

/** Worst projects first, so a fault is never below the fold; ties alphabetical. */
function groupProjects(projects: ProjectDashboardDto[], filter: EnvFilter): ProjectGroup[] {
  return [...projects]
    .sort(
      (a, b) =>
        b.serviceStatistics.degraded - a.serviceStatistics.degraded || a.name.localeCompare(b.name)
    )
    .map(project => ({
      project,
      environments: [...project.environments]
        .filter(env => filter === 'all' || needsAttention(env))
        .sort(
          (a, b) =>
            b.serviceStatistics.degraded - a.serviceStatistics.degraded ||
            a.name.localeCompare(b.name)
        ),
    }))
    .filter(g => filter === 'all' || g.environments.length > 0);
}

export function DashboardPage() {
  const { t } = useTranslation('dashboard');
  const { t: tCommon } = useTranslation('common');
  const navigate = useNavigate();
  const [filter, setFilter] = useState<EnvFilter>('all');

  const { data: projectsData, isLoading: projectsLoading } = useProjectsDashboard({
    pageSize: 100,
  });
  const { data: eventsData, isLoading: eventsLoading } = useEvents({ pageSize: 4 });
  const { data: overviewData, isLoading: overviewLoading } = useDashboardOverview();
  const attentionQuery = useDashboardAttention(5);
  const deploymentsQuery = useRecentDeployments(4);
  const { data: insights } = useDashboardInsights();

  const canView = usePermission('projects.read');

  useSetBreadcrumbs([{ label: 'Dashboard' }]);

  const groups = useMemo(
    () => groupProjects(projectsData?.items ?? [], filter),
    [projectsData?.items, filter]
  );

  const stats = overviewData?.serviceStatistics;
  const attention = overviewData?.attentionEnvironment;
  const markLabel = (state: Parameters<typeof StatusMark>[0]['state'], label: string) => (
    <span className={styles.readoutLabel}>
      <StatusMark state={state} size={11} />
      {label}
    </span>
  );

  const host = insights.host;

  return (
    <div className={styles.container}>
      <div className={styles.pageHead}>
        <h1 className={styles.pageTitle}>{t('title')}</h1>
        <div className={styles.pageActions}>
          <PermissionGuard permission="projects.create">
            <Button
              variant="ghost"
              size="sm"
              icon={<Plus size={16} />}
              onClick={() => navigate('/projects/create')}
            >
              {t('createProject')}
            </Button>
            <Button
              variant="primary"
              size="sm"
              icon={<Plus size={16} />}
              onClick={() => navigate('/services/create')}
            >
              {t('createService')}
            </Button>
          </PermissionGuard>
        </div>
      </div>

      {canView && (
        <>
          <section className={styles.status} aria-label={t('overview.sectionTitle')}>
            {overviewLoading ? (
              <div className={styles.loading}>
                <Spinner size="md" />
              </div>
            ) : attention ? (
              <Banner
                variant="warning"
                title={`${attention.projectName} / ${attention.environmentName}`}
                description={t('overview.attentionAffected', {
                  count: attention.affectedServiceCount,
                })}
                onClick={() =>
                  navigate(
                    `/projects/${attention.projectId}/environments/${attention.environmentId}`
                  )
                }
              />
            ) : (
              <Banner variant="success" description={t('overview.allHealthy')} />
            )}
          </section>

          <div className={styles.mainGrid}>
            <div className={styles.column}>
              <StatGrid
                className={styles.readout}
                items={[
                  {
                    label: markLabel('running', tCommon('health.running')),
                    value: stats?.running ?? 0,
                  },
                  {
                    label: markLabel('degraded', tCommon('health.degraded')),
                    value: stats?.degraded ?? 0,
                    color: stats?.degraded ? 'var(--color-warning)' : undefined,
                  },
                  {
                    label: markLabel('stopped', tCommon('health.stopped')),
                    value: stats?.stopped ?? 0,
                  },
                  {
                    label: markLabel('deploying', tCommon('health.deploying')),
                    value: stats?.deploying ?? 0,
                  },
                  {
                    label: markLabel('pending', tCommon('health.deploymentPending')),
                    value: stats?.deploymentPending ?? 0,
                  },
                  {
                    label: t('overview.deploysLast24h'),
                    value: overviewData?.deploymentsLast24h ?? 0,
                  },
                ]}
              />
              <Panel flush title={t('incidents.title')}>
                {attentionQuery.isLoading ? (
                  <div className={styles.loading}>
                    <Spinner size="md" />
                  </div>
                ) : attentionQuery.isError ? (
                  <p className={styles.empty}>{t('incidents.loadError')}</p>
                ) : (
                  <IncidentList services={attentionQuery.data ?? []} />
                )}
              </Panel>

              <div className={styles.projectsHead}>
                <h2 className={styles.sectionTitle}>{t('projects.title')}</h2>
                <span className={styles.sectionMeta}>
                  {t('environments.scope', {
                    projects: overviewData?.totalProjects ?? 0,
                    environments: overviewData?.totalEnvironments ?? 0,
                  })}
                </span>
                <div className={styles.projectsFilter}>
                  <FilterPillGroup
                    value={filter}
                    onChange={v => setFilter(v as EnvFilter)}
                    options={[
                      { value: 'all', label: t('environments.all') },
                      { value: 'attention', label: t('environments.needsAttention') },
                    ]}
                  />
                </div>
              </div>

              {projectsLoading ? (
                <div className={styles.loading}>
                  <Spinner size="md" />
                </div>
              ) : groups.length > 0 ? (
                groups.map(g => (
                  <ProjectStatusCard
                    key={g.project.id}
                    project={g.project}
                    environments={g.environments}
                  />
                ))
              ) : (
                <p className={styles.empty}>
                  {filter === 'attention' ? t('environments.empty') : t('noProjects')}
                </p>
              )}
            </div>

            <div className={styles.column}>
              {SHOW_HOST_METRICS && (
                <Panel
                  flush
                  title={t('host.title')}
                  meta={
                    <>
                      {host.hostname} <SampleDataTag />
                    </>
                  }
                >
                  <div className={styles.hostGrid}>
                    {host.metrics.map(m => (
                      <HostMetricTile key={m.key} metric={m} />
                    ))}
                  </div>
                  <p className={styles.hostFooter}>
                    {t('host.footer', {
                      cores: host.cores,
                      uptime: formatDuration(host.uptimeSeconds),
                      running: host.runningContainers,
                      total: host.totalContainers,
                    })}
                  </p>
                </Panel>
              )}
              <Panel flush title={t('deployments.title')}>
                {deploymentsQuery.isLoading ? (
                  <div className={styles.loading}>
                    <Spinner size="md" />
                  </div>
                ) : deploymentsQuery.isError ? (
                  <p className={styles.empty}>{t('deployments.loadError')}</p>
                ) : (
                  <DeploymentList deployments={deploymentsQuery.data ?? []} />
                )}
              </Panel>

              <Panel flush title={t('recentEvents.sectionTitle')}>
                {eventsLoading ? (
                  <div className={styles.loading}>
                    <Spinner size="md" />
                  </div>
                ) : eventsData?.items?.length ? (
                  <ul className={styles.events}>
                    {eventsData.items.map(event => (
                      <li key={event.id} className={styles.event}>
                        <EventIcon type={event.eventType} />
                        <span className={styles.eventMessage}>{event.message}</span>
                        <time className={styles.eventTime} dateTime={event.triggeredAt}>
                          {formatRelative(event.triggeredAt, tCommon)}
                        </time>
                      </li>
                    ))}
                  </ul>
                ) : (
                  <p className={styles.empty}>{t('recentEvents.empty')}</p>
                )}
              </Panel>
            </div>
          </div>
        </>
      )}
    </div>
  );
}
