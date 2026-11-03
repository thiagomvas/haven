import { Download, GitBranch, Play, RotateCw, Settings, Square } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';

import { ServiceDashboardDto } from '@/api/types';
import { Button } from '@/components/ui/Button';
import { CodeSpan } from '@/components/ui/CodeSpan';
import { HealthIndicator } from '@/components/ui/HealthIndicator';
import styles from '@/styles/components/services/ServiceHeaderCard.module.css';

interface ServiceHeaderCardProps {
  service: ServiceDashboardDto;
  environmentName?: string;
  environmentUrl?: string;
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

/** Page head for a service: identity, source/runtime identifiers and the primary actions. */
export function ServiceHeaderCard({
  service,
  environmentName,
  environmentUrl,
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

  const registry = service.registry;
  const isRunning = service.status === 'Running';
  // Intent, not just observed state: a crashing or restarting service must still be stoppable.
  const canControl =
    service.desiredState === 'Running' ||
    ['Running', 'Restarting', 'Crashed'].includes(service.runtimeState);
  const busy = actionLoading !== null;

  const binding = service.repositoryBinding;
  const hasMeta = !!binding || !!registry?.containerName || !!registry?.ipAddress;

  return (
    <div className={styles.pageHead}>
      <div className={styles.identity}>
        <div className={styles.titleRow}>
          <HealthIndicator health={service.status.toLowerCase()} useTooltip />
          <h1 className={styles.pageTitle}>{service.name}</h1>
          {service.alias && <CodeSpan>{`@${service.alias}`}</CodeSpan>}
          {environmentName && environmentUrl && (
            <Link className={styles.parent} to={environmentUrl}>
              {t('common:nouns.in')} {environmentName}
            </Link>
          )}
        </div>
        <p className={styles.descriptor}>
          {[
            t(`services:descriptor.type.${service.type}`, { defaultValue: service.type }),
            t(`services:descriptor.exposure.${service.exposureMode}`, {
              defaultValue: service.exposureMode,
            }),
          ].join(' · ')}
        </p>
        {hasMeta && (
          <div className={styles.meta}>
            {binding && (
              <span className={styles.metaItem}>
                <GitBranch size={14} aria-hidden />
                <span className={styles.metaLabel}>{t('services:repositoryBound.source')}</span>
                <CodeSpan copyable>
                  {`${binding.repository}@${binding.branch}:${binding.filePath}`}
                </CodeSpan>
              </span>
            )}
            {registry?.containerName && (
              <span className={styles.metaItem}>
                <span className={styles.metaLabel}>{t('services:containerName')}</span>
                <CodeSpan copyable>{registry.containerName}</CodeSpan>
              </span>
            )}
            {registry?.ipAddress && (
              <span className={styles.metaItem}>
                <span className={styles.metaLabel}>{t('services:internalIp')}</span>
                <CodeSpan copyable>{registry.ipAddress}</CodeSpan>
              </span>
            )}
          </div>
        )}
      </div>
      <div className={styles.actions}>
        {canExportService && (
          <Button variant="ghost" size="sm" icon={<Download size={16} />} onClick={onExport}>
            {t('services:export.button')}
          </Button>
        )}
        {canUpdateService && (
          <Button
            variant={isConfigOpen ? 'ghost' : 'secondary'}
            size="sm"
            icon={isConfigOpen ? undefined : <Settings size={16} />}
            onClick={onConfigToggle}
          >
            {isConfigOpen ? t('common:labels.closeSettings') : t('common:labels.settings')}
          </Button>
        )}
        {canDeployService && canControl && (
          <>
            <Button
              variant="ghost"
              size="sm"
              icon={<RotateCw size={16} />}
              onClick={onRestart}
              disabled={busy}
              isLoading={actionLoading === 'restart'}
            >
              {t('services:restart')}
            </Button>
            <Button
              variant="ghost"
              size="sm"
              icon={<Square size={16} />}
              onClick={onStop}
              disabled={busy}
              isLoading={actionLoading === 'stop'}
            >
              {t('services:stop')}
            </Button>
          </>
        )}
        {canDeployService && (
          <Button
            variant="primary"
            size="sm"
            icon={<Play size={16} />}
            onClick={onDeploy}
            disabled={busy}
            isLoading={actionLoading === 'deploy'}
          >
            {isRunning ? t('services:redeploy') : t('services:deploy')}
          </Button>
        )}
      </div>
    </div>
  );
}
