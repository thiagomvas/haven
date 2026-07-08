import { Square } from 'lucide-react';
import { useCallback, useEffect, useRef, useState } from 'react';

import { servicesApi } from '@/api/services';
import { DeploymentDto } from '@/api/types';
import { DeploymentStatus } from '@/api/types';
import { Row, Stack } from '@/components/layout';
import { Badge } from '@/components/ui/Badge';
import { Button } from '@/components/ui/Button';
import { Card } from '@/components/ui/Card';
import { Label } from '@/components/ui/Label';
import { Spinner } from '@/components/ui/Spinner';
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

function statusVariant(status: DeploymentStatus): 'success' | 'danger' | 'warning' | 'default' {
  switch (status) {
    case 'Succeeded':
      return 'success';
    case 'Failed':
      return 'danger';
    case 'Cancelled':
      return 'default';
    case 'InProgress':
      return 'warning';
  }
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
          <span>Waiting for log entries...</span>
        </div>
      )}
      {!loadingLogs && isEmpty && !isActive && (
        <div className={styles.logEmpty}>
          <span>No logs found for this deployment.</span>
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
  const [deployments, setDeployments] = useState<DeploymentDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [cancelling, setCancelling] = useState(false);
  const [refreshCount, setRefreshCount] = useState(0);
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
          setError(err instanceof Error ? err.message : 'Failed to load deployments');
          setLoading(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [projectId, environmentId, serviceId, refreshCount]);

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
      setError(err instanceof Error ? err.message : 'Failed to cancel deployment');
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
        <Label variant="error">{error}</Label>
      </div>
    );
  }

  if (deployments.length === 0) {
    return (
      <div className={styles.center}>
        <Label variant="secondary">No deployments yet.</Label>
      </div>
    );
  }

  const selected = deployments.find(d => d.id === selectedId);

  return (
    <div className={styles.layout}>
      <div className={styles.sidebar}>
        {deployments.map(deployment => (
          <button
            key={deployment.id}
            className={`${styles.deploymentRow} ${selectedId === deployment.id ? styles.selected : ''}`}
            onClick={() => setSelectedId(deployment.id)}
          >
            <Stack gap="1">
              <Row gap="2" align="center">
                <Badge variant={statusVariant(deployment.status)}>{deployment.status}</Badge>
                {deployment.triggeredBy && (
                  <Label variant="secondary" size="xs">
                    by {deployment.triggeredBy}
                  </Label>
                )}
              </Row>
              <Label variant="secondary" size="xs">
                {new Date(deployment.startedAt).toLocaleString()}
              </Label>
              <Label variant="secondary" size="xs">
                {formatDuration(deployment.startedAt, deployment.finishedAt ?? undefined)}
              </Label>
            </Stack>
          </button>
        ))}
      </div>

      <div className={styles.detail}>
        {selected && (
          <Card padding="var(--space-4)" style={{ height: '100%' }}>
            <Stack gap="3" style={{ height: '100%' }}>
              <Row gap="3" align="center">
                <Badge variant={statusVariant(selected.status)}>{selected.status}</Badge>
                <Label variant="secondary" size="sm">
                  Started {new Date(selected.startedAt).toLocaleString()}
                </Label>
                {selected.finishedAt && (
                  <Label variant="secondary" size="sm">
                    · {formatDuration(selected.startedAt, selected.finishedAt)}
                  </Label>
                )}
                {canDeploy && selected.status === 'InProgress' && (
                  <Button
                    variant="danger"
                    size="sm"
                    icon={<Square size={14} />}
                    onClick={() => handleCancel(selected.id)}
                    isLoading={cancelling}
                    disabled={cancelling}
                  >
                    Cancel
                  </Button>
                )}
              </Row>
              <DeploymentLogViewer
                deploymentId={selected.id}
                isActive={selected.status === 'InProgress'}
              />
            </Stack>
          </Card>
        )}
      </div>
    </div>
  );
}
