import { clsx } from 'clsx';
import { ReactNode } from 'react';

import styles from '@/styles/components/layout/ConfigSection.module.css';

/** Vertical stack of ConfigSections with consistent rhythm and a readable max width. */
export function ConfigContent({ children }: { children: ReactNode }) {
  return <div className={styles.content}>{children}</div>;
}

interface ConfigSectionProps {
  title: ReactNode;
  description?: ReactNode;
  /** Controls aligned to the end of the header. */
  actions?: ReactNode;
  /** Footer row, typically the Save button; sits on a tinted strip. */
  footer?: ReactNode;
  tone?: 'default' | 'danger';
  /** Remove body padding so lists and tables run edge to edge. */
  flush?: boolean;
  children?: ReactNode;
}

/**
 * One titled group of settings: hairline panel, header with title and description,
 * body of fields or rows, optional footer for the commit action.
 */
export function ConfigSection({
  title,
  description,
  actions,
  footer,
  tone = 'default',
  flush = false,
  children,
}: ConfigSectionProps) {
  return (
    <section className={clsx(styles.section, tone === 'danger' && styles.danger)}>
      <header className={styles.header}>
        <div className={styles.heading}>
          <h3 className={styles.title}>{title}</h3>
          {description && <p className={styles.description}>{description}</p>}
        </div>
        {actions && <div className={styles.actions}>{actions}</div>}
      </header>
      {children && <div className={clsx(styles.body, flush && styles.flush)}>{children}</div>}
      {footer && <footer className={styles.footer}>{footer}</footer>}
    </section>
  );
}

interface ConfigActionProps {
  title: ReactNode;
  description?: ReactNode;
  /** The control that performs the action. */
  action: ReactNode;
}

/** A divided row inside a ConfigSection: what it does on the left, the button on the right. */
export function ConfigAction({ title, description, action }: ConfigActionProps) {
  return (
    <div className={styles.action}>
      <div className={styles.actionInfo}>
        <h4 className={styles.actionTitle}>{title}</h4>
        {description && <p className={styles.actionDescription}>{description}</p>}
      </div>
      <div className={styles.actionControl}>{action}</div>
    </div>
  );
}
