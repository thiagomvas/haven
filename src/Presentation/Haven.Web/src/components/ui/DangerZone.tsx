import { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';

import styles from './DangerZone.module.css';

interface DangerZoneProps {
  children: ReactNode;
}

export function DangerZone({ children }: DangerZoneProps) {
  const { t } = useTranslation('projects');

  return (
    <div className={styles.dangerZone}>
      <div className={styles.dangerZoneHeader}>
        <h3 className={styles.dangerZoneTitle}>{t('dangerZone')}</h3>
        <p className={styles.dangerZoneDescription}>{t('dangerZoneDescription')}</p>
      </div>
      <div className={styles.dangerZoneContent}>{children}</div>
    </div>
  );
}
