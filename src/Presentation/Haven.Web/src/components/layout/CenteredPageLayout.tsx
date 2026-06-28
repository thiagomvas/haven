import { ReactNode } from 'react';

import styles from './CenteredPageLayout.module.css';

interface CenteredPageLayoutProps {
  children: ReactNode;
  maxWidth?: number;
}

export function CenteredPageLayout({ children, maxWidth }: CenteredPageLayoutProps) {
  return (
    <div className={styles.container}>
      <div className={styles.inner} style={maxWidth ? { maxWidth } : undefined}>
        {children}
      </div>
    </div>
  );
}
