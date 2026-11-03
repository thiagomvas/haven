import { useTranslation } from 'react-i18next';

import type { HostMetricDto } from '@/api/types';
import { Meter, meterTone } from '@/components/ui/Meter';
import styles from '@/styles/components/dashboard/HostMetricTile.module.css';

import { Sparkline } from './Sparkline';

interface HostMetricTileProps {
  metric: HostMetricDto;
}

export function HostMetricTile({ metric }: HostMetricTileProps) {
  const { t } = useTranslation('dashboard');
  const label = t(`host.metrics.${metric.key}`);
  const peak = Math.max(...metric.series);
  const tone = metric.percent !== undefined ? meterTone(metric.percent) : 'ok';
  const sparkTone = tone === 'danger' ? 'danger' : tone === 'warn' ? 'warn' : 'neutral';
  const figure = metric.unit === '%' ? `${metric.value}%` : `${metric.value} ${metric.unit}`;

  return (
    <div className={styles.tile}>
      <div className={styles.top}>
        <span className={styles.label}>{label}</span>
        <span className={styles.peak}>
          {t('host.peak', { value: `${peak}${metric.unit === '%' ? '%' : ` ${metric.unit}`}` })}
        </span>
      </div>
      <div className={styles.reading}>
        <span className={styles.value}>{figure}</span>
        {metric.capacity !== undefined && metric.key !== 'cpu' && (
          <span className={styles.capacity}>
            {t('host.of', { capacity: `${metric.capacity} ${metric.unit}` })}
          </span>
        )}
        {metric.key === 'cpu' && metric.capacity !== undefined && (
          <span className={styles.capacity}>{t('host.cores', { count: metric.capacity })}</span>
        )}
      </div>
      {metric.percent !== undefined ? (
        <Meter value={metric.percent} label={label} />
      ) : (
        <div className={styles.meterSpacer} />
      )}
      <Sparkline
        values={metric.series}
        tone={sparkTone}
        max={metric.unit === '%' ? 100 : metric.capacity}
        label={t('host.trendLabel', { metric: label })}
      />
    </div>
  );
}
