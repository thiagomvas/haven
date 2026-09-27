import {
  Check,
  Container,
  Copy,
  FileCode,
  Globe,
  Link,
  Lock,
  RefreshCw,
  Settings,
  Terminal,
} from 'lucide-react';
import { useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';

import { DockerConfig, DockerfileConfig } from '@/api/types';
import { ServiceDashboardDto } from '@/api/types';
import { Grid, Row, Stack } from '@/components/layout';
import { ComputedOutputsCard } from '@/components/services/ComputedOutputsCard';
import { Button } from '@/components/ui/Button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/Card';
import { Chip } from '@/components/ui/Chip';
import { CodeSpan } from '@/components/ui/CodeSpan';
import { EnvironmentVariablesCard } from '@/components/ui/EnvironmentVariablesCard';
import { useNetworks } from '@/hooks/useNetworks';
import styles from '@/styles/components/services/ServiceOverviewTab.module.css';

import { HealthIndicator } from '../ui/HealthIndicator';

function copyToClipboard(text: string): Promise<void> {
  if (navigator.clipboard) {
    return navigator.clipboard.writeText(text);
  }
  const textarea = document.createElement('textarea');
  textarea.value = text;
  textarea.style.position = 'fixed';
  textarea.style.opacity = '0';
  document.body.appendChild(textarea);
  textarea.focus();
  textarea.select();
  try {
    if (!document.execCommand('copy')) {
      throw new Error('Copy command was unsuccessful');
    }
  } finally {
    document.body.removeChild(textarea);
  }
  return Promise.resolve();
}

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
      variant="secondary"
      size="sm"
      icon={copied ? <Check size={14} /> : icon}
      onClick={handleClick}
    >
      {copied ? copiedLabel : label}
    </Button>
  );
}

const tlsChipVariant = { None: 'default', Acme: 'success', Custom: 'warning' } as const;

interface ServiceOverviewTabProps {
  projectId: string;
  service: ServiceDashboardDto;
  webhookUrl: string;
  actionLoading: string | null;
  onRegenerateToken: () => void;
}

