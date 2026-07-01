import { ReactNode } from 'react';

import styles from '@/styles/components/ui/FeaturePanel.module.css';

interface FeaturePanelProps {
  title: string;
  description?: string;
  icon?: ReactNode;
  action?: ReactNode;
  children?: ReactNode;
  empty?: boolean;
  emptyMessage?: string;
}

export function FeaturePanel({
  title,
  description,
  icon,
  action,
  children,
  empty = false,
  emptyMessage = 'No items',
}: FeaturePanelProps) {
  return (
    <div className={styles.panel}>
      <div className={styles.header}>
        <div className={styles.titleGroup}>
          {icon && <span className={styles.icon}>{icon}</span>}
          <div>
            <h3 className={styles.title}>{title}</h3>
            {description && <p className={styles.description}>{description}</p>}
          </div>
        </div>
        {action && <div className={styles.action}>{action}</div>}
      </div>
      {empty ? (
        <div className={styles.empty}>
          <p>{emptyMessage}</p>
        </div>
      ) : (
        <div className={styles.content}>{children}</div>
      )}
    </div>
  );
}
