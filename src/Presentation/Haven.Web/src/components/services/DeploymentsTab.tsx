import { Square } from 'lucide-react';
import { useCallback, useEffect, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';

import { servicesApi } from '@/api/services';
import { DeploymentDto } from '@/api/types';
import { DeploymentStatus } from '@/api/types';
import { DeploymentIdentifier } from '@/components/services/DeploymentIdentifier';
import { Button } from '@/components/ui/Button';
import { Panel } from '@/components/ui/Panel';
import { Spinner } from '@/components/ui/Spinner';
import { MarkState, StatusMark } from '@/components/ui/StatusMark';
import { usePermission } from '@/hooks/usePermission';
import { deploymentLogsHub, serviceStatusHub } from '@/lib/signalr/hubs';
import {
  DeploymentLogEntry,
  useSubscribeToDeploymentLogs,
} from '@/lib/signalr/useSubscribeToDeploymentLogs';
import { useSubscribeToServiceUpdates } from '@/lib/signalr/useSubscribeToServiceUpdates';
import styles from '@/styles/components/services/DeploymentsTab.module.css';

interface DeploymentsTabProps {
  projectId: string;
  environmentId: string;
  serviceId: string;
}

const MARK: Record<DeploymentStatus, MarkState> = {
  Succeeded: 'running',
  Failed: 'fault',
  Cancelled: 'stopped',
  Queued: 'pending',
  Building: 'deploying',
  Starting: 'deploying',
};

function DeploymentState({ status }: { status: DeploymentStatus }) {
  return (
    <span className={styles.state}>
      <StatusMark state={MARK[status]} size={12} />
      {status}
    </span>
  );
}

function isDeploymentActive(status: DeploymentStatus): boolean {
  return status === 'Queued' || status === 'Building' || status === 'Starting';
}

function formatDuration(start: string, end?: string): string {
  const startMs = new Date(start).getTime();
  const endMs = end ? new Date(end).getTime() : Date.now();
  const seconds = Math.floor((endMs - startMs) / 1000);
  if (seconds < 60) return `${seconds}s`;
  const minutes = Math.floor(seconds / 60);
  const remainingSeconds = seconds % 60;
  return `${minutes}m ${remainingSeconds}s`;
}

function DeploymentLogViewer({
  deploymentId,
  isActive,
}: {
  deploymentId: string;
  isActive: boolean;
}) {
  const { t } = useTranslation('services');
  const [historicLines, setHistoricLines] = useState<string[] | null>(null);
  const [liveEntries, setLiveEntries] = useState<DeploymentLogEntry[]>([]);
  const bottomRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    let cancelled = false;

    servicesApi
      .getDeploymentLogs(deploymentId)
      .then(lines => {
        if (!cancelled) setHistoricLines(lines ?? []);
      })
      .catch(err => {
        if (!cancelled) {
          console.error(err);
          setHistoricLines([]);
        }
      });

    return () => {
      cancelled = true;
      setHistoricLines(null);
      setLiveEntries([]);
    };
  }, [deploymentId]);

  const handleLogEntry = useCallback((entry: DeploymentLogEntry) => {
    setLiveEntries(prev => [...prev, entry]);
  }, []);

  useSubscribeToDeploymentLogs(
    deploymentLogsHub,
    isActive ? deploymentId : undefined,
    handleLogEntry
  );

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [historicLines, liveEntries]);

  const loadingLogs = historicLines === null;
  const isEmpty = (historicLines?.length ?? 0) === 0 && liveEntries.length === 0;

  return (
    <div className={styles.logViewer}>
      {loadingLogs && (
        <div className={styles.logEmpty}>
          <Spinner />
        </div>
      )}
      {!loadingLogs && isEmpty && isActive && (
        <div className={styles.logEmpty}>
          <Spinner />
          <span>{t('deploymentsTab.waitingForLogs')}</span>
        </div>
      )}
      {!loadingLogs && isEmpty && !isActive && (
        <div className={styles.logEmpty}>
          <span>{t('deploymentsTab.noLogs')}</span>
        </div>
      )}
      {(historicLines ?? []).map((line, i) => (
        <div key={`h-${i}`} className={styles.logLine}>
          <span className={styles.logMessage}>{line}</span>
        </div>
      ))}
      {liveEntries.map((entry, i) => (
        <div key={`l-${i}`} className={styles.logLine}>
          <span className={styles.logTimestamp}>
            {new Date(entry.timestamp).toLocaleTimeString()}
          </span>
          <span className={styles.logMessage}>{entry.message}</span>
        </div>
      ))}
      <div ref={bottomRef} />
    </div>
  );
}

