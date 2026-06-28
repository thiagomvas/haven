import { AlertCircle } from 'lucide-react';

import styles from './ErrorAlert.module.css';

interface ErrorAlertProps {
  message: string;
  variant?: 'inline' | 'block';
}

export function ErrorAlert({ message, variant = 'inline' }: ErrorAlertProps) {
  return (
    <div className={`${styles.alert} ${styles[variant]}`}>
      <AlertCircle size={16} />
      {message}
    </div>
  );
}
