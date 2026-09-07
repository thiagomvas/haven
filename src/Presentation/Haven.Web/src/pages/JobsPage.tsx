import { Clock, RefreshCw, Timer } from 'lucide-react';
import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';

import { JobInfoDto } from '@/api/types';
import {
  Row,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/layout';
import { Badge } from '@/components/ui/Badge';
import { Button } from '@/components/ui/Button';
import { Card, CardContent, CardHeader } from '@/components/ui/Card';
import { ErrorAlert } from '@/components/ui/ErrorAlert';
import { Spinner } from '@/components/ui/Spinner';
import { StatGrid } from '@/components/ui/StatGrid';
import { useJobs } from '@/hooks/useJobs';
import { useSetBreadcrumbs } from '@/hooks/useSetBreadcrumbs';
import { formatDate, formatRelative } from '@/lib/utils';
import styles from '@/styles/pages/JobsPage.module.css';

function sortJobs(jobs: JobInfoDto[]): JobInfoDto[] {
  return [...jobs].sort((a, b) => {
    if (!a.nextRunTime && !b.nextRunTime) return a.name.localeCompare(b.name);
    if (!a.nextRunTime) return 1;
    if (!b.nextRunTime) return -1;
    return new Date(a.nextRunTime).getTime() - new Date(b.nextRunTime).getTime();
  });
}

export function JobsPage() {
  const { t } = useTranslation('jobs');
  const { t: tCommon } = useTranslation('common');
  const { data, isLoading, isFetching, error, refetch } = useJobs();

  useSetBreadcrumbs([{ label: t('title') }]);

  const jobs = useMemo(() => sortJobs(data ?? []), [data]);

  const stats = useMemo(() => {
    const total = jobs.length;
    const scheduled = jobs.filter(j => !!j.nextRunTime).length;
    const neverRun = jobs.filter(j => !j.lastRunTime).length;
    const nextUp = jobs.find(j => !!j.nextRunTime) ?? null;
    return { total, scheduled, neverRun, nextUp };
  }, [jobs]);

  return (
    <Stack gap="6" className={styles.container}>
      <div className={styles.header}>
        <div>
          <h1 className={styles.title}>{t('title')}</h1>
          <p className={styles.subtitle}>{t('subtitle')}</p>
        </div>
        <Button
          variant="outline"
          size="sm"
          icon={<RefreshCw size={14} className={isFetching ? styles.spinning : undefined} />}
          onClick={() => refetch()}
          disabled={isFetching}
        >
          {t('refresh')}
        </Button>
      </div>

      {!error && !isLoading && jobs.length > 0 && (
        <StatGrid
          items={[
            { label: t('stats.total'), value: stats.total },
            { label: t('stats.scheduled'), value: stats.scheduled },
            { label: t('stats.neverRun'), value: stats.neverRun },
            {
              label: t('stats.nextUp'),
              value: stats.nextUp ? (
                <span
                  className={styles.nextUpValue}
                  title={
                    stats.nextUp.nextRunTime ? formatDate(stats.nextUp.nextRunTime) : undefined
                  }
                >
                  {formatRelative(stats.nextUp.nextRunTime!, tCommon)}
                </span>
              ) : (
                '—'
              ),
            },
          ]}
        />
      )}

      {error && <ErrorAlert message={t('error')} variant="block" />}

      {!error && isLoading && (
        <div className={styles.spinner}>
          <Spinner size="lg" />
          <p>{t('loading')}</p>
        </div>
      )}

      {!error && !isLoading && (
        <Card>
          <CardHeader>
            <h2 className={styles.sectionTitle}>{t('sectionTitle')}</h2>
          </CardHeader>
          <CardContent className={styles.tableContent}>
            {jobs.length === 0 ? (
              <p className={styles.emptyState}>{t('empty')}</p>
            ) : (
              <Table hoverable striped>
                <TableHead>
                  <TableRow isHeader>
                    <TableHeader>{t('table.name')}</TableHeader>
                    <TableHeader>{t('table.status')}</TableHeader>
                    <TableHeader>{t('table.lastRun')}</TableHeader>
                    <TableHeader>{t('table.nextRun')}</TableHeader>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {jobs.map(job => (
                    <TableRow key={job.key}>
                      <TableCell>
                        <Row gap="2" align="center">
                          <Timer size={15} className={styles.jobIcon} />
                          <span className={styles.jobName}>
                            {t(`names.${job.key}`, { defaultValue: job.name })}
                          </span>
                        </Row>
                      </TableCell>
                      <TableCell>
                        {job.nextRunTime ? (
                          <Badge variant="success">{t('status.scheduled')}</Badge>
                        ) : (
                          <Badge variant="default">{t('status.unscheduled')}</Badge>
                        )}
                      </TableCell>
                      <TableCell variant="muted" nowrap>
                        <span title={job.lastRunTime ? formatDate(job.lastRunTime) : undefined}>
                          {job.lastRunTime ? formatRelative(job.lastRunTime, tCommon) : t('never')}
                        </span>
                      </TableCell>
                      <TableCell variant="muted" nowrap>
                        <span title={job.nextRunTime ? formatDate(job.nextRunTime) : undefined}>
                          {job.nextRunTime ? (
                            <Row gap="1" align="center">
                              <Clock size={12} />
                              {formatRelative(job.nextRunTime, tCommon)}
                            </Row>
                          ) : (
                            t('notScheduled')
                          )}
                        </span>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )}
          </CardContent>
        </Card>
      )}
    </Stack>
  );
}
