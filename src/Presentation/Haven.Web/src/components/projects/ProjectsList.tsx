import { Edit2 } from 'lucide-react';
import { useTranslation } from 'react-i18next';

import type { ProjectDashboardDto } from '@/api/types';
import { Card, CardContent, CardHeader } from '@/components/ui/Card';
import type { EnvironmentStatus } from '@/components/ui/EnvironmentStatusChip';
import { EnvironmentStatusChip } from '@/components/ui/EnvironmentStatusChip';
import { ProjectAvatar } from '@/components/ui/ProjectAvatar';
import { formatRelative, getStatusColor } from '@/lib/utils';
import styles from '@/styles/components/projects/ProjectsList.module.css';

interface ProjectsListProps {
  projects: ProjectDashboardDto[];
  onRowClick: (projectId: string) => void;
  onEdit?: (project: ProjectDashboardDto) => void;
  isLoading?: boolean;
}

function getEnvironmentStatus(project: ProjectDashboardDto, envId: string): EnvironmentStatus {
  const env = project.environments.find(e => e.id === envId);
  if (!env) return 'empty';
  if (env.serviceStatistics.running === 0) return 'stopped';
  if (env.serviceStatistics.running === env.serviceStatistics.total) return 'running';
  return 'partial';
}

function getProjectServiceStatus(project: ProjectDashboardDto): EnvironmentStatus {
  if (project.serviceStatistics.total === 0) return 'empty';
  if (project.serviceStatistics.running === 0) return 'stopped';
  if (project.serviceStatistics.running === project.serviceStatistics.total) return 'running';
  return 'partial';
}

export function ProjectsList({ projects, onRowClick, onEdit, isLoading }: ProjectsListProps) {
  const { t: tCommon } = useTranslation('common');
  const { t } = useTranslation('projects');

  if (isLoading) {
    return <div className={styles.loadingState}>Loading projects...</div>;
  }

  if (!projects || projects.length === 0) {
    return <div className={styles.emptyState}>{t('emptyState')}</div>;
  }

  return (
    <Card>
      <CardHeader>
        <h2 className={styles.sectionTitle}>{tCommon('labels.projects')}</h2>
      </CardHeader>
      <CardContent className={styles.tableContent}>
        <table className={styles.projectsTable}>
          <thead>
            <tr>
              <th>{tCommon('labels.project')}</th>
              <th>{tCommon('labels.environments')}</th>
              <th>{tCommon('labels.services')}</th>
              <th>Last Deploy</th>
              {onEdit && <th style={{ width: '40px' }}></th>}
            </tr>
          </thead>
          <tbody>
            {projects.map(project => (
              <tr
                key={project.id}
                className={styles.tableRow}
                onClick={() => onRowClick(project.id)}
              >
                <td className={styles.projectCell}>
                  <div onClick={() => onRowClick(project.id)} className={styles.clickableCell}>
                    <ProjectAvatar
                      name={project.name}
                      description={project.description}
                      showText={true}
                    />
                  </div>
                </td>
                <td className={styles.environmentsCell}>
                  <div className={styles.environmentsList}>
                    {project.environments.map(env => (
                      <EnvironmentStatusChip
                        key={env.id}
                        name={env.name}
                        status={getEnvironmentStatus(project, env.id)}
                      />
                    ))}
                  </div>
                </td>
                <td className={styles.servicesCell}>
                  <span className={styles[getProjectServiceStatus(project)]}>
                    {project.serviceStatistics.running}
                  </span>
                  /{project.serviceStatistics.total}
                </td>
                <td className={styles.deployCell}>
                  <div className={styles.deployContent}>
                    <span>
                      {project.lastDeployedAt
                        ? formatRelative(project.lastDeployedAt, tCommon)
                        : '—'}
                    </span>
                    <span className={styles.clickIndicator} onClick={() => onRowClick(project.id)}>
                      →
                    </span>
                  </div>
                </td>
                {onEdit && (
                  <td className={styles.actionCell}>
                    <button
                      className={styles.editButton}
                      onClick={e => {
                        e.stopPropagation();
                        onEdit(project);
                      }}
                      title={t('edit')}
                      aria-label={`${t('edit')} ${project.name}`}
                    >
                      <Edit2 size={18} />
                    </button>
                  </td>
                )}
              </tr>
            ))}
          </tbody>
        </table>
      </CardContent>
    </Card>
  );
}
