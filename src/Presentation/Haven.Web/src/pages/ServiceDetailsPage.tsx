import { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { Play, Square, RotateCw, RefreshCw, Settings, Link, Container } from 'lucide-react';
import { useSetBreadcrumbs } from '@/hooks/useSetBreadcrumbs';
import { usePermission } from '@/hooks/usePermission';
import { projectsApi } from '../api/projects';
import { environmentsApi } from '../api/environments';
import { servicesApi } from '../api/services';
import {
  ProjectDto,
  EnvironmentDto,
  ServiceDashboardDto,
  DockerConfig,
  ServiceStatus,
} from '../api/types';
import { ServiceVariablesEditor } from '../components/services/ServiceVariablesEditor';
import { ServiceSettingsForm } from '../components/services/ServiceSettingsForm';
import { FeatureFlagsEditor } from '../components/services/FeatureFlagsEditor';
import { Button } from '../components/ui/Button';
import { Spinner } from '../components/ui/Spinner';
import { serviceStatusHub } from '../lib/signalr/hubs';
import { useSubscribeToServiceUpdates } from '../lib/signalr/useSubscribeToServiceUpdates';
import styles from './ServiceDetailsPage.module.css';
import { ServiceTypeChip } from '@/components/ui/chips/serviceTypeChip';
import { ServiceExposureChip } from '@/components/ui/chips/serviceExposureChip';
import { Row, ConfigurationPageLayout, Stack, Spacer, Grid } from '@/components/layout';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/Card';
import { Label } from '@/components/ui/Label';
import { HealthIndicator } from '@/components/ui/HealthIndicator';
import { CodeSpan } from '@/components/ui/CodeSpan';
import { EnvironmentVariablesCard } from '@/components/ui/EnvironmentVariablesCard';
import { KeyValueList, KeyValueRow } from '@/components/ui/KeyValueList';
import { Tabs } from '@/components/ui/Tabs';

export function ServiceDetailsPage() {
  const { projectId, environmentId, serviceId } = useParams<{
    projectId: string;
    environmentId: string;
    serviceId: string;
  }>();
  const navigate = useNavigate();
  const { t } = useTranslation(['projects', 'services', 'common']);

  const [project, setProject] = useState<ProjectDto | null>(null);
  const [environment, setEnvironment] = useState<EnvironmentDto | null>(null);
  const [service, setService] = useState<ServiceDashboardDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [actionLoading, setActionLoading] = useState<string | null>(null);
  const [isConfigOpen, setIsConfigOpen] = useState(false);
  const [isRegenerateConfirmOpen, setIsRegenerateConfirmOpen] = useState(false);

  useSetBreadcrumbs([
    { label: 'Projects', to: '/projects' },
    {
      label: project?.name ?? '…',
      to: projectId ? `/projects/${projectId}` : undefined,
    },
    {
      label: environment?.name ?? '…',
      to:
        projectId && environmentId
          ? `/projects/${projectId}/environments/${environmentId}`
          : undefined,
    },
    { label: service?.name ?? '…' },
  ]);

  useEffect(() => {
    const loadData = async () => {
      if (!projectId || !environmentId || !serviceId) return;

      try {
        setLoading(true);
        setError(null);

        const [projectData, environmentData, serviceData] = await Promise.all([
          projectsApi.getById(projectId),
          environmentsApi.getById(projectId, environmentId),
          servicesApi.getDashboard(projectId, environmentId, serviceId),
        ]);

        if (!projectData) {
          setError('Project not found');
          return;
        }
        if (!environmentData) {
          setError('Environment not found');
          return;
        }
        if (!serviceData) {
          setError('Service not found');
          return;
        }

        setProject(projectData);
        setEnvironment(environmentData);
        setService(serviceData);
      } catch (err) {
        setError(err instanceof Error ? err.message : t('error'));
      } finally {
        setLoading(false);
      }
    };

    loadData();
  }, [projectId, environmentId, serviceId, t]);

  useSubscribeToServiceUpdates(serviceStatusHub, serviceId, data => {
    if (data.serviceId === serviceId) {
      setService(prev => (prev ? { ...prev, status: data.newStatus as ServiceStatus } : null));
    }
  });

  const handleServiceUpdated = async () => {
    if (!projectId || !environmentId || !serviceId) return;
    try {
      const updated = await servicesApi.getDashboard(projectId, environmentId, serviceId);
      setService(updated);
    } catch (err) {
      console.error('Failed to refresh service', err);
    }
  };

  const handleDeploy = async () => {
    if (!projectId || !environmentId || !serviceId) return;
    try {
      setActionLoading('deploy');
      await servicesApi.deploy(projectId, environmentId, serviceId);
    } catch (err) {
      console.error('Failed to deploy service', err);
      setError(err instanceof Error ? err.message : t('error'));
    } finally {
      setActionLoading(null);
    }
  };

  const handleRestart = async () => {
    if (!projectId || !environmentId || !serviceId) return;
    try {
      setActionLoading('restart');
      await servicesApi.restart(projectId, environmentId, serviceId);
    } catch (err) {
      console.error('Failed to restart service', err);
      setError(err instanceof Error ? err.message : t('error'));
    } finally {
      setActionLoading(null);
    }
  };

  const handleStop = async () => {
    if (!projectId || !environmentId || !serviceId) return;
    try {
      setActionLoading('stop');
      await servicesApi.stop(projectId, environmentId, serviceId);
    } catch (err) {
      console.error('Failed to stop service', err);
      setError(err instanceof Error ? err.message : t('error'));
    } finally {
      setActionLoading(null);
    }
  };

  const getWebhookUrl = () => {
    if (!service?.webhookUrl) return '';
    const origin = window.location.origin;
    return `${origin}/${service.webhookUrl.replace(/^\/+/, '')}`;
  };

  const handleRegenerateTokenConfirm = async () => {
    if (!projectId || !environmentId || !serviceId) return;
    try {
      setActionLoading('regenerateToken');
      await servicesApi.regenerateToken(projectId, environmentId, serviceId);
      const updated = await servicesApi.getDashboard(projectId, environmentId, serviceId);
      setService(updated);
      setIsRegenerateConfirmOpen(false);
    } catch (err) {
      console.error('Failed to regenerate token', err);
      setError(err instanceof Error ? err.message : t('error'));
    } finally {
      setActionLoading(null);
    }
  };

  const canDeployService = usePermission('projects.manage_deploys');
  const canUpdateService = usePermission('projects.create');

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

  if (!project || !environment || !service) {
    return (
      <div className={styles.container}>
        <div className={styles.error}>
          <p>{t('projects:notFound')}</p>
          <button onClick={() => navigate(`/projects/${projectId}/environments/${environmentId}`)}>
            {t('projects:back')}
          </button>
        </div>
      </div>
    );
  }

  const header = (
    <Card style={{ width: '100%', padding: 'var(--space-4)' }}>
      <Row align="center" gap="4" full>
        <div style={{ flex: 1 }}>
          <Stack gap="2">
            <Row gap="2" full align="center">
              <HealthIndicator health={service.status.toLowerCase()} useTooltip />
              <Label variant="primary" size="xxl" weight="bold">
                {service.name}
              </Label>
              <ServiceTypeChip serviceType={service.type} size="sm" />
              <ServiceExposureChip exposureMode={service.exposureMode} size="sm" />
              <Spacer expand direction="horizontal" />
              <Row gap="2" wrap>
                {canUpdateService && (
                  <Button
                    variant="text"
                    size="sm"
                    icon={<Settings size={16} />}
                    onClick={() => setIsConfigOpen(!isConfigOpen)}
                  >
                    {isConfigOpen ? t('common:labels.closeSettings') : t('common:labels.settings')}
                  </Button>
                )}
                {canDeployService && service.status === 'Running' && (
                  <Button
                    variant="secondary"
                    size="sm"
                    icon={<RotateCw size={16} />}
                    onClick={handleRestart}
                    disabled={actionLoading !== null}
                    isLoading={actionLoading === 'restart'}
                  >
                    {t('services:restart')}
                  </Button>
                )}
                {canDeployService && service.status === 'Running' && (
                  <Button
                    variant="secondary"
                    size="sm"
                    icon={<Square size={16} />}
                    onClick={handleStop}
                    disabled={actionLoading !== null}
                    isLoading={actionLoading === 'stop'}
                  >
                    {t('services:stop')}
                  </Button>
                )}
                {canDeployService && (
                  <Button
                    variant="primary"
                    size="sm"
                    icon={<Play size={16} />}
                    onClick={handleDeploy}
                    disabled={actionLoading !== null}
                    isLoading={actionLoading === 'deploy'}
                  >
                    {service.status === 'Running' ? t('services:redeploy') : t('services:deploy')}
                  </Button>
                )}
              </Row>
            </Row>
          </Stack>
        </div>
      </Row>
    </Card>
  );

  const overviewContent = (
    <Grid columns={2} columnTemplate="1.5fr 1fr">
      <Stack gap="4">
        <Card padding="var(--space-4)">
          <Stack gap="3">
            <Label variant="secondary" size="sm" weight="semibold">
              {t('services:id')}
            </Label>
            <CodeSpan copyable>{service.id}</CodeSpan>
          </Stack>
        </Card>
        <Card padding="var(--space-4)">
          <Stack gap="3">
            <Row gap="2" align="center">
              <Link size={14} />
              <Label variant="secondary" size="sm" weight="semibold">
                Webhook URL
              </Label>
            </Row>
            <Row gap="2" align="center">
              <CodeSpan copyable style={{ flex: 1 }}>
                {getWebhookUrl()}
              </CodeSpan>
              <Button
                variant="ghost"
                size="sm"
                icon={
                  actionLoading === 'regenerateToken' ? (
                    <RefreshCw size={14} className={styles.spinning} />
                  ) : (
                    <RefreshCw size={14} />
                  )
                }
                onClick={() => setIsRegenerateConfirmOpen(true)}
                disabled={actionLoading !== null}
                title="Regenerate token"
              >
                Regenerate
              </Button>
            </Row>
          </Stack>
        </Card>
      </Stack>
      <Stack gap="4">
        <EnvironmentVariablesCard
          variables={service.environmentVariables}
          totalEnvVars={service.environmentVariables.length}
        />
        {service.type === 'DockerImage' &&
          (() => {
            const cfg = service.sourceConfig as DockerConfig | undefined;
            if (!cfg) return null;
            return (
              <Card padding="var(--space-4)">
                <CardHeader>
                  <CardTitle>
                    <Row gap="2" align="center">
                      <Container size={16} />
                      {t('common:labels.container')}
                    </Row>
                  </CardTitle>
                </CardHeader>
                <CardContent>
                  <KeyValueList bare>
                    <KeyValueRow label={t('common:labels.image')}>{cfg.image}</KeyValueRow>
                  </KeyValueList>
                </CardContent>
              </Card>
            );
          })()}
      </Stack>
    </Grid>
  );

  const menuItems = [
    ...(canUpdateService
      ? [
          {
            id: 'settings',
            label: t('services:configuration'),
            content: (
              <ServiceSettingsForm
                projectId={projectId!}
                environmentId={environmentId!}
                serviceId={serviceId!}
                service={service}
                onSuccess={handleServiceUpdated}
              />
            ),
          },
        ]
      : []),
    ...(projectId && environmentId && serviceId
      ? [
          {
            id: 'variables',
            label: t('services:environment'),
            content: (
              <ServiceVariablesEditor
                projectId={projectId}
                environmentId={environmentId}
                serviceId={serviceId}
              />
            ),
          },
        ]
      : []),
    ...(projectId && environmentId && serviceId
      ? [
          {
            id: 'featureFlags',
            label: t('services:featureFlags') || 'Feature Flags',
            content: (
              <FeatureFlagsEditor
                projectId={projectId}
                environmentId={environmentId}
                serviceId={serviceId}
              />
            ),
          },
        ]
      : []),
  ];

  return (
    <>
      {error && (
        <div className={styles.errorBanner}>
          <div className={styles.errorBannerContent}>
            <p>{error}</p>
            <button className={styles.errorBannerClose} onClick={() => setError(null)}>
              ✕
            </button>
          </div>
        </div>
      )}

      <ConfigurationPageLayout
        mainHeader={header}
        configHeader={header}
        menuItems={menuItems}
        isConfigOpen={isConfigOpen}
        onConfigOpenChange={setIsConfigOpen}
        hideConfigButton={true}
        hideCloseButton={true}
      >
        <Tabs
          items={[
            {
              id: 'overview',
              label: t('common:labels.overview'),
              content: overviewContent,
            },
          ]}
        />
      </ConfigurationPageLayout>

      {isRegenerateConfirmOpen && (
        <div className={styles.deleteConfirmOverlay}>
          <div className={styles.deleteConfirmDialog}>
            <h2 className={styles.deleteConfirmTitle}>{t('services:regenerateTokenTitle')}</h2>
            <p className={styles.deleteConfirmMessage}>{t('services:regenerateTokenWarning')}</p>
            <div className={styles.deleteConfirmActions}>
              <Button
                variant="ghost"
                onClick={() => setIsRegenerateConfirmOpen(false)}
                disabled={actionLoading === 'regenerateToken'}
              >
                {t('projects:cancel')}
              </Button>
              <Button
                variant="danger"
                onClick={handleRegenerateTokenConfirm}
                isLoading={actionLoading === 'regenerateToken'}
              >
                {t('services:regenerateToken')}
              </Button>
            </div>
          </div>
        </div>
      )}
    </>
  );
}
