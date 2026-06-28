import { Container, Network, Play, RotateCw, Settings, Square } from 'lucide-react';
import { useTranslation } from 'react-i18next';

import { ServiceDashboardDto } from '@/api/types/service.types';
import { Row, Spacer, Stack } from '@/components/layout';
import { Button } from '@/components/ui/Button';
import { Card } from '@/components/ui/Card';
import { ServiceExposureChip } from '@/components/ui/chips/serviceExposureChip';
import { ServiceTypeChip } from '@/components/ui/chips/serviceTypeChip';
import { CodeSpan } from '@/components/ui/CodeSpan';
import { Divider } from '@/components/ui/Divider';
import { HealthIndicator } from '@/components/ui/HealthIndicator';
import { Label } from '@/components/ui/Label';

interface ServiceHeaderCardProps {
  service: ServiceDashboardDto;
  canDeployService: boolean;
  canUpdateService: boolean;
  isConfigOpen: boolean;
  onConfigToggle: () => void;
  onDeploy: () => void;
  onRestart: () => void;
  onStop: () => void;
  actionLoading: string | null;
}

export function ServiceHeaderCard({
  service,
  canDeployService,
  canUpdateService,
  isConfigOpen,
  onConfigToggle,
  onDeploy,
  onRestart,
  onStop,
  actionLoading,
}: ServiceHeaderCardProps) {
  const { t } = useTranslation(['services', 'common']);

  return (
    <Card style={{ width: '100%', padding: 'var(--space-4)' }}>
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
                onClick={onConfigToggle}
              >
                {isConfigOpen ? t('common:labels.closeSettings') : t('common:labels.settings')}
              </Button>
            )}
            {canDeployService && service.status === 'Running' && (
              <Button
                variant="secondary"
                size="sm"
                icon={<RotateCw size={16} />}
                onClick={onRestart}
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
                onClick={onStop}
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
                onClick={onDeploy}
                disabled={actionLoading !== null}
                isLoading={actionLoading === 'deploy'}
              >
                {service.status === 'Running' ? t('services:redeploy') : t('services:deploy')}
              </Button>
            )}
          </Row>
        </Row>
        {service.registry &&
          (service.registry.containerName ||
            service.registry.ipAddress ||
            service.registry.ports.length > 0) && (
            <>
              <Divider />
              <Row gap="4" align="center" wrap>
                {service.registry.containerName && (
                  <Row gap="2" align="center">
                    <Container size={14} style={{ color: 'var(--color-text-secondary)' }} />
                    <Label variant="secondary" size="sm" weight="semibold">
                      {t('services:containerName')}
                    </Label>
                    <CodeSpan copyable>{service.registry.containerName}</CodeSpan>
                  </Row>
                )}
                {service.registry.ipAddress && (
                  <>
                    {service.registry.containerName && (
                      <Divider orientation="vertical" style={{ height: '16px' }} />
                    )}
                    <Row gap="2" align="center">
                      <Network size={14} style={{ color: 'var(--color-text-secondary)' }} />
                      <Label variant="secondary" size="sm" weight="semibold">
                        {t('services:internalIp')}
                      </Label>
                      <CodeSpan copyable>{service.registry.ipAddress}</CodeSpan>
                    </Row>
                  </>
                )}
                {service.registry.ports.length > 0 && (
                  <>
                    {(service.registry.containerName || service.registry.ipAddress) && (
                      <Divider orientation="vertical" style={{ height: '16px' }} />
                    )}
                    <Row gap="2" align="center" wrap>
                      <Label variant="secondary" size="sm" weight="semibold">
                        Ports
                      </Label>
                      <Row gap="1" wrap>
                        {service.registry.ports.map((p, i) => (
                          <CodeSpan key={i}>
                            {p.hostPort != null
                              ? `${p.hostPort}:${p.containerPort}`
                              : String(p.containerPort)}
                          </CodeSpan>
                        ))}
                      </Row>
                    </Row>
                  </>
                )}
              </Row>
            </>
          )}
      </Stack>
    </Card>
  );
}
