import { ChevronRight } from 'lucide-react';
import { Link } from 'react-router-dom';

import { useBreadcrumbContext } from '@/context/BreadcrumbContext';

import styles from '@/styles/components/layout/Breadcrumb.module.css';

export function Breadcrumb() {
  const { breadcrumbs } = useBreadcrumbContext();

  if (!breadcrumbs.length) {
    return null;
  }

  return (
    <nav className={styles.breadcrumb} aria-label="breadcrumb">
      <ol className={styles.list}>
        {breadcrumbs.map((item, index) => {
          const isLast = index === breadcrumbs.length - 1;

          return (
            <li key={index} className={styles.item}>
              {index > 0 && <ChevronRight size={16} className={styles.separator} />}
              {item.to && !isLast ? (
                <Link to={item.to} className={styles.link}>
                  {item.label}
                </Link>
              ) : (
                <span className={`${styles.text} ${isLast ? styles.current : ''}`}>
                  {item.label}
                </span>
              )}
            </li>
          );
        })}
      </ol>
    </nav>
  );
}
