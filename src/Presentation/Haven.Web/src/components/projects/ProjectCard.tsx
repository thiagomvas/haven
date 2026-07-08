import { Edit2 } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';

import { ProjectDto } from '@/api/types';
import styles from '@/styles/components/projects/ProjectCard.module.css';

import { Card, CardContent, CardHeader } from '../ui/Card';

interface ProjectCardProps {
  project: ProjectDto;
  onEdit?: (project: ProjectDto) => void;
}

export function ProjectCard({ project, onEdit }: ProjectCardProps) {
  const navigate = useNavigate();
  const { t } = useTranslation('projects');

  const handleClick = () => {
    navigate(`/projects/${project.id}`);
  };

  const handleEdit = (e: React.MouseEvent) => {
    e.stopPropagation();
    onEdit?.(project);
  };

  return (
    <Card
      className={styles.projectCard}
      onClick={handleClick}
      role="button"
      tabIndex={0}
      onKeyDown={e => {
        if (e.key === 'Enter' || e.key === ' ') {
          handleClick();
        }
      }}
    >
      <CardHeader>
        <div className={styles.headerWithAction}>
          <h3 className={styles.title}>{project.name}</h3>
          {onEdit && (
            <button
              className={styles.editButton}
              onClick={handleEdit}
              title={t('edit')}
              aria-label={`${t('edit')} ${project.name}`}
            >
              <Edit2 size={18} />
            </button>
          )}
        </div>
      </CardHeader>
      <CardContent>
        <p className={styles.description}>{project.description || t('noDescription')}</p>
        <div className={styles.stats}>
          <div className={styles.stat}>
            <span className={styles.statLabel}>{t('environments')}</span>
            <span className={styles.statValue}>{project.environmentCount}</span>
          </div>
          <div className={styles.stat}>
            <span className={styles.statLabel}>{t('services')}</span>
            <span className={styles.statValue}>{project.serviceCount}</span>
          </div>
        </div>
      </CardContent>
    </Card>
  );
}
