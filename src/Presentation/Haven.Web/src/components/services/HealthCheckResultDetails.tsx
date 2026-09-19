import { useTranslation } from 'react-i18next';

import { HealthCheckFailureReason } from '@/api/types';
import styles from '@/styles/components/services/HealthChecks.module.css';

import { Badge } from '../ui/Badge';
import { CodeBlock } from '../ui/CodeBlock';
import { Label } from '../ui/Label';

export interface HealthCheckOutcome {
  reason: HealthCheckFailureReason;
  message?: string;
  durationMs?: number;
  httpStatusCode?: number;
  exitCode?: number;
  output?: string;
  attempts?: number;
}

interface HealthCheckResultDetailsProps {
  outcome: HealthCheckOutcome;
}

/** Explains a single health check run: why it failed, what was observed, and the raw output. */
export function HealthCheckResultDetails({ outcome }: HealthCheckResultDetailsProps) {
  const { t } = useTranslation(['services']);

  return (
    <div className={styles.details}>
      {outcome.message && (
        <Label variant="secondary" size="sm" className={styles.message}>
          {outcome.message}
        </Label>
      )}

      <div className={styles.facts}>
        {outcome.reason !== 'None' && (
          <Badge variant="danger">{t(`services:healthChecks.reasons.${outcome.reason}`)}</Badge>
        )}
        {outcome.httpStatusCode != null && (
          <Badge>
            {t('services:healthChecks.details.httpStatus', { code: outcome.httpStatusCode })}
          </Badge>
        )}
        {outcome.exitCode != null && (
          <Badge>{t('services:healthChecks.details.exitCode', { code: outcome.exitCode })}</Badge>
        )}
        {outcome.durationMs != null && (
          <Badge>{t('services:healthChecks.details.duration', { ms: outcome.durationMs })}</Badge>
        )}
        {outcome.attempts != null && outcome.attempts > 1 && (
          <Badge>{t('services:healthChecks.details.attempts', { count: outcome.attempts })}</Badge>
        )}
      </div>

      {outcome.output && (
        <CodeBlock
          className={styles.output}
          header={t('services:healthChecks.details.output')}
          code={outcome.output}
          copyable
        />
      )}
    </div>
  );
}
