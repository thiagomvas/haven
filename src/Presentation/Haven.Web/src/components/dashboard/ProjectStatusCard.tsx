import { useTranslation } from 'react-i18next';
import { Link, useNavigate } from 'react-router-dom';

import type { EnvironmentDashboardDto, ProjectDashboardDto } from '@/api/types';
import { Badge } from '@/components/ui/Badge';
import { BuoyStrip } from '@/components/ui/BuoyStrip';
import { Meter } from '@/components/ui/Meter';
import { formatRelative } from '@/lib/utils';
import { getEnvironmentInsight } from '@/mocks/dashboardInsights';
import styles from '@/styles/components/dashboard/ProjectStatusCard.module.css';

interface ProjectStatusCardProps {
  project: ProjectDashboardDto;
  environments: EnvironmentDashboardDto[];
}

function envTone(env: EnvironmentDashboardDto): 'ok' | 'warn' | 'off' | 'none' {
  const s = env.serviceStatistics;
  if (s.total === 0) return 'none';
  if (s.degraded > 0) return 'warn';
  if (s.running === 0) return 'off';
  return 'ok';
}

export function ProjectStatusCard({ project, environments }: ProjectStatusCardProps) {
  const { t } = useTranslation('dashboard');
  const { t: tCommon } = useTranslation('common');
  const navigate = useNavigate();
  const s = project.serviceStatistics;

  const badge =
    s.total === 0 ? (
      <Badge variant="default">{t('projects.empty')}</Badge>
    ) : s.degraded > 0 ? (
      <Badge variant="warning">{t('projects.degraded', { count: s.degraded })}</Badge>
    ) : s.deploying > 0 || s.deploymentPending > 0 ? (
      <Badge variant="primary">{t('projects.deploying')}</Badge>
    ) : s.running === 0 ? (
      <Badge variant="default">{tCommon('health.stopped')}</Badge>
    ) : (
      <Badge variant="success">{t('projects.healthy')}</Badge>
    );

  return (
    <section className={styles.card}>
      <header className={styles.header}>
        <h3 className={styles.name}>
          <Link to={`/projects/${project.id}`}>{project.name}</Link>
        </h3>
        {badge}
        <span className={styles.summary}>
          {t('projects.services', { count: s.total })} ·{' '}
          {t('projects.environments', { count: project.environments.length })}
        </span>
        <span className={styles.deploy}>
          {project.lastDeployedAt
            ? t('projects.lastDeploy', { time: formatRelative(project.lastDeployedAt, tCommon) })
            : t('projects.neverDeployed')}
        </span>
      </header>

      {environments.length > 0 ? (
        <div className={styles.table} role="table">
          <div className={`${styles.row} ${styles.columns}`} role="row">
            <span role="columnheader">{tCommon('labels.environment')}</span>
            <span role="columnheader">{t('environments.services')}</span>
            <span role="columnheader" className={styles.num}>
              {t('environments.up')}
            </span>
            <span role="columnheader" className={styles.num}>
              {t('projects.uptime24h')}
            </span>
            <span role="columnheader">{t('projects.cpu')}</span>
            <span role="columnheader" className={styles.num}>
              {t('projects.memory')}
            </span>
          </div>
          {environments.map(env => {
            const insight = getEnvironmentInsight(env.id, env.serviceStatistics.running);
            const tone = envTone(env);
            return (
              <div
                key={env.id}
                className={`${styles.row} ${styles.envRow}`}
                role="row"
                tabIndex={0}
                onClick={() => navigate(`/projects/${project.id}/environments/${env.id}`)}
                onKeyDown={e => {
                  if (e.key === 'Enter') navigate(`/projects/${project.id}/environments/${env.id}`);
                }}
              >
                <span className={styles.envName} role="cell">
                  {env.name}
                </span>
                <span role="cell">
                  <BuoyStrip stats={env.serviceStatistics} />
                </span>
                <span role="cell" className={`${styles.num} ${styles.up} ${styles[`up-${tone}`]}`}>
                  {env.serviceStatistics.running}
                  <i>/{env.serviceStatistics.total}</i>
                </span>
                <span
                  role="cell"
                  className={`${styles.num} ${insight.availability24hPercent < 99 ? styles.lowUptime : ''}`}
                >
                  {insight.availability24hPercent.toFixed(2)}%
                </span>
                <span role="cell" className={styles.cpu}>
                  <Meter value={Math.min(insight.cpuPercent, 100)} label={t('projects.cpu')} />
                  <span>{insight.cpuPercent}%</span>
                </span>
                <span role="cell" className={styles.num}>
                  {insight.memoryMb >= 1024
                    ? `${(insight.memoryMb / 1024).toFixed(1)} GB`
                    : `${insight.memoryMb} MB`}
                </span>
              </div>
            );
          })}
        </div>
      ) : (
        <p className={styles.empty}>
          {project.environments.length === 0
            ? t('projects.noEnvironments')
            : t('projects.filteredOut')}
        </p>
      )}
    </section>
  );
}
