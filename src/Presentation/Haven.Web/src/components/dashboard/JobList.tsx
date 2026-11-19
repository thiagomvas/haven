import { DatabaseBackup, RefreshCw, ShieldCheck, Trash2 } from 'lucide-react';
import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';

import type { ScheduledJobDto, ScheduledJobKind } from '@/api/types';
import { StatusMark } from '@/components/ui/StatusMark';
import { formatRelative } from '@/lib/utils';
import styles from '@/styles/components/dashboard/JobList.module.css';

const ICONS: Record<ScheduledJobKind, ReactNode> = {
  backup: <DatabaseBackup size={16} />,
  cleanup: <Trash2 size={16} />,
  sync: <RefreshCw size={16} />,
  certificates: <ShieldCheck size={16} />,
};

interface JobListProps {
  jobs: ScheduledJobDto[];
}

export function JobList({ jobs }: JobListProps) {
  const { t } = useTranslation('dashboard');
  const { t: tCommon } = useTranslation('common');

  return (
    <ul className={styles.list}>
      {jobs.map(job => {
        const failed = job.lastStatus === 'Failed';
        return (
          <li key={job.key} className={styles.item}>
            <span className={styles.icon}>{ICONS[job.kind]}</span>
            <div className={styles.body}>
              <span className={styles.name}>{job.name}</span>
              <span className={`${styles.detail} ${failed ? styles.failed : ''}`}>
                {job.detail ?? t('jobs.noDetail')}
              </span>
            </div>
            <div className={styles.meta}>
              <span className={styles.last}>
                <StatusMark
                  state={failed ? 'fault' : 'running'}
                  size={11}
                  title={failed ? t('jobs.failed') : t('jobs.succeeded')}
                />
                {job.lastRunTime ? formatRelative(job.lastRunTime, tCommon) : '—'}
              </span>
              <span>
                {job.nextRunTime
                  ? t('jobs.next', { time: formatRelative(job.nextRunTime, tCommon) })
                  : ''}
              </span>
            </div>
          </li>
        );
      })}
    </ul>
  );
}
