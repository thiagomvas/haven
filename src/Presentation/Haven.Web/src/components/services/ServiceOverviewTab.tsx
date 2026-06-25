import { Link, Container, RefreshCw } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { ServiceDashboardDto, DockerConfig } from '@/api/types';
import { Grid, Stack, Row } from '@/components/layout';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/Card';
import { Label } from '@/components/ui/Label';
import { Button } from '@/components/ui/Button';
import { CodeSpan } from '@/components/ui/CodeSpan';
import { EnvironmentVariablesCard } from '@/components/ui/EnvironmentVariablesCard';
import { KeyValueList, KeyValueRow } from '@/components/ui/KeyValueList';
import styles from './ServiceOverviewTab.module.css';
import { ServiceChip } from '../ui/chips/ServiceChip';
import { HealthIndicator } from '../ui/HealthIndicator';

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

  return (
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
                title="Regenerate token"
              >
                Regenerate
              </Button>
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
                  </KeyValueList>
                </CardContent>
              </Card>
            );
          })()}
      </Stack>
    </Grid>
  );
}
