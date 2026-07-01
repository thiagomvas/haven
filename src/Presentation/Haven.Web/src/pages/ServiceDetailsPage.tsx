import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate, useParams } from 'react-router-dom';

import { EnvironmentDto } from '@/api/types/environment.types';
import { ProjectDto } from '@/api/types/project.types';
import { ServiceDashboardDto } from '@/api/types/service.types';
import { ServiceStatus } from '@/api/types/service.types';
import { ConfigurationPageLayout, Row, Stack } from '@/components/layout';
import { ScopedNotificationsSection } from '@/components/notificationChannels/ScopedNotificationsSection';
import { Tabs } from '@/components/ui/Tabs';
import { usePermission } from '@/hooks/usePermission';
import { useSetBreadcrumbs } from '@/hooks/useSetBreadcrumbs';
import { useUrlState } from '@/hooks/useUrlState';

import { environmentsApi } from '../api/environments';
import { projectsApi } from '../api/projects';
import { servicesApi } from '../api/services';
import { DeploymentsTab } from '../components/services/DeploymentsTab';
import { FeatureFlagsEditor } from '../components/services/FeatureFlagsEditor';
import { ServiceHeaderCard } from '../components/services/ServiceHeaderCard';
import { ServiceManifestEditor } from '../components/services/ServiceManifestEditor';
import { ServiceOverviewTab } from '../components/services/ServiceOverviewTab';
import { ServiceSettingsForm } from '../components/services/ServiceSettingsForm';
import { ServiceVariablesEditor } from '../components/services/ServiceVariablesEditor';
import { Button } from '../components/ui/Button';
import { ErrorAlert } from '../components/ui/ErrorAlert';
import { Label } from '../components/ui/Label';
import { Modal } from '../components/ui/Modal';
import { Spinner } from '../components/ui/Spinner';
import { serviceStatusHub } from '../lib/signalr/hubs';
import { useSubscribeToServiceUpdates } from '../lib/signalr/useSubscribeToServiceUpdates';
import styles from '@/styles/pages/ServiceDetailsPage.module.css';

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
  const [isRegenerateConfirmOpen, setIsRegenerateConfirmOpen] = useState(false);
  const [activeTab, setActiveTab] = useUrlState('tab', 'overview');
  const [configParam, setConfigParam] = useUrlState('config', '');
  const isConfigOpen = configParam !== '';
  const selectedConfigMenuId = configParam || 'settings';
  const setIsConfigOpen = (open: boolean) => setConfigParam(open ? 'settings' : '');
  const setSelectedConfigMenuId = (id: string) => setConfigParam(id);

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
    if (service.webhookUrl.startsWith('http://') || service.webhookUrl.startsWith('https://'))
      return service.webhookUrl;
    return `${window.location.origin}/${service.webhookUrl.replace(/^\/+/, '')}`;
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
  const canReadNotifications = usePermission('system.read_notifications');

  if (loading) {
    return (
      <div className={styles.spinner}>
        <Spinner />
        <Label variant="secondary">{t('projects:loading')}</Label>
      </div>
    );
  }

  if (!project || !environment || !service) {
    return (
      <Stack gap="3" className={styles.notFound}>
        <ErrorAlert message={t('projects:notFound')} variant="block" />
        <Button
          variant="secondary"
          onClick={() => navigate(`/projects/${projectId}/environments/${environmentId}`)}
        >
          {t('projects:back')}
        </Button>
      </Stack>
    );
  }

  const header = (
    <ServiceHeaderCard
      service={service}
      canDeployService={canDeployService}
      canUpdateService={canUpdateService}
      isConfigOpen={isConfigOpen}
      onConfigToggle={() => setIsConfigOpen(!isConfigOpen)}
      onDeploy={handleDeploy}
      onRestart={handleRestart}
      onStop={handleStop}
      actionLoading={actionLoading}
    />
  );

  const generalItems = [
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
    ...(canReadNotifications && serviceId
      ? [
          {
            id: 'notifications',
            label: t('services:notifications') || 'Notifications',
            content: <ScopedNotificationsSection ctx={{ scope: 'Service', scopeId: serviceId }} />,
          },
        ]
      : []),
  ];

  const advancedItems = [
    ...(canUpdateService && projectId && environmentId && serviceId
      ? [
          {
            id: 'manifest',
            label: t('services:manifest.tab'),
            content: (
              <ServiceManifestEditor
                projectId={projectId}
                environmentId={environmentId}
                serviceId={serviceId}
                onApplied={handleServiceUpdated}
              />
            ),
          },
        ]
      : []),
  ];

  const sections = [
    ...(generalItems.length > 0
      ? [{ id: 'general', label: t('common:labels.settings'), items: generalItems }]
      : []),
    ...(advancedItems.length > 0
      ? [{ id: 'advanced', label: t('common:labels.advanced'), items: advancedItems }]
      : []),
  ];

  return (
    <>
      {error && (
        <div className={styles.errorBanner}>
          <Row className={styles.errorBannerContent} gap="4" align="center">
            <Label variant="secondary" style={{ color: 'var(--color-error)', flex: 1 }}>
              {error}
            </Label>
            <button className={styles.errorBannerClose} onClick={() => setError(null)}>
              ✕
            </button>
          </Row>
        </div>
      )}

      <ConfigurationPageLayout
        mainHeader={header}
        configHeader={header}
        sections={sections}
        isConfigOpen={isConfigOpen}
        onConfigOpenChange={setIsConfigOpen}
        selectedMenuId={selectedConfigMenuId}
        onSelectedMenuIdChange={setSelectedConfigMenuId}
        hideConfigButton={true}
        hideCloseButton={true}
      >
        <Tabs
          activeTab={activeTab}
          onChange={setActiveTab}
          items={[
            {
              id: 'overview',
              label: t('common:labels.overview'),
              content: (
                <ServiceOverviewTab
                  service={service}
                  webhookUrl={getWebhookUrl()}
                  actionLoading={actionLoading}
                  onRegenerateToken={() => setIsRegenerateConfirmOpen(true)}
                />
              ),
            },
            {
              id: 'deployments',
              label: t('services:deployments') || 'Deployments',
              content: (
                <DeploymentsTab
                  projectId={projectId!}
                  environmentId={environmentId!}
                  serviceId={serviceId!}
                />
              ),
            },
          ]}
        />
      </ConfigurationPageLayout>

      <Modal
        isOpen={isRegenerateConfirmOpen}
        onClose={() => setIsRegenerateConfirmOpen(false)}
        title={t('services:regenerateTokenTitle')}
        description={t('services:regenerateTokenWarning')}
        size="sm"
        footer={
          <Row gap="3" justify="flex-end">
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
          </Row>
        }
      >
        {null}
      </Modal>
    </>
  );
}
