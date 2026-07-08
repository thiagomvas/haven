import { Bell, Network, Plus, Rocket, Settings, Wifi } from 'lucide-react';
import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate, useParams } from 'react-router-dom';

import { EnvironmentDashboardDto } from '@/api/types';
import { ProjectDto } from '@/api/types';
import { ServiceDto } from '@/api/types';
import { ServiceStatus } from '@/api/types';
import { ConfigurationPageLayout, Grid, Row, Spacer, Stack } from '@/components/layout';
import { ScopedNotificationsSection } from '@/components/notificationChannels/ScopedNotificationsSection';
import { Card } from '@/components/ui/Card';
import { Chip } from '@/components/ui/Chip';
import { DegradedServicesChip } from '@/components/ui/chips/degradedServicesChip';
import { CodeSpan } from '@/components/ui/CodeSpan';
import { EnvironmentVariablesCard } from '@/components/ui/EnvironmentVariablesCard';
import { HealthIndicator } from '@/components/ui/HealthIndicator';
import { Label } from '@/components/ui/Label';
import { ProjectAvatar } from '@/components/ui/ProjectAvatar';
import { usePermission } from '@/hooks/usePermission';
import { useSetBreadcrumbs } from '@/hooks/useSetBreadcrumbs';
import { useUrlState } from '@/hooks/useUrlState';
import styles from '@/styles/pages/EnvironmentDetailsPage.module.css';

import { environmentsApi } from '../api/environments';
import { projectsApi } from '../api/projects';
import { servicesApi } from '../api/services';
import { EnvironmentSettingsForm } from '../components/environments/EnvironmentSettingsForm';
import { EnvironmentVariablesEditor } from '../components/environments/EnvironmentVariablesEditor';
import { ServiceCard } from '../components/projects/ServiceCard';
import { Button } from '../components/ui/Button';
import { Spinner } from '../components/ui/Spinner';
import { serviceStatusHub } from '../lib/signalr/hubs';
import { useSubscribeToMultipleServices } from '../lib/signalr/useSubscribeToMultipleServices';

export function EnvironmentDetailsPage() {
  const { projectId, environmentId } = useParams<{
    projectId: string;
    environmentId: string;
  }>();
  const navigate = useNavigate();
  const { t } = useTranslation(['projects', 'environments', 'common']);

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
  const canReadNotifications = usePermission('system.read_notifications');

  const handleAddService = () => {
    navigate(`/services/create?projectId=${projectId}&environmentId=${environmentId}`);
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
            ? { ...service, status: data.newStatus as ServiceStatus }
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

  if (loading) {
    return (
      <div className={styles.container}>
        <div className={styles.spinner}>
          <Spinner />
          <p>{t('projects:loading')}</p>
        </div>
      </div>
    );
  }

  if (error || !project || !environment) {
    return (
      <div className={styles.container}>
        <div className={styles.error}>
          <p>{error || t('projects:notFound')}</p>
          <button onClick={() => navigate(`/projects/${projectId}`)}>{t('projects:back')}</button>
        </div>
      </div>
    );
  }

  const header = (
    <Card style={{ width: '100%', padding: 'var(--space-4)' }}>
      <Row align="center" gap="4" full>
        <Stack gap="2">
          <Row gap="2" full align="center">
            <Label variant="primary" size="xxl" weight="bold">
              {environment.name}
            </Label>
            <Label variant="muted" size="md">
              {t('common:nouns.in')}
            </Label>
            <Label variant="secondary" size="xl">
              {project.name}
            </Label>
            <DegradedServicesChip count={environment.serviceStatistics.degraded} />
            <Spacer expand direction="horizontal" />
            {canUpdateEnvironment && (
              <Button
                variant="text"
                size="sm"
                icon={<Settings size={16} />}
                onClick={() => setIsConfigOpen(!isConfigOpen)}
              >
                {isConfigOpen ? t('common:labels.closeSettings') : t('common:labels.settings')}
              </Button>
            )}
          </Row>
          <Row gap="2">
            <CodeSpan icon={<Wifi size={'var(--icon-size-sm)'} />} copyable>
              {environment.networkName}
            </CodeSpan>
          </Row>
          {environment.description && (
            <p className={styles.description}>{environment.description}</p>
          )}
        </Stack>
      </Row>
    </Card>
  );

  const servicesContent = (
    <Grid columns={2} columnTemplate="1.5fr 1fr">
      <Stack>
        <Card padding="var(--space-4)">
          <Row align="center" gap="2" full>
            <Row gap="2" align="center" full>
              {t('environments:services')}
              <Chip variant="default" size="sm" content={services.length} />
              <Spacer expand direction="horizontal" />
              {canCreateService && (
                <Button variant="text" disabled icon={<Rocket size={16} />}>
                  {t('common:actions.deployAll')}
                </Button>
              )}
            </Row>
          </Row>
          {services.length > 0 ? (
            <div style={{ marginTop: 'var(--space-4)' }}>
              <Grid columns={2}>
                {services.map(service => (
                  <ServiceCard
                    key={service.id}
                    service={service}
                    onClick={() =>
                      navigate(
                        `/projects/${projectId}/environments/${environmentId}/services/${service.id}`
                      )
                    }
                  />
                ))}
                {canCreateService && (
                  <Button
                    onClick={handleAddService}
                    variant="secondary"
                    size="lg"
                    icon={<Plus size={32} />}
                    style={{
                      display: 'flex',
                      flexDirection: 'column',
                      alignItems: 'center',
                      justifyContent: 'center',
                      gap: 'var(--space-3)',
                      minHeight: '200px',
                      width: '100%',
                    }}
                  >
                    {t('environments:addServiceToEnvironment', {
                      environmentName: environment.name,
                    })}
                  </Button>
                )}
              </Grid>
            </div>
          ) : (
            <p
              style={{
                padding: 'var(--space-3)',
                color: 'var(--color-text-secondary)',
                marginTop: 'var(--space-3)',
              }}
            >
              {t('environments:noServices')}
              <Button
                onClick={handleAddService}
                size="sm"
                icon={<Plus size={16} />}
                style={{ marginLeft: 'var(--space-2)' }}
              >
                {t('environments:addServiceToEnvironment', {
                  environmentName: environment.name,
                })}
              </Button>
            </p>
          )}
        </Card>
      </Stack>
      <Stack gap="2">
        {environment.environmentVariables && environment.environmentVariables.length > 0 && (
          <EnvironmentVariablesCard
            variables={environment.environmentVariables}
            totalEnvVars={environment.totalEnvVars}
            onViewAll={() => setSelectedMenuId('variables')}
          />
        )}
      </Stack>
    </Grid>
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
              <EnvironmentVariablesEditor projectId={projectId} environmentId={environmentId} />
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
      {servicesContent}
    </ConfigurationPageLayout>
  );
}
