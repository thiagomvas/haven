import { useTranslation } from 'react-i18next';
import styles from './HealthIndicator.module.css';
import { Tooltip } from './Tooltip';

interface HealthIndicatorProps {
  health: 'healthy' | 'degraded' | 'stopped' | 'died' | 'muted' | 'running' | 'unknown' | string;
  useTooltip?: boolean;
}

export function HealthIndicator({ health, useTooltip }: HealthIndicatorProps) {
  const { t } = useTranslation('common');
  const healthTooltip = t(`health.${health}`, { defaultValue: health });

  if (!useTooltip) {
    return <span className={`${styles[health]} ${styles.indicator} `} />;
  }

  return (
    <Tooltip content={healthTooltip}>
      <span className={`${styles[health]} ${styles.indicator} `} />
    </Tooltip>
  );
}
