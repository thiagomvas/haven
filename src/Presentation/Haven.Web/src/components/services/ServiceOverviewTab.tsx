import { Check, Copy, Globe, Lock, RefreshCw, Terminal } from 'lucide-react';
import { type ReactNode, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';

import { DeploymentDto, DockerConfig, DockerfileConfig } from '@/api/types';
import { ServiceDashboardDto } from '@/api/types';
import { ComputedOutputsCard } from '@/components/services/ComputedOutputsCard';
import { CustomActionsCard } from '@/components/services/CustomActionsCard';
import { DeploymentIdentifier } from '@/components/services/DeploymentIdentifier';
import { Button } from '@/components/ui/Button';
import { Chip } from '@/components/ui/Chip';
import { CodeSpan } from '@/components/ui/CodeSpan';
import { EnvironmentVariablesCard } from '@/components/ui/EnvironmentVariablesCard';
import { Panel } from '@/components/ui/Panel';
import { useNetworks } from '@/hooks/useNetworks';
import { copyToClipboard } from '@/lib/clipboard';
import styles from '@/styles/components/services/ServiceOverviewTab.module.css';

function CopyCommandButton({
  label,
  copiedLabel,
  icon,
  command,
}: {
  label: string;
  copiedLabel: string;
  icon: React.ReactNode;
  command: string;
}) {
  const [copied, setCopied] = useState(false);
  const timeoutRef = useRef<ReturnType<typeof setTimeout> | undefined>(undefined);

  const handleClick = async () => {
    try {
      await copyToClipboard(command);
      setCopied(true);
      clearTimeout(timeoutRef.current);
      timeoutRef.current = setTimeout(() => setCopied(false), 2000);
    } catch (err) {
      console.error('Failed to copy command:', err);
    }
  };

  return (
    <Button
      variant="ghost"
      size="sm"
      icon={copied ? <Check size={14} /> : icon}
      onClick={handleClick}
    >
      {copied ? copiedLabel : label}
    </Button>
  );
}

interface ServiceOverviewTabProps {
  projectId: string;
  service: ServiceDashboardDto;
  webhookUrl: string;
  deployments: DeploymentDto[];
  actionLoading: string | null;
  onRegenerateToken: () => void;
}

export function ServiceOverviewTab({
  projectId,
  service,
  webhookUrl,
  deployments,
  actionLoading,
  onRegenerateToken,
}: ServiceOverviewTabProps) {
  const { t } = useTranslation(['services', 'common']);

  const curlCommand = `curl -X POST '${webhookUrl}'`;
  const httpieCommand = `http POST ${webhookUrl}`;

  const deployed = deployments.find(d => d.status === 'Succeeded' && d.identifier);

  const { data: networks } = useNetworks();
  const sharedNetworks = (networks ?? []).filter(
    n => (n.type === 'Shared' || n.type === 'External') && n.services.some(s => s.id === service.id)
  );

  const domains = service.registry?.domains ?? [];
  const dockerImageConfig =
    service.type === 'DockerImage' ? (service.sourceConfig as DockerConfig) : undefined;
  const dockerfileConfig =
    service.type === 'Dockerfile' ? (service.sourceConfig as DockerfileConfig) : undefined;
  const sourceConfig = dockerImageConfig ?? dockerfileConfig;

  const rows: { label: string; value: ReactNode; mono?: boolean; key: string }[] = [
    ...(deployed
      ? [
          {
            key: 'deployed',
            label: t('services:deploymentsTab.currentlyDeployed'),
            value: <DeploymentIdentifier deployment={deployed} />,
          },
        ]
      : []),
    ...(dockerImageConfig
      ? [
          {
            key: 'image',
            label: t('common:labels.image'),
            value: dockerImageConfig.image,
            mono: true,
          },
        ]
      : []),
    ...(dockerfileConfig?.repository
      ? [
          {
            key: 'repository',
            label: t('services:createPage.gitRepository'),
            value: dockerfileConfig.repository,
            mono: true,
          },
        ]
      : []),
    ...(dockerfileConfig?.branch
      ? [
          {
            key: 'branch',
            label: t('services:createPage.branch'),
            value: dockerfileConfig.branch,
            mono: true,
          },
        ]
      : []),
    ...(sourceConfig?.restartPolicy
      ? [
          {
            key: 'restart',
            label: t('services:restartPolicy'),
            value: t(`services:restartPolicies.${sourceConfig.restartPolicy}`),
          },
        ]
      : []),
    ...(sourceConfig?.commandArgs && sourceConfig.commandArgs.length > 0
      ? [
          {
            key: 'args',
            label: t('common:labels.commandArgs'),
            value: sourceConfig.commandArgs.join(' '),
            mono: true,
          },
        ]
      : []),
  ];

  const sourceTitle = dockerfileConfig
    ? t('services:source.dockerfileTitle')
    : dockerImageConfig
      ? t('services:source.dockerImageTitle')
      : t('services:source.title');
  const showSource = rows.length > 0 || domains.length > 0 || sharedNetworks.length > 0;

  return (
    <div className={styles.layout}>
      <div className={styles.column}>
        <CustomActionsCard
          projectId={projectId}
          environmentId={service.environmentId}
          serviceId={service.id}
        />
        <Panel
          title={t('services:webhook.title')}
          actions={
            <Button
              variant="ghost"
              size="xs"
              icon={
                <RefreshCw
                  size={14}
                  className={actionLoading === 'regenerateToken' ? styles.spinning : undefined}
                />
              }
              onClick={onRegenerateToken}
              disabled={actionLoading !== null}
              title={t('services:webhook.regenerateTooltip')}
            >
              {t('services:webhook.regenerate')}
            </Button>
          }
        >
          <div className={styles.webhook}>
            <div className={styles.webhookUrl}>
              <Chip content="POST" size="sm" />
              <CodeSpan copyable className={styles.grow}>
                {webhookUrl}
              </CodeSpan>
            </div>
            <div className={styles.webhookCopy}>
              <CopyCommandButton
                label={t('services:webhook.copyAsCurl')}
                copiedLabel={t('services:webhook.copied')}
                icon={<Copy size={14} />}
                command={curlCommand}
              />
              <CopyCommandButton
                label={t('services:webhook.copyAsHttpie')}
                copiedLabel={t('services:webhook.copied')}
                icon={<Terminal size={14} />}
                command={httpieCommand}
              />
            </div>
          </div>
        </Panel>
        {service.computedOutputs && service.computedOutputs.length > 0 && (
          <ComputedOutputsCard
            projectId={projectId}
            environmentId={service.environmentId}
            serviceId={service.id}
            outputs={service.computedOutputs}
          />
        )}
      </div>

      <div className={styles.column}>
        {showSource && (
          <Panel flush title={sourceTitle}>
            <dl className={styles.defs}>
              {rows.map(row => (
                <div key={row.key} className={styles.def}>
                  <dt>{row.label}</dt>
                  <dd
                    className={row.mono ? styles.mono : undefined}
                    title={typeof row.value === 'string' ? row.value : undefined}
                  >
                    {row.value}
                  </dd>
                </div>
              ))}
              {sharedNetworks.length > 0 && (
                <div className={styles.def}>
                  <dt>{t('common:labels.networks')}</dt>
                  <dd className={styles.chips}>
                    {sharedNetworks.map(network => (
                      <Chip key={network.id} content={network.name} size="sm" />
                    ))}
                  </dd>
                </div>
              )}
              {domains.length > 0 && (
                <div className={styles.def}>
                  <dt>{t('services:routing.title')}</dt>
                  <dd className={styles.chips}>
                    {domains.map(domain => (
                      <Chip
                        key={domain.id}
                        icon={domain.tlsMode === 'None' ? <Globe size={14} /> : <Lock size={14} />}
                        content={`${domain.hostname}:${domain.containerPort}`}
                        size="sm"
                      />
                    ))}
                  </dd>
                </div>
              )}
            </dl>
          </Panel>
        )}
        {service.environmentVariables && service.environmentVariables.length > 0 && (
          <EnvironmentVariablesCard
            variables={service.environmentVariables}
            totalEnvVars={service.environmentVariables.length}
          />
        )}
      </div>
    </div>
  );
}
