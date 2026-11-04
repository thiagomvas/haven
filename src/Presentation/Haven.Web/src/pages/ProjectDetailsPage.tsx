import { Bell, Play, Plus, RotateCw, Settings, Square, Upload } from 'lucide-react';
import { type ReactNode, useCallback, useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useNavigate, useParams } from 'react-router-dom';

import type {
  AttentionServiceDto,
  EnvironmentDashboardDto,
  ProjectDashboardDto,
  ServiceDashboardDto,
  ServiceStatisticsDto,
} from '@/api/types';
import { DeploymentList } from '@/components/dashboard/DeploymentList';
import { IncidentList } from '@/components/dashboard/IncidentList';
import {
  ConfigurationPageLayout,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/layout';
import { ScopedNotificationsSection } from '@/components/notificationChannels/ScopedNotificationsSection';
import { PermissionGuard } from '@/components/PermissionGuard';
import { Banner } from '@/components/ui/Banner';
import { BuoyStrip } from '@/components/ui/BuoyStrip';
import { CodeSpan } from '@/components/ui/CodeSpan';
import { FilterPillGroup } from '@/components/ui/FilterPillGroup';
import { Panel } from '@/components/ui/Panel';
import { StatGrid } from '@/components/ui/StatGrid';
import { StatusMark, toMarkState } from '@/components/ui/StatusMark';
import { useRecentDeployments } from '@/hooks/useDashboard';
import { usePermission } from '@/hooks/usePermission';
import { useDeployService, useRestartService, useStopService } from '@/hooks/useServices';
import { useSetBreadcrumbs } from '@/hooks/useSetBreadcrumbs';
import { useUrlState } from '@/hooks/useUrlState';
import { formatRelative } from '@/lib/utils';
import styles from '@/styles/pages/ProjectDetailsPage.module.css';

import { projectsApi } from '../api/projects';
import { ImportEnvironmentModal } from '../components/environments/ImportEnvironmentModal';
import { EnvironmentVariablesEditor } from '../components/projects/EnvironmentVariablesEditor';
import { ProjectSettingsForm } from '../components/projects/ProjectSettingsForm';
import { SecretsSection } from '../components/secrets/SecretsSection';
import { Button } from '../components/ui/Button';
import { Spinner } from '../components/ui/Spinner';

type ServiceFilter = 'all' | 'attention';

const VARIABLE_PREVIEW = 6;
const DEPLOYMENT_PREVIEW = 5;

/** Mirrors the dashboard's attention rule: unhealthy or crashed. Intentionally stopped services don't count. */
const attentionCount = (s: ServiceStatisticsDto) => s.degraded + s.crashed;

const byAttentionThenName = (
  a: { name: string; serviceStatistics: ServiceStatisticsDto },
  b: { name: string; serviceStatistics: ServiceStatisticsDto }
) =>
  attentionCount(b.serviceStatistics) - attentionCount(a.serviceStatistics) ||
  a.name.localeCompare(b.name);

/** Lower is worse, so faults sort to the top. */
const STATUS_RANK: Record<string, number> = {
  Crashed: 0,
  Degraded: 0,
  Stopped: 1,
  Deploying: 2,
  Starting: 2,
  Queued: 2,
  Unknown: 3,
  NotDeployed: 4,
  Running: 5,
};

interface ServiceRow {
  service: ServiceDashboardDto;
  environment: EnvironmentDashboardDto;
}

function latestDeploy(env: EnvironmentDashboardDto): string | undefined {
  return env.services
    .map(s => s.lastDeployedAt)
    .filter((d): d is string => !!d)
    .sort()
    .pop();
}

export function ProjectDetailsPage() {
  const { projectId } = useParams<{ projectId: string }>();
  const navigate = useNavigate();
  const { t } = useTranslation(['projects', 'secrets', 'environments', 'dashboard', 'services']);
  const { t: tCommon } = useTranslation('common');

  const [project, setProject] = useState<ProjectDashboardDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isImportEnvModalOpen, setIsImportEnvModalOpen] = useState(false);
  const [serviceFilter, setServiceFilter] = useState<ServiceFilter>('all');
  const [tabParam, setTabParam] = useUrlState('tab', '');
  const isConfigOpen = tabParam !== '';
  const selectedMenuId = tabParam || 'settings';
  const setIsConfigOpen = (open: boolean) => setTabParam(open ? 'settings' : '');
  const setSelectedMenuId = (id: string) => setTabParam(id);
  const canUpdateProject = usePermission('projects.update');
  const canReadNotifications = usePermission('system.read_notifications');
  const canManageSecrets = usePermission('projects.manage_secrets');
  const canDeployService = usePermission('projects.manage_deploys');
  const deployService = useDeployService();
  const restartService = useRestartService();
  const stopService = useStopService();
  const [pendingAction, setPendingAction] = useState<string | null>(null);

  const deploymentsQuery = useRecentDeployments(50);

  useSetBreadcrumbs([{ label: 'Projects', to: '/projects' }, { label: project?.name ?? '…' }]);

  const refresh = useCallback(async () => {
    if (!projectId) return;
    try {
      const data = await projectsApi.getDashboardById(projectId);
      if (data) setProject(data);
    } catch (err) {
      console.error('Failed to refresh project dashboard', err);
    }
  }, [projectId]);

  useEffect(() => {
    const load = async () => {
      if (!projectId) return;
      try {
        setLoading(true);
        setError(null);
        const data = await projectsApi.getDashboardById(projectId);
        if (!data) {
          setError('Project not found');
          return;
        }
        setProject(data);
      } catch (err) {
        setError(err instanceof Error ? err.message : t('error'));
      } finally {
        setLoading(false);
      }
    };
    load();
  }, [projectId, t]);

  const runAction = async (
    action: 'deploy' | 'restart' | 'stop',
    { service, environment }: ServiceRow
  ) => {
    if (!projectId) return;
    const mutation = { deploy: deployService, restart: restartService, stop: stopService }[action];
    setPendingAction(`${service.id}:${action}`);
    try {
      await mutation.mutateAsync({
        projectId,
        environmentId: environment.id,
        serviceId: service.id,
      });
      await refresh();
    } catch (err) {
      console.error(`Failed to ${action} service`, err);
    } finally {
      setPendingAction(null);
    }
  };

  const environments = useMemo(
    () => [...(project?.environments ?? [])].sort(byAttentionThenName),
    [project?.environments]
  );

  const services = useMemo<ServiceRow[]>(
    () =>
      environments
        .flatMap(environment => environment.services.map(service => ({ service, environment })))
        .sort(
          (a, b) =>
            (STATUS_RANK[a.service.status] ?? 3) - (STATUS_RANK[b.service.status] ?? 3) ||
            a.environment.name.localeCompare(b.environment.name) ||
            a.service.name.localeCompare(b.service.name)
        ),
    [environments]
  );

  const incidents = useMemo<AttentionServiceDto[]>(
    () =>
      services
        .filter(({ service }) => service.status === 'Degraded' || service.status === 'Crashed')
        .map(({ service, environment }) => ({
          serviceId: service.id,
          serviceName: service.name,
          projectId: environment.projectId,
          projectName: environment.projectName,
          environmentId: environment.id,
          environmentName: environment.name,
          reason: service.status === 'Crashed' ? 'Crashed' : 'Unhealthy',
        })),
    [services]
  );

  const visibleServices =
    serviceFilter === 'attention'
      ? services.filter(
          ({ service }) => service.status === 'Degraded' || service.status === 'Crashed'
        )
      : services;

  const deployments = useMemo(
    () =>
      (deploymentsQuery.data ?? [])
        .filter(d => d.projectId === projectId)
        .slice(0, DEPLOYMENT_PREVIEW),
    [deploymentsQuery.data, projectId]
  );

  if (loading) {
    return (
      <div className={styles.loading}>
        <Spinner />
        <p>{t('loading')}</p>
      </div>
    );
  }

  if (error || !project) {
    return (
      <div className={styles.container}>
        <Banner variant="error" title={error || t('notFound')}>
          <Button variant="ghost" size="sm" onClick={() => navigate('/projects')}>
            {t('back')}
          </Button>
        </Banner>
      </div>
    );
  }

  const stats = project.serviceStatistics;
  const worstEnvironment = environments[0];
  const mark = (state: Parameters<typeof StatusMark>[0]['state'], label: string) => (
    <span className={styles.readoutLabel}>
      <StatusMark state={state} size={11} />
      {label}
    </span>
  );

  const header = (
    <div className={styles.pageHead}>
      <div className={styles.identity}>
        <div className={styles.titleRow}>
          <h1 className={styles.pageTitle}>{project.name}</h1>
          {project.alias && <CodeSpan>{project.alias}</CodeSpan>}
        </div>
        {project.description && <p className={styles.description}>{project.description}</p>}
      </div>
      <div className={styles.pageActions}>
        {isConfigOpen ? (
          <Button variant="ghost" size="sm" onClick={() => setIsConfigOpen(false)}>
            {t('closeSettings')}
          </Button>
        ) : (
          <>
            <PermissionGuard permission="environments.create">
              <Button
                variant="ghost"
                size="sm"
                icon={<Upload size={16} />}
                onClick={() => setIsImportEnvModalOpen(true)}
              >
                {t('environments:import.action')}
              </Button>
              <Button
                variant="ghost"
                size="sm"
                icon={<Plus size={16} />}
                onClick={() => navigate(`/environments/create?projectId=${projectId}`)}
              >
                {t('detail.addEnvironment')}
              </Button>
            </PermissionGuard>
            {canUpdateProject && (
              <Button
                variant="primary"
                size="sm"
                icon={<Settings size={16} />}
                onClick={() => setIsConfigOpen(true)}
              >
                {t('settings')}
              </Button>
            )}
          </>
        )}
      </div>
    </div>
  );

  const overview = (
    <div className={styles.overview}>
      {attentionCount(stats) > 0 && worstEnvironment ? (
        <Banner
          variant="warning"
          title={`${worstEnvironment.name}`}
          description={t('dashboard:overview.attentionAffected', { count: attentionCount(stats) })}
          onClick={() => navigate(`/projects/${project.id}/environments/${worstEnvironment.id}`)}
        />
      ) : stats.total === 0 ? (
        <Banner variant="info" description={t('detail.noServices')} />
      ) : (
        <Banner variant="success" description={t('detail.allHealthy')} />
      )}

      <StatGrid
        className={styles.readout}
        items={[
          {
            label: mark('running', tCommon('health.running')),
            value: stats.running - stats.degraded,
          },
          {
            label: mark('degraded', tCommon('health.degraded')),
            value: stats.degraded,
            color: stats.degraded ? 'var(--color-warning)' : undefined,
          },
          { label: mark('stopped', tCommon('health.stopped')), value: stats.stopped },
          { label: mark('deploying', tCommon('health.deploying')), value: stats.deploying },
          {
            label: mark('pending', tCommon('health.deploymentPending')),
            value: stats.deploymentPending,
          },
          { label: tCommon('labels.environments'), value: project.environments.length },
          {
            label: t('detail.lastDeploy'),
            value: project.lastDeployedAt
              ? formatRelative(project.lastDeployedAt, tCommon)
              : t('detail.never'),
          },
        ]}
      />
    </div>
  );

  const environmentsPanel = (
    <Panel flush title={tCommon('labels.environments')} meta={project.environments.length}>
      {environments.length > 0 ? (
        <Table>
          <TableHead>
            <TableRow isHeader>
              <TableHeader>{tCommon('labels.environment')}</TableHeader>
              <TableHeader>{tCommon('labels.services')}</TableHeader>
              <TableHeader align="right">{t('detail.up')}</TableHeader>
              <TableHeader align="right">{t('detail.lastDeploy')}</TableHeader>
            </TableRow>
          </TableHead>
          <TableBody>
            {environments.map(env => {
              const s = env.serviceStatistics;
              const tone =
                s.total === 0
                  ? 'none'
                  : attentionCount(s) > 0
                    ? 'warn'
                    : s.running === 0
                      ? 'off'
                      : 'ok';
              const deployed = latestDeploy(env);
              return (
                <TableRow
                  key={env.id}
                  onRowClick={() => navigate(`/projects/${project.id}/environments/${env.id}`)}
                >
                  <TableCell>
                    <Link
                      className={styles.rowLink}
                      to={`/projects/${project.id}/environments/${env.id}`}
                      onClick={e => e.stopPropagation()}
                    >
                      {env.name}
                    </Link>
                    <span className={styles.network}>{env.networkName}</span>
                  </TableCell>
                  <TableCell>
                    <BuoyStrip stats={s} />
                  </TableCell>
                  <TableCell align="right" nowrap>
                    <span className={`${styles.up} ${styles[`up-${tone}`]}`}>
                      {s.running}
                      <i>/{s.total}</i>
                    </span>
                  </TableCell>
                  <TableCell align="right" nowrap variant="muted">
                    {deployed ? formatRelative(deployed, tCommon) : '—'}
                  </TableCell>
                </TableRow>
              );
            })}
          </TableBody>
        </Table>
      ) : (
        <p className={styles.empty}>{t('detail.noEnvironments')}</p>
      )}
    </Panel>
  );

  const servicesPanel = (
    <Panel
      flush
      title={tCommon('labels.services')}
      meta={stats.total}
      actions={
        stats.total > 0 ? (
          <FilterPillGroup
            value={serviceFilter}
            onChange={v => setServiceFilter(v as ServiceFilter)}
            options={[
              { value: 'all', label: t('dashboard:environments.all') },
              { value: 'attention', label: t('dashboard:environments.needsAttention') },
            ]}
          />
        ) : undefined
      }
    >
      {visibleServices.length > 0 ? (
        <Table>
          <TableHead>
            <TableRow isHeader>
              <TableHeader>{tCommon('labels.service')}</TableHeader>
              <TableHeader>{tCommon('labels.environment')}</TableHeader>
              <TableHeader>{tCommon('labels.status')}</TableHeader>
              <TableHeader align="right">{t('detail.lastDeploy')}</TableHeader>
              {canDeployService && <TableHeader align="right" />}
            </TableRow>
          </TableHead>
          <TableBody>
            {visibleServices.map(row => {
              const { service, environment } = row;
              // Intent, not just observed state: a crashing or restarting service must still be stoppable.
              const canControl =
                service.desiredState === 'Running' ||
                ['Running', 'Restarting', 'Crashed'].includes(service.runtimeState);
              const busy = pendingAction !== null;
              const actionButton = (
                action: 'deploy' | 'restart' | 'stop',
                label: string,
                icon: ReactNode
              ) => (
                <Button
                  variant="ghost"
                  size="xs"
                  icon={icon}
                  title={label}
                  aria-label={`${label} ${service.name}`}
                  disabled={busy}
                  isLoading={pendingAction === `${service.id}:${action}`}
                  onClick={() => runAction(action, row)}
                />
              );
              return (
                <TableRow
                  key={service.id}
                  onRowClick={() =>
                    navigate(
                      `/projects/${project.id}/environments/${environment.id}/services/${service.id}`
                    )
                  }
                >
                  <TableCell>
                    <Link
                      className={styles.rowLink}
                      to={`/projects/${project.id}/environments/${environment.id}/services/${service.id}`}
                      onClick={e => e.stopPropagation()}
                    >
                      {service.name}
                    </Link>
                  </TableCell>
                  <TableCell variant="muted">{environment.name}</TableCell>
                  <TableCell nowrap>
                    <span className={styles.state}>
                      <StatusMark state={toMarkState(service.status)} size={12} />
                      {tCommon(`health.${service.status.toLowerCase()}`, {
                        defaultValue: service.status,
                      })}
                    </span>
                  </TableCell>
                  <TableCell align="right" nowrap variant="muted">
                    {service.lastDeployedAt ? formatRelative(service.lastDeployedAt, tCommon) : '—'}
                  </TableCell>
                  {canDeployService && (
                    <TableCell align="right" nowrap>
                      {/* Row click navigates; keep the controls from triggering it. */}
                      <span className={styles.actions} onClick={e => e.stopPropagation()}>
                        {canControl &&
                          actionButton('restart', t('services:restart'), <RotateCw size={14} />)}
                        {canControl &&
                          actionButton('stop', t('services:stop'), <Square size={14} />)}
                        {actionButton(
                          'deploy',
                          service.status === 'Running'
                            ? t('services:redeploy')
                            : t('services:deploy'),
                          <Play size={14} />
                        )}
                      </span>
                    </TableCell>
                  )}
                </TableRow>
              );
            })}
          </TableBody>
        </Table>
      ) : (
        <p className={styles.empty}>
          {stats.total === 0 ? t('detail.noServices') : t('dashboard:environments.empty')}
        </p>
      )}
    </Panel>
  );

  const variables = project.environmentVariables ?? [];
  const variablesPanel = (
    <Panel
      flush
      title={tCommon('labels.variables')}
      meta={project.totalEnvVars}
      actions={
        canUpdateProject && project.totalEnvVars > 0 ? (
          <Button variant="text" size="xs" onClick={() => setSelectedMenuId('variables')}>
            {tCommon('labels.viewAll')}
          </Button>
        ) : undefined
      }
    >
      {variables.length > 0 ? (
        <>
          <ul className={styles.variables}>
            {variables.slice(0, VARIABLE_PREVIEW).map(v => (
              <li key={v.key} className={styles.variable}>
                <code className={styles.varKey}>{v.key}</code>
                <code className={styles.varValue}>{v.value}</code>
              </li>
            ))}
          </ul>
          <p className={styles.note}>{t('environmentVariableNotice')}</p>
        </>
      ) : (
        <p className={styles.empty}>{t('noVariables')}</p>
      )}
    </Panel>
  );

  const content = (
    <div className={styles.container}>
      {overview}
      <div className={styles.mainGrid}>
        <div className={styles.column}>
          {environmentsPanel}
          {servicesPanel}
        </div>
        <div className={styles.column}>
          {incidents.length > 0 && (
            <Panel flush title={t('dashboard:incidents.title')}>
              <IncidentList services={incidents} />
            </Panel>
          )}
          <Panel flush title={t('dashboard:deployments.title')}>
            {deploymentsQuery.isLoading ? (
              <div className={styles.loadingInline}>
                <Spinner size="md" />
              </div>
            ) : (
              <DeploymentList deployments={deployments} />
            )}
          </Panel>
          {variablesPanel}
        </div>
      </div>
    </div>
  );

  const menuItems = [
    ...(canUpdateProject
      ? [
          {
            id: 'settings',
            label: t('settings'),
            content: (
              <ProjectSettingsForm
                project={{
                  ...project,
                  environmentCount: project.environments.length,
                  serviceCount: project.serviceStatistics.total,
                }}
                onSuccess={refresh}
              />
            ),
          },
          {
            id: 'variables',
            label: t('variables'),
            content: projectId ? (
              <EnvironmentVariablesEditor projectId={projectId} projectName={project.name} />
            ) : null,
          },
        ]
      : []),
    ...(canManageSecrets && projectId
      ? [
          {
            id: 'secrets',
            label: t('secrets:title'),
            content: <SecretsSection parentType="Project" projectId={projectId} />,
          },
        ]
      : []),
    ...(canReadNotifications && projectId
      ? [
          {
            id: 'notifications',
            label: t('notifications'),
            icon: <Bell size={16} />,
            content: <ScopedNotificationsSection ctx={{ scope: 'Project', scopeId: projectId }} />,
          },
        ]
      : []),
  ];

  return (
    <>
      <ConfigurationPageLayout
        mainHeader={header}
        configHeader={header}
        menuItems={menuItems}
        isConfigOpen={isConfigOpen}
        onConfigOpenChange={setIsConfigOpen}
        selectedMenuId={selectedMenuId}
        onSelectedMenuIdChange={setSelectedMenuId}
        hideConfigButton={true}
        hideCloseButton={true}
      >
        {content}
      </ConfigurationPageLayout>

      {projectId && isImportEnvModalOpen && (
        <ImportEnvironmentModal
          projectId={projectId}
          onClose={() => setIsImportEnvModalOpen(false)}
          onSuccess={refresh}
        />
      )}
    </>
  );
}
