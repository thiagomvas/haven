import { Check, Loader2, Minus, X } from 'lucide-react';

import styles from '@/styles/components/ui/ProgressSteps.module.css';

import { Tooltip } from './Tooltip';

export type ProgressStepStatus = 'pending' | 'running' | 'success' | 'failed' | 'skipped';

export interface ProgressStep {
  id: string;
  label: string;
  status: ProgressStepStatus;
  /** Shown in a tooltip when hovering the step's circle. */
  message?: string;
}

interface ProgressStepsProps {
  steps: ProgressStep[];
}

const icons: Record<ProgressStepStatus, React.ReactNode> = {
  pending: null,
  running: <Loader2 size={16} className={styles.spin} />,
  success: <Check size={16} />,
  failed: <X size={16} />,
  skipped: <Minus size={16} />,
};

export function ProgressSteps({ steps }: ProgressStepsProps) {
  return (
    <ol className={styles.steps}>
      {steps.map((step, index) => (
        <li key={step.id} className={`${styles.step} ${styles[step.status]}`}>
          {index > 0 && (
            <div
              className={`${styles.connector} ${
                steps[index - 1].status === 'success' ? styles.connectorDone : ''
              }`}
            />
          )}
          <Tooltip content={step.message ?? step.label} direction="above" wrap>
            <div className={styles.circle} tabIndex={0} aria-label={step.message ?? step.label}>
              {icons[step.status]}
            </div>
          </Tooltip>
          <span className={styles.label}>{step.label}</span>
        </li>
      ))}
    </ol>
  );
}
