import {
  Bell,
  CheckSquare,
  Download,
  Play,
  Plus,
  Rocket,
  RotateCw,
  Settings,
  Square,
} from 'lucide-react';
import { type ReactNode, useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useNavigate, useParams } from 'react-router-dom';

import type { AttentionServiceDto, DockerConfig, ServiceDashboardDto } from '@/api/types';
import { BulkServiceActionResponse } from '@/api/types';
import { EnvironmentDashboardDto } from '@/api/types';
import { ProjectDto } from '@/api/types';
import { ServiceDto } from '@/api/types';
import { ServiceDisplayStatus } from '@/api/types';
import { DeploymentList } from '@/components/dashboard/DeploymentList';
import { IncidentList } from '@/components/dashboard/IncidentList';
import {
  BulkAction,
  BulkActionConfirmModal,
  BulkActionResultBanner,
  BulkActionToolbar,
} from '@/components/environments';
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
import { Banner } from '@/components/ui/Banner';
import { CodeSpan } from '@/components/ui/CodeSpan';
import { FilterPillGroup } from '@/components/ui/FilterPillGroup';
import { Panel } from '@/components/ui/Panel';
import { StatGrid } from '@/components/ui/StatGrid';
import { StatusMark, toMarkState } from '@/components/ui/StatusMark';
import { useRecentDeployments } from '@/hooks/useDashboard';
import { usePermission } from '@/hooks/usePermission';
import {
  useBulkDeployServices,
  useBulkRestartServices,
  useBulkStopServices,
  useDeployService,
  useRestartService,
  useStopService,
} from '@/hooks/useServices';
import { useSetBreadcrumbs } from '@/hooks/useSetBreadcrumbs';
import { useUrlState } from '@/hooks/useUrlState';
import { formatRelative } from '@/lib/utils';
import styles from '@/styles/pages/EnvironmentDetailsPage.module.css';

import { environmentsApi } from '../api/environments';
import { projectsApi } from '../api/projects';
import { servicesApi } from '../api/services';
import { EnvironmentSettingsForm } from '../components/environments/EnvironmentSettingsForm';
import { EnvironmentVariablesEditor } from '../components/environments/EnvironmentVariablesEditor';
import { ExportEnvironmentModal } from '../components/environments/ExportEnvironmentModal';
import { ServiceCard } from '../components/projects/ServiceCard';
import { SecretsSection } from '../components/secrets/SecretsSection';
import { Button } from '../components/ui/Button';
import { Spinner } from '../components/ui/Spinner';
import { serviceStatusHub } from '../lib/signalr/hubs';
import { useSubscribeToMultipleServices } from '../lib/signalr/useSubscribeToMultipleServices';

type ServiceFilter = 'all' | 'attention';

const DEPLOYMENT_PREVIEW = 5;
const VARIABLE_PREVIEW = 6;

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

const needsAttention = (status: ServiceDisplayStatus) =>
  status === 'Degraded' || status === 'Crashed';

