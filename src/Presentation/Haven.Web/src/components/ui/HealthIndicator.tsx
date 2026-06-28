import { useTranslation } from 'react-i18next';
import styles from './HealthIndicator.module.css';
import { Tooltip } from './Tooltip';
import { HealthStatus } from "@/api/types/service.types";

interface HealthIndicatorProps {
  health:
    | HealthStatus
    | 'healthy'
    | 'degraded'
    | 'stopped'
    | 'died'
    | 'muted'
    | 'running'
    | 'unknown'
    | 'deploying'
    | 'deploymentpending'
    | string;
  useTooltip?: boolean;
  showLabel?: boolean;
}

export function HealthIndicator({ health, useTooltip, showLabel }: HealthIndicatorProps) {
  const { t } = useTranslation('common');
  const healthLabel = t(`health.${health.toLocaleLowerCase()}`, { defaultValue: health });

  const dot = <span className={`${styles[health.toLocaleLowerCase()]} ${styles.indicator}`} />;

  const content = showLabel ? (
    <span className={styles.withLabel}>
      {dot}
      <span>{healthLabel}</span>
    </span>
  ) : (
    dot
  );

  if (!useTooltip || showLabel) {
    return content;
  }

  return <Tooltip content={healthLabel}>{content}</Tooltip>;
}
