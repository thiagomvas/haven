import { useTranslation } from 'react-i18next';

import { HealthStatus } from '@/api/types';
import styles from '@/styles/components/ui/HealthIndicator.module.css';

import { StatusMark, toMarkState } from './StatusMark';
import { Tooltip } from './Tooltip';

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
    | 'crashed'
    | string;
  useTooltip?: boolean;
  showLabel?: boolean;
}

export function HealthIndicator({ health, useTooltip, showLabel }: HealthIndicatorProps) {
  const { t } = useTranslation('common');
  const healthLabel = t(`health.${health.toLocaleLowerCase()}`, { defaultValue: health });

  const key = health.toLocaleLowerCase();
  const dot = (
    <span className={`${styles.indicator} ${key === 'notdeployed' ? styles.notdeployed : ''}`}>
      <StatusMark state={toMarkState(key)} size={12} />
    </span>
  );

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
