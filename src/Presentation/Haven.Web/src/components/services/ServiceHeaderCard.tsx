import {
  Container,
  Download,
  Globe,
  Network,
  Play,
  RotateCw,
  Settings,
  Square,
} from 'lucide-react';
import { useTranslation } from 'react-i18next';

import { ServiceDashboardDto } from '@/api/types';
import { Row, Spacer, Stack } from '@/components/layout';
import { Button } from '@/components/ui/Button';
import { Card } from '@/components/ui/Card';
import { ServiceExposureChip } from '@/components/ui/chips/serviceExposureChip';
import { ServiceTypeChip } from '@/components/ui/chips/serviceTypeChip';
import { CodeSpan } from '@/components/ui/CodeSpan';
import { HealthIndicator } from '@/components/ui/HealthIndicator';
import { Label } from '@/components/ui/Label';
import { formatRelative } from '@/lib/utils';
import styles from '@/styles/components/services/ServiceHeaderCard.module.css';

interface ServiceHeaderCardProps {
  service: ServiceDashboardDto;
  canDeployService: boolean;
  canUpdateService: boolean;
  canExportService: boolean;
  isConfigOpen: boolean;
  onConfigToggle: () => void;
  onDeploy: () => void;
  onRestart: () => void;
  onStop: () => void;
  onExport: () => void;
  actionLoading: string | null;
}

export function ServiceHeaderCard({
  service,
  canDeployService,
  canUpdateService,
  canExportService,
  isConfigOpen,
  onConfigToggle,
  onDeploy,
  onRestart,
  onStop,
  onExport,
  actionLoading,
}: ServiceHeaderCardProps) {
  const { t } = useTranslation(['services', 'common']);
  const { t: tCommon } = useTranslation('common');

  const health =
    service.status !== 'Running'
      ? service.status.toLowerCase()
      : service.health !== 'Healthy'
        ? service.health.toLowerCase()
        : service.status.toLowerCase();

  const registry = service.registry;
  const domainsCount = registry?.domains.length ?? 0;
  const isRunning = service.status === 'Running';
  const startedRelative =
    isRunning && registry?.startedAt ? formatRelative(registry.startedAt, tCommon) : null;

  const hasMeta =
    !!startedRelative ||
    !!registry?.containerName ||
    !!registry?.ipAddress ||
    (registry?.ports.length ?? 0) > 0 ||
    domainsCount > 0;

  return (
    <Card className={styles.headerCard} padding="var(--space-4)">
      <Stack gap="3">
        <Row gap="3" full align="center" wrap>
          <div className={styles.identity}>
            <HealthIndicator health={health} useTooltip />
            <div className={styles.nameBlock}>
              <Label variant="primary" size="xxl" weight="bold" className={styles.name}>
                {service.name}
              </Label>
              {service.alias && <span className={styles.alias}>@{service.alias}</span>}
            </div>
          </div>
          <Row gap="2">
            <ServiceTypeChip serviceType={service.type} size="sm" />
            <ServiceExposureChip exposureMode={service.exposureMode} size="sm" />
          </Row>
          <Spacer expand direction="horizontal" />
          <Row gap="2" wrap className={styles.actions}>
            <Row gap="1" className={styles.utilityActions}>
              {canExportService && (
                <Button variant="ghost" size="sm" icon={<Download size={16} />} onClick={onExport}>
                  {t('services:export.button')}
                </Button>
              )}
              {canUpdateService && (
                <Button
                  variant="ghost"
                  size="sm"
                  icon={<Settings size={16} />}
                  onClick={onConfigToggle}
                >
                  {isConfigOpen ? t('common:labels.closeSettings') : t('common:labels.settings')}
                </Button>
              )}
            </Row>
            {canDeployService && isRunning && (
              <Row gap="2">
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
              </Row>
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
                {isRunning ? t('services:redeploy') : t('services:deploy')}
              </Button>
            )}
          </Row>
        </Row>

        {hasMeta && (
          <div className={styles.metaBar}>
            {startedRelative && (
              <div className={styles.metaItem}>
                <span className={styles.metaLabel}>{t('services:startedAt')}</span>
                <span className={styles.metaValue}>{startedRelative}</span>
              </div>
            )}
            {registry?.containerName && (
              <div className={styles.metaItem}>
                <Container size={14} className={styles.metaIcon} />
                <span className={styles.metaLabel}>{t('services:containerName')}</span>
                <CodeSpan copyable>{registry.containerName}</CodeSpan>
              </div>
            )}
            {registry?.ipAddress && (
              <div className={styles.metaItem}>
                <Network size={14} className={styles.metaIcon} />
                <span className={styles.metaLabel}>{t('services:internalIp')}</span>
                <CodeSpan copyable>{registry.ipAddress}</CodeSpan>
              </div>
            )}
            {registry && registry.ports.length > 0 && (
              <div className={styles.metaItem}>
                <span className={styles.metaLabel}>{t('services:ports')}</span>
                <Row gap="1" wrap>
                  {registry.ports.map((p, i) => (
                    <CodeSpan key={i}>
                      {p.hostPort != null
                        ? `${p.hostPort}:${p.containerPort}`
                        : String(p.containerPort)}
                    </CodeSpan>
                  ))}
                </Row>
              </div>
            )}
            {domainsCount > 0 && (
              <div className={styles.metaItem}>
                <Globe size={14} className={styles.metaIcon} />
                <span className={styles.metaLabel}>{t('services:domains.title')}</span>
                <span className={styles.metaValue}>{domainsCount}</span>
              </div>
            )}
          </div>
        )}
      </Stack>
    </Card>
  );
}