export function EnvironmentDetailsPage() {
  const { projectId, environmentId } = useParams<{
    projectId: string;
    environmentId: string;
  }>();
  const navigate = useNavigate();
  const { t } = useTranslation(['projects', 'environments', 'common', 'secrets', 'services']);

  const [project, setProject] = useState<ProjectDto | null>(null);
  const [environment, setEnvironment] = useState<EnvironmentDashboardDto | null>(null);
  const [services, setServices] = useState<ServiceDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [tabParam, setTabParam] = useUrlState('tab', '');
  const isConfigOpen = tabParam !== '';
  const selectedMenuId = tabParam || 'configuration';
  const setIsConfigOpen = (open: boolean) => setTabParam(open ? 'configuration' : '');
  const setSelectedMenuId = (id: string) => setTabParam(id);
  const canCreateService = usePermission('projects.create');
  const canUpdateEnvironment = usePermission('projects.create');
  const canDeployService = usePermission('projects.manage_deploys');
  const canReadNotifications = usePermission('system.read_notifications');
  const canManageSecrets = usePermission('projects.manage_secrets');
  // Matches ExportEnvironmentToDockerComposeCommand's required permissions on the backend.
  const canReadProjectsForExport = usePermission('projects.read');
  const canManageConfigForExport = usePermission('projects.manage_config');
  const canExportEnvironment = canReadProjectsForExport && canManageConfigForExport;

  const [isExportModalOpen, setIsExportModalOpen] = useState(false);
  const [selectionMode, setSelectionMode] = useState(false);
  const [selectedServiceIds, setSelectedServiceIds] = useState<Set<string>>(new Set());
  const [pendingBulkAction, setPendingBulkAction] = useState<BulkAction | null>(null);
  const [bulkResult, setBulkResult] = useState<BulkServiceActionResponse | null>(null);
  const [bulkError, setBulkError] = useState<string | undefined>(undefined);
  const [serviceFilter, setServiceFilter] = useState<ServiceFilter>('all');
  const [pendingAction, setPendingAction] = useState<string | null>(null);
  const { t: tCommon } = useTranslation('common');
  const { t: tDash } = useTranslation('dashboard');
  const deployService = useDeployService();
  const restartService = useRestartService();
  const stopService = useStopService();
  const deploymentsQuery = useRecentDeployments(50);

  const bulkDeployMutation = useBulkDeployServices();
  const bulkRestartMutation = useBulkRestartServices();
  const bulkStopMutation = useBulkStopServices();
  const isBulkSubmitting =
    bulkDeployMutation.isPending || bulkRestartMutation.isPending || bulkStopMutation.isPending;

  const handleAddService = () => {
    navigate(`/services/create?projectId=${projectId}&environmentId=${environmentId}`);
  };

  const handleToggleSelectionMode = () => {
    setSelectionMode(mode => !mode);
    setSelectedServiceIds(new Set());
  };

  const handleToggleServiceSelected = (serviceId: string) => {
    setSelectedServiceIds(prev => {
      const next = new Set(prev);
      if (next.has(serviceId)) {
        next.delete(serviceId);
      } else {
        next.add(serviceId);
      }
      return next;
    });
  };

  const handleDeployAllClick = () => {
    setSelectedServiceIds(new Set(services.map(s => s.id)));
    setPendingBulkAction('deploy');
  };

  const handleToggleSelectAll = () => {
    setSelectedServiceIds(prev =>
      prev.size === services.length ? new Set() : new Set(services.map(s => s.id))
    );
  };

  const handleConfirmBulkAction = async () => {
    if (!pendingBulkAction || !projectId || !environmentId) return;
    setBulkError(undefined);

    const serviceIds = Array.from(selectedServiceIds);
    const mutation =
      pendingBulkAction === 'deploy'
        ? bulkDeployMutation
        : pendingBulkAction === 'restart'
          ? bulkRestartMutation
          : bulkStopMutation;

    try {
      const result = await mutation.mutateAsync({ projectId, environmentId, serviceIds });
      setBulkResult(result);
      setPendingBulkAction(null);
      setSelectionMode(false);
      setSelectedServiceIds(new Set());

      const refreshedServices = await servicesApi.getByEnvironmentId(projectId, environmentId);
      setServices(refreshedServices || []);
    } catch (err) {
      setBulkError(err instanceof Error ? err.message : t('error'));
    }
  };

  useSetBreadcrumbs([
    { label: 'Projects', to: '/projects' },
    {
      label: project?.name ?? '…',
      to: projectId ? `/projects/${projectId}` : undefined,
    },
    { label: environment?.name ?? '…' },
  ]);

  useEffect(() => {
    const loadData = async () => {
      if (!projectId || !environmentId) return;

      try {
        setLoading(true);
        setError(null);

        const [projectData, environmentData, servicesData] = await Promise.all([
          projectsApi.getById(projectId),
          environmentsApi.getDashboard(projectId, environmentId),
          servicesApi.getByEnvironmentId(projectId, environmentId),
        ]);

        if (!projectData) {
          setError('Project not found');
          return;
        }

        if (!environmentData) {
          setError('Environment not found');
          return;
        }

        setProject(projectData);
        setEnvironment(environmentData);
        setServices(servicesData || []);
      } catch (err) {
        setError(err instanceof Error ? err.message : t('error'));
      } finally {
        setLoading(false);
      }
    };

    loadData();
  }, [projectId, environmentId, t]);

  useSubscribeToMultipleServices(
    serviceStatusHub,
    services.map(s => s.id),
    data => {
      setServices(prevServices =>
        prevServices.map(service =>
          service.id === data.serviceId
            ? { ...service, status: data.newStatus as ServiceDisplayStatus }
            : service
        )
      );
    }
  );

  const handleEnvironmentUpdated = async () => {
    if (!projectId || !environmentId) return;
    try {
      const environmentData = await environmentsApi.getDashboard(projectId, environmentId);
      if (environmentData) {
        setEnvironment(environmentData);
      }
    } catch (err) {
      console.error('Failed to refresh environment', err);
    }
  };

  const deployedAtById = useMemo(
    () =>
      new Map<string, string | undefined>(
        (environment?.services ?? []).map((s: ServiceDashboardDto) => [s.id, s.lastDeployedAt])
      ),
    [environment?.services]
  );

  const sortedServices = useMemo(
    () =>
      [...services].sort(
        (a, b) =>
          (STATUS_RANK[a.status] ?? 3) - (STATUS_RANK[b.status] ?? 3) ||
          a.name.localeCompare(b.name)
      ),
    [services]
  );

  const visibleServices =
    serviceFilter === 'attention'
      ? sortedServices.filter(s => needsAttention(s.status))
      : sortedServices;

  const incidents = useMemo<AttentionServiceDto[]>(
    () =>
      sortedServices
        .filter(s => needsAttention(s.status))
        .map(s => ({
          serviceId: s.id,
          serviceName: s.name,
          projectId: projectId ?? '',
          projectName: project?.name ?? '',
          environmentId: environmentId ?? '',
          environmentName: environment?.name ?? '',
          reason: s.status === 'Crashed' ? 'Crashed' : 'Unhealthy',
        })),
    [sortedServices, projectId, project?.name, environmentId, environment?.name]
  );

  const deployments = useMemo(
    () =>
      (deploymentsQuery.data ?? [])
        .filter(d => d.environmentId === environmentId)
        .slice(0, DEPLOYMENT_PREVIEW),
    [deploymentsQuery.data, environmentId]
  );

  const runAction = async (action: 'deploy' | 'restart' | 'stop', service: ServiceDto) => {
    if (!projectId || !environmentId) return;
    const mutation = { deploy: deployService, restart: restartService, stop: stopService }[action];
    setPendingAction(`${service.id}:${action}`);
    try {
      await mutation.mutateAsync({ projectId, environmentId, serviceId: service.id });
      setServices((await servicesApi.getByEnvironmentId(projectId, environmentId)) || []);
    } catch (err) {
      console.error(`Failed to ${action} service`, err);
    } finally {
      setPendingAction(null);
    }
  };

  if (loading) {
    return (
      <div className={styles.loading}>
        <Spinner />
        <p>{t('projects:loading')}</p>
      </div>
    );
  }

  if (error || !project || !environment) {
    return (
      <div className={styles.container}>
        <Banner variant="error" title={error || t('projects:notFound')}>
          <Button variant="ghost" size="sm" onClick={() => navigate(`/projects/${projectId}`)}>
            {t('projects:back')}
          </Button>
        </Banner>
      </div>
    );
  }

  const count = (...statuses: ServiceDisplayStatus[]) =>
    services.filter(s => statuses.includes(s.status)).length;
  const degraded = count('Degraded');
  const crashed = count('Crashed');
  const attention = degraded + crashed;
  const running = count('Running');
  const lastDeploy = [...deployedAtById.values()]
    .filter((d): d is string => !!d)
    .sort()
    .pop();
  const serviceUrl = (id: string) =>
    `/projects/${projectId}/environments/${environmentId}/services/${id}`;
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
          <h1 className={styles.pageTitle}>{environment.name}</h1>
          <Link className={styles.parent} to={`/projects/${projectId}`}>
            {t('common:nouns.in')} {project.name}
          </Link>
          {environment.alias && <CodeSpan>{environment.alias}</CodeSpan>}
        </div>
        {environment.description && <p className={styles.description}>{environment.description}</p>}
        <div className={styles.meta}>
          <span className={styles.metaLabel}>{t('environments:network')}</span>
          <CodeSpan copyable>{environment.networkName}</CodeSpan>
        </div>
      </div>
      <div className={styles.pageActions}>
        {canExportEnvironment && (
          <Button
            variant="ghost"
            size="sm"
            icon={<Download size={16} />}
            onClick={() => setIsExportModalOpen(true)}
          >
            {t('environments:export.button')}
          </Button>
        )}
        {canUpdateEnvironment && (
          <Button
            variant={isConfigOpen ? 'ghost' : 'primary'}
            size="sm"
            icon={isConfigOpen ? undefined : <Settings size={16} />}
            onClick={() => setIsConfigOpen(!isConfigOpen)}
          >
            {isConfigOpen ? t('common:labels.closeSettings') : t('common:labels.settings')}
          </Button>
        )}
        {!isConfigOpen && canDeployService && (
          <Button
            variant="ghost"
            size="sm"
            icon={<Rocket size={16} />}
            onClick={handleDeployAllClick}
            disabled={services.length === 0 || isBulkSubmitting}
            isLoading={isBulkSubmitting && pendingBulkAction === 'deploy'}
          >
            {t('environments:deployAll')}
          </Button>
        )}
      </div>
    </div>
  );

  const overview = (
    <div className={styles.overview}>
      {attention > 0 ? (
        <Banner
          variant="warning"
          title={tDash('overview.attention')}
          description={tDash('overview.attentionAffected', { count: attention })}
          onClick={() => setServiceFilter('attention')}
        />
      ) : services.length === 0 ? (
        <Banner variant="info" description={t('projects:detail.noServices')} />
      ) : (
        <Banner variant="success" description={t('projects:detail.allHealthy')} />
      )}
      <StatGrid
        className={styles.readout}
        items={[
          { label: mark('running', tCommon('health.running')), value: running },
          {
            label: mark('degraded', tCommon('health.degraded')),
            value: degraded,
            color: degraded ? 'var(--color-warning)' : undefined,
          },
          {
            label: mark('fault', tCommon('health.crashed')),
            value: crashed,
            color: crashed ? 'var(--color-error)' : undefined,
          },
          { label: mark('stopped', tCommon('health.stopped')), value: count('Stopped') },
          {
            label: mark('deploying', tCommon('health.deploying')),
            value: count('Deploying', 'Starting'),
          },
          { label: mark('pending', tCommon('health.deploymentPending')), value: count('Queued') },
          {
            label: t('projects:detail.lastDeploy'),
            value: lastDeploy ? formatRelative(lastDeploy, tCommon) : t('projects:detail.never'),
          },
        ]}
      />
    </div>
  );

  const servicesPanel = (
    <Panel
      flush
      title={t('environments:services')}
      meta={services.length}
      actions={
        <span className={styles.panelActions}>
          {services.length > 0 && (
            <FilterPillGroup
              value={serviceFilter}
              onChange={v => setServiceFilter(v as ServiceFilter)}
              options={[
                { value: 'all', label: tDash('environments.all') },
                { value: 'attention', label: tDash('environments.needsAttention') },
              ]}
            />
          )}
          {canCreateService && services.length > 0 && !selectionMode && (
            <Button
              variant="ghost"
              size="xs"
              icon={<CheckSquare size={14} />}
              onClick={handleToggleSelectionMode}
            >
              {t('common:actions.select')}
            </Button>
          )}
          {canCreateService && (
            <Button variant="ghost" size="xs" icon={<Plus size={14} />} onClick={handleAddService}>
              {t('common:actions.createService')}
            </Button>
          )}
        </span>
      }
    >
      {(selectionMode || bulkResult) && (
        <div className={styles.bulk}>
          {selectionMode && (
            <BulkActionToolbar
              selectedCount={selectedServiceIds.size}
              totalCount={services.length}
              allSelected={selectedServiceIds.size === services.length && services.length > 0}
              someSelected={selectedServiceIds.size > 0}
              onToggleSelectAll={handleToggleSelectAll}
              onRequestAction={action => setPendingBulkAction(action)}
              onExitSelectionMode={handleToggleSelectionMode}
            />
          )}
          {bulkResult && (
            <BulkActionResultBanner
              result={bulkResult}
              services={services}
              onDismiss={() => setBulkResult(null)}
            />
          )}
        </div>
      )}
      {visibleServices.length > 0 ? (
        <div className={styles.tableScroll}>
          <Table>
            <TableHead>
              <TableRow isHeader>
                {selectionMode && <TableHeader />}
                <TableHeader>{tCommon('labels.service')}</TableHeader>
                <TableHeader>{tCommon('labels.status')}</TableHeader>
                <TableHeader align="right">{t('projects:detail.lastDeploy')}</TableHeader>
                {canDeployService && !selectionMode && <TableHeader align="right" />}
              </TableRow>
            </TableHead>
            <TableBody>
              {visibleServices.map(service => {
                const image = (service.sourceConfig as DockerConfig | undefined)?.image;
                const deployed = deployedAtById.get(service.id);
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
                    onClick={() => runAction(action, service)}
                  />
                );
                return (
                  <TableRow
                    key={service.id}
                    onRowClick={() =>
                      selectionMode
                        ? handleToggleServiceSelected(service.id)
                        : navigate(serviceUrl(service.id))
                    }
                  >
                    {selectionMode && (
                      <TableCell nowrap>
                        <input
                          type="checkbox"
                          className={styles.check}
                          aria-label={service.name}
                          checked={selectedServiceIds.has(service.id)}
                          onClick={e => e.stopPropagation()}
                          onChange={() => handleToggleServiceSelected(service.id)}
                        />
                      </TableCell>
                    )}
                    <TableCell>
                      <Link
                        className={styles.rowLink}
                        to={serviceUrl(service.id)}
                        onClick={e => e.stopPropagation()}
                      >
                        {service.name}
                      </Link>
                      {image && <span className={styles.image}>{image}</span>}
                    </TableCell>
                    <TableCell nowrap>
                      <span className={styles.state}>
                        <StatusMark state={toMarkState(service.status)} size={12} />
                        {tCommon(`health.${service.status.toLowerCase()}`, {
                          defaultValue: service.status,
                        })}
                      </span>
                    </TableCell>
                    <TableCell align="right" nowrap variant="muted">
                      {deployed ? formatRelative(deployed, tCommon) : '—'}
                    </TableCell>
                    {canDeployService && !selectionMode && (
                      <TableCell align="right" nowrap>
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
        </div>
      ) : (
        <p className={styles.empty}>
          {services.length === 0 ? t('environments:noServices') : tDash('environments.empty')}
        </p>
      )}
    </Panel>
  );

  const variables = environment.environmentVariables ?? [];
  const variablesPanel = (
    <Panel
      flush
      title={tCommon('labels.variables')}
      meta={environment.totalEnvVars}
      actions={
        variables.length > 0 ? (
          <Button variant="text" size="xs" onClick={() => setSelectedMenuId('variables')}>
            {tCommon('labels.viewAll')}
          </Button>
        ) : undefined
      }
    >
      {variables.length > 0 ? (
        <ul className={styles.variables}>
          {variables.slice(0, VARIABLE_PREVIEW).map(v => (
            <li key={v.key} className={styles.variable}>
              <code className={styles.varKey}>{v.key}</code>
              <code className={styles.varValue}>{v.value}</code>
            </li>
          ))}
        </ul>
      ) : (
        <p className={styles.empty}>{t('environments:noVariables')}</p>
      )}
    </Panel>
  );

  const content = (
    <div className={styles.container}>
      {overview}
      <div className={styles.mainGrid}>
        <div className={styles.column}>{servicesPanel}</div>
        <div className={styles.column}>
          {incidents.length > 0 && (
            <Panel flush title={tDash('incidents.title')}>
              <IncidentList services={incidents} />
            </Panel>
          )}
          <Panel flush title={tDash('deployments.title')}>
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
    ...(canUpdateEnvironment
      ? [
          {
            id: 'configuration',
            label: t('environments:configuration'),
            content:
              projectId && environment ? (
                <EnvironmentSettingsForm
                  projectId={projectId}
                  environment={{ ...environment, serviceCount: services.length }}
                  onSuccess={handleEnvironmentUpdated}
                />
              ) : null,
          },
        ]
      : []),
    ...(projectId && environmentId
      ? [
          {
            id: 'variables',
            label: t('environments:variables'),
            content: (
              <EnvironmentVariablesEditor
                projectId={projectId}
                environmentId={environmentId}
                environmentName={environment.name}
              />
            ),
          },
        ]
      : []),
    ...(canManageSecrets && projectId && environmentId
      ? [
          {
            id: 'secrets',
            label: t('secrets:title'),
            content: (
              <SecretsSection
                parentType="Environment"
                projectId={projectId}
                environmentId={environmentId}
              />
            ),
          },
        ]
      : []),
    ...(canReadNotifications && environmentId
      ? [
          {
            id: 'notifications',
            label: t('environments:notifications'),
            icon: <Bell size={16} />,
            content: (
              <ScopedNotificationsSection ctx={{ scope: 'Environment', scopeId: environmentId }} />
            ),
          },
        ]
      : []),
  ];

  return (
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
      <BulkActionConfirmModal
        action={pendingBulkAction}
        services={services.filter(s => selectedServiceIds.has(s.id))}
        isSubmitting={isBulkSubmitting}
        error={bulkError}
        onCancel={() => {
          setPendingBulkAction(null);
          setBulkError(undefined);
        }}
        onConfirm={handleConfirmBulkAction}
      />
      {isExportModalOpen && projectId && environmentId && (
        <ExportEnvironmentModal
          onClose={() => setIsExportModalOpen(false)}
          projectId={projectId}
          environmentId={environmentId}
          environmentName={environment.name}
          services={services}
        />
      )}
    </ConfigurationPageLayout>
  );
}
