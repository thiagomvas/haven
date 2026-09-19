import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';

import { healthChecksApi } from '@/api/healthChecks';
import { HealthCheckDto, HealthCheckResultDto } from '@/api/types';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/layout';
import styles from '@/styles/components/services/HealthChecks.module.css';

import { Stack } from '../layout';
import { Badge } from '../ui/Badge';
import { Label } from '../ui/Label';
import { Modal } from '../ui/Modal';
import { Spinner } from '../ui/Spinner';
import { HealthCheckResultDetails } from './HealthCheckResultDetails';

interface HealthCheckHistoryModalProps {
  projectId: string;
  environmentId: string;
  serviceId: string;
  healthCheck: HealthCheckDto | null;
  onClose: () => void;
}

const STATUS_VARIANT: Record<HealthCheckResultDto['status'], 'success' | 'danger' | 'default'> = {
  Healthy: 'success',
  Unhealthy: 'danger',
  Unknown: 'default',
};

const dateFormatter = new Intl.DateTimeFormat(undefined, {
  dateStyle: 'medium',
  timeStyle: 'medium',
});

/** Recent runs of a health check, newest first; selecting a row shows what was observed. */
export function HealthCheckHistoryModal({
  projectId,
  environmentId,
  serviceId,
  healthCheck,
  onClose,
}: HealthCheckHistoryModalProps) {
  const { t } = useTranslation(['services']);
  const [results, setResults] = useState<HealthCheckResultDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [selectedId, setSelectedId] = useState<string | null>(null);

  const healthCheckId = healthCheck?.id;

  useEffect(() => {
    if (!healthCheckId) return;

    let cancelled = false;
    (async () => {
      try {
        setLoading(true);
        setError(null);
        setSelectedId(null);
        const data = await healthChecksApi.results(
          projectId,
          environmentId,
          serviceId,
          healthCheckId
        );
        if (!cancelled) setResults(data ?? []);
      } catch (err) {
        if (!cancelled) setError(err instanceof Error ? err.message : t('services:error'));
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [projectId, environmentId, serviceId, healthCheckId, t]);

  const selected = results.find(r => r.id === selectedId) ?? null;

  return (
    <Modal
      isOpen={!!healthCheck}
      onClose={onClose}
      title={t('services:healthChecks.history.title', { name: healthCheck?.name })}
      size="lg"
      error={error ?? undefined}
    >
      {loading ? (
        <Stack align="center">
          <Spinner />
        </Stack>
      ) : results.length === 0 ? (
        <Label variant="secondary" size="sm">
          {t('services:healthChecks.history.empty')}
        </Label>
      ) : (
        <div className={styles.historyList}>
          <Table hoverable compact>
            <TableHead>
              <TableRow isHeader>
                <TableHeader>{t('services:healthChecks.history.time')}</TableHeader>
                <TableHeader>{t('services:healthChecks.history.status')}</TableHeader>
                <TableHeader>{t('services:healthChecks.history.reason')}</TableHeader>
                <TableHeader>{t('services:healthChecks.history.duration')}</TableHeader>
              </TableRow>
            </TableHead>
            <TableBody>
              {results.map(result => (
                <TableRow
                  key={result.id}
                  highlight={result.id === selectedId}
                  onRowClick={() => setSelectedId(result.id === selectedId ? null : result.id)}
                >
                  <TableCell nowrap>{dateFormatter.format(new Date(result.ranAt))}</TableCell>
                  <TableCell>
                    <Badge variant={STATUS_VARIANT[result.status]}>{result.status}</Badge>
                  </TableCell>
                  <TableCell>
                    {result.reason === 'None'
                      ? '-'
                      : t(`services:healthChecks.reasons.${result.reason}`)}
                  </TableCell>
                  <TableCell nowrap>
                    {t('services:healthChecks.details.duration', { ms: result.durationMs })}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>

          {selected ? (
            <HealthCheckResultDetails outcome={selected} />
          ) : (
            <Label variant="muted" size="xs">
              {t('services:healthChecks.history.selectHint')}
            </Label>
          )}
        </div>
      )}
    </Modal>
  );
}
