import { Check, Container, Copy, Link, RefreshCw, Terminal } from 'lucide-react';
import { useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';

import { DockerConfig } from '@/api/types';
import { ServiceDashboardDto } from '@/api/types';
import { Grid, Row, Stack } from '@/components/layout';
import { Button } from '@/components/ui/Button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/Card';
import { Chip } from '@/components/ui/Chip';
import { CodeSpan } from '@/components/ui/CodeSpan';
import { EnvironmentVariablesCard } from '@/components/ui/EnvironmentVariablesCard';
import { KeyValueList, KeyValueRow } from '@/components/ui/KeyValueList';
import { Label } from '@/components/ui/Label';
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

interface ServiceOverviewTabProps {
  service: ServiceDashboardDto;
  webhookUrl: string;
  actionLoading: string | null;
  onRegenerateToken: () => void;
}

export function ServiceOverviewTab({
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

  return (
    <Grid columns={2} columnTemplate="1.5fr 1fr">
      <Stack gap="4">
        <Card padding="var(--space-4)">
          <Stack gap="3">
            <Row gap="2" align="center">
              <Link size={14} />
              <Label variant="secondary" size="sm" weight="semibold">
                {t('services:webhook.title')}
              </Label>
            </Row>
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
        </Card>
      </Stack>
      <Stack gap="4">
        {service.environmentVariables && service.environmentVariables.length > 0 && (
          <EnvironmentVariablesCard
            variables={service.environmentVariables}
            totalEnvVars={service.environmentVariables.length}
          />
        )}
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
                    <KeyValueRow label={t('common:labels.internalIp')}>
                      {service.registry?.ipAddress}
                    </KeyValueRow>
                    <KeyValueRow label={t('common:labels.status')}>
                      <HealthIndicator showLabel health={service.status.toLocaleLowerCase()} />
                    </KeyValueRow>
                    {sharedNetworks.length > 0 && (
                      <KeyValueRow label={t('common:labels.networks')}>
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
                      </KeyValueRow>
                    )}
                  </KeyValueList>
                </CardContent>
              </Card>
            );
          })()}
      </Stack>
    </Grid>
  );
}