export function ServiceOverviewTab({
  projectId,
  service,
  webhookUrl,
  actionLoading,
  onRegenerateToken,
}: ServiceOverviewTabProps) {
  const { t } = useTranslation(['services', 'common']);

  const curlCommand = `curl -X POST '${webhookUrl}'`;
  const httpieCommand = `http POST ${webhookUrl}`;

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

  return (
    <Grid columns={2} columnTemplate="1.5fr 1fr">
      <Stack gap="4">
        <Card padding="var(--space-4)">
          <CardHeader>
            <CardTitle>
              <Row gap="2" align="center">
                <Link size={16} />
                {t('services:webhook.title')}
              </Row>
            </CardTitle>
          </CardHeader>
          <CardContent>
            <Stack gap="3">
              <Row gap="2" align="center">
                <Chip content="POST" variant="success" size="sm" />
                <CodeSpan copyable style={{ flex: 1 }}>
                  {webhookUrl}
                </CodeSpan>
                <Button
                  variant="ghost"
                  size="sm"
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
              </Row>
              <Row gap="2" align="center">
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
              </Row>
            </Stack>
          </CardContent>
        </Card>
        {service.computedOutputs && service.computedOutputs.length > 0 && (
          <ComputedOutputsCard
            projectId={projectId}
            environmentId={service.environmentId}
            serviceId={service.id}
            outputs={service.computedOutputs}
          />
        )}
      </Stack>

      <Stack gap="4">
        {(sourceConfig || domains.length > 0 || sharedNetworks.length > 0) && (
          <Card padding="var(--space-4)">
            <CardHeader>
              <CardTitle>
                <Row gap="2" align="center">
                  {dockerfileConfig ? (
                    <FileCode size={16} />
                  ) : dockerImageConfig ? (
                    <Container size={16} />
                  ) : (
                    <Settings size={16} />
                  )}
                  {dockerfileConfig
                    ? t('services:source.dockerfileTitle')
                    : dockerImageConfig
                      ? t('services:source.dockerImageTitle')
                      : t('services:source.title')}
                </Row>
              </CardTitle>
            </CardHeader>
            <CardContent>
              <Stack gap="4">
                <div className={styles.infoGrid}>
                  <div className={styles.infoItem}>
                    <span className={styles.infoLabel}>{t('common:labels.status')}</span>
                    <HealthIndicator showLabel health={service.status.toLocaleLowerCase()} />
                  </div>
                  {dockerImageConfig && (
                    <div className={styles.infoItem}>
                      <span className={styles.infoLabel}>{t('common:labels.image')}</span>
                      <span
                        className={`${styles.infoValue} ${styles.infoValueMono}`}
                        title={dockerImageConfig.image}
                      >
                        {dockerImageConfig.image}
                      </span>
                    </div>
                  )}
                  {dockerfileConfig?.repository && (
                    <div className={styles.infoItem}>
                      <span className={styles.infoLabel}>
                        {t('services:createPage.gitRepository')}
                      </span>
                      <span className={styles.infoValue} title={dockerfileConfig.repository}>
                        {dockerfileConfig.repository}
                      </span>
                    </div>
                  )}
                  {dockerfileConfig?.branch && (
                    <div className={styles.infoItem}>
                      <span className={styles.infoLabel}>{t('services:createPage.branch')}</span>
                      <span className={styles.infoValue}>{dockerfileConfig.branch}</span>
                    </div>
                  )}
                  {service.registry?.ipAddress && (
                    <div className={styles.infoItem}>
                      <span className={styles.infoLabel}>{t('common:labels.internalIp')}</span>
                      <span className={`${styles.infoValue} ${styles.infoValueMono}`}>
                        {service.registry.ipAddress}
                      </span>
                    </div>
                  )}
                  {sourceConfig?.restartPolicy && (
                    <div className={styles.infoItem}>
                      <span className={styles.infoLabel}>{t('services:restartPolicy')}</span>
                      <span className={styles.infoValue}>
                        {t(`services:restartPolicies.${sourceConfig.restartPolicy}`)}
                      </span>
                    </div>
                  )}
                </div>
                {sourceConfig?.commandArgs && sourceConfig.commandArgs.length > 0 && (
                  <div className={styles.extraRow}>
                    <span className={styles.extraLabel}>{t('common:labels.commandArgs')}</span>
                    <CodeSpan>{sourceConfig.commandArgs.join(' ')}</CodeSpan>
                  </div>
                )}
                {sharedNetworks.length > 0 && (
                  <div className={styles.extraRow}>
                    <span className={styles.extraLabel}>{t('common:labels.networks')}</span>
                    <Row gap="2" wrap>
                      {sharedNetworks.map(network => (
                        <Chip
                          key={network.id}
                          content={network.name}
                          size="sm"
                          variant={network.type === 'Shared' ? 'success' : 'warning'}
                        />
                      ))}
                    </Row>
                  </div>
                )}
                {domains.length > 0 && (
                  <div className={styles.extraRow}>
                    <span className={styles.extraLabel}>{t('services:routing.title')}</span>
                    <Row gap="2" wrap>
                      {domains.map(domain => (
                        <Chip
                          key={domain.id}
                          icon={
                            domain.tlsMode === 'None' ? <Globe size={14} /> : <Lock size={14} />
                          }
                          content={`${domain.hostname}:${domain.containerPort}`}
                          size="sm"
                          variant={tlsChipVariant[domain.tlsMode]}
                        />
                      ))}
                    </Row>
                  </div>
                )}
              </Stack>
            </CardContent>
          </Card>
        )}
        {service.environmentVariables && service.environmentVariables.length > 0 && (
          <EnvironmentVariablesCard
            variables={service.environmentVariables}
            totalEnvVars={service.environmentVariables.length}
          />
        )}
      </Stack>
    </Grid>
  );
}