export function DeploymentsTab({ projectId, environmentId, serviceId }: DeploymentsTabProps) {
  const { t } = useTranslation('services');
  const [deployments, setDeployments] = useState<DeploymentDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [cancelling, setCancelling] = useState(false);
  const [refreshCount, setRefreshCount] = useState(0);
  const [, setTick] = useState(0);
  const canDeploy = usePermission('projects.manage_deploys');

  useEffect(() => {
    let cancelled = false;

    servicesApi
      .getDeployments(projectId, environmentId, serviceId)
      .then(data => {
        if (cancelled) return;
        setDeployments(data ?? []);
        setSelectedId(prev => prev ?? (data && data.length > 0 ? data[0].id : null));
        setLoading(false);
      })
      .catch(err => {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : t('deploymentsTab.loadFailed'));
          setLoading(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [projectId, environmentId, serviceId, refreshCount, t]);

  const hasActive = deployments.some(d => !d.finishedAt && isDeploymentActive(d.status));

  useEffect(() => {
    if (!hasActive) return;
    const id = setInterval(() => setTick(n => n + 1), 1000);
    return () => clearInterval(id);
  }, [hasActive]);

  useSubscribeToServiceUpdates(
    serviceStatusHub,
    serviceId,
    useCallback(() => setRefreshCount(c => c + 1), [])
  );

  const handleCancel = async (deploymentId: string) => {
    try {
      setCancelling(true);
      await servicesApi.cancelDeployment(deploymentId);
    } catch (err) {
      setError(err instanceof Error ? err.message : t('deploymentsTab.cancelFailed'));
    } finally {
      setCancelling(false);
    }
  };

  if (loading) {
    return (
      <div className={styles.center}>
        <Spinner />
      </div>
    );
  }

  if (error) {
    return (
      <div className={styles.center}>
        <p className={styles.error}>{error}</p>
      </div>
    );
  }

  if (deployments.length === 0) {
    return (
      <div className={styles.center}>
        <p className={styles.muted}>{t('deploymentsTab.empty')}</p>
      </div>
    );
  }

  const selected = deployments.find(d => d.id === selectedId);

  return (
    <div className={styles.layout}>
      <Panel flush title={t('deployments')} meta={deployments.length} className={styles.listPanel}>
        <ul className={styles.list}>
          {deployments.map(deployment => (
            <li key={deployment.id}>
              <button
                type="button"
                className={`${styles.deploymentRow} ${selectedId === deployment.id ? styles.selected : ''}`}
                aria-current={selectedId === deployment.id ? 'true' : undefined}
                onClick={() => setSelectedId(deployment.id)}
              >
                <span className={styles.rowTop}>
                  <DeploymentState status={deployment.status} />
                  <span className={styles.muted}>
                    {formatDuration(deployment.startedAt, deployment.finishedAt ?? undefined)}
                  </span>
                </span>
                <DeploymentIdentifier deployment={deployment} />
                <span className={styles.muted}>
                  {new Date(deployment.startedAt).toLocaleString()}
                  {deployment.triggeredBy &&
                    ` · ${t('deploymentsTab.by', { user: deployment.triggeredBy })}`}
                </span>
              </button>
            </li>
          ))}
        </ul>
      </Panel>

      {selected ? (
        <Panel
          className={styles.detailPanel}
          title={<DeploymentState status={selected.status} />}
          meta={
            <>
              {t('deploymentsTab.startedAt', {
                date: new Date(selected.startedAt).toLocaleString(),
              })}
              {` · ${formatDuration(selected.startedAt, selected.finishedAt ?? undefined)}`}
              {selected.triggeredBy &&
                ` · ${t('deploymentsTab.by', { user: selected.triggeredBy })}`}
            </>
          }
          actions={
            <>
              <DeploymentIdentifier deployment={selected} variant="inline" />
              {canDeploy && isDeploymentActive(selected.status) && (
                <Button
                  variant="danger"
                  size="xs"
                  icon={<Square size={14} />}
                  onClick={() => handleCancel(selected.id)}
                  isLoading={cancelling}
                  disabled={cancelling}
                >
                  {t('deploymentsTab.cancel')}
                </Button>
              )}
            </>
          }
        >
          <DeploymentLogViewer
            deploymentId={selected.id}
            isActive={isDeploymentActive(selected.status)}
          />
        </Panel>
      ) : (
        <p className={styles.muted}>{t('deploymentsTab.selectPrompt')}</p>
      )}
    </div>
  );
}
