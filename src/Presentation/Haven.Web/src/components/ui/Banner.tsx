import { AlertCircle, AlertTriangle, CheckCircle, Info } from 'lucide-react';
import { ReactNode } from 'react';

import styles from '@/styles/components/ui/Banner.module.css';

type BannerVariant = 'error' | 'success' | 'warning' | 'info';

interface BannerProps {
  variant?: BannerVariant;
  title?: string;
  description?: string;
  children?: ReactNode;
}

const icons: Record<BannerVariant, ReactNode> = {
  error: <AlertCircle size={18} />,
  success: <CheckCircle size={18} />,
  warning: <AlertTriangle size={18} />,
  info: <Info size={18} />,
};

export function Banner({ variant = 'info', title, description, children }: BannerProps) {
  return (
    <div className={`${styles.banner} ${styles[variant]}`}>
      <span className={styles.icon}>{icons[variant]}</span>
      <div className={styles.content}>
        {title && <p className={styles.title}>{title}</p>}
        {description && <p className={styles.description}>{description}</p>}
        {children}
      </div>
    </div>
  );
}
