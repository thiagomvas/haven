import { useTranslation } from 'react-i18next';

import { EnvironmentVariableDto } from '@/api/types';
import styles from '@/styles/components/ui/EnvironmentVariablesCard.module.css';

import { Button } from './Button';
import { Panel } from './Panel';

interface EnvironmentVariablesCardProps {
  variables: EnvironmentVariableDto[];
  totalEnvVars: number;
  onViewAll?: () => void;
  notice?: string;
}

const PREVIEW_COUNT = 5;

export function EnvironmentVariablesCard({
  variables,
  totalEnvVars,
  onViewAll,
  notice,
}: EnvironmentVariablesCardProps) {
  const { t } = useTranslation('common');
  const preview = variables.slice(0, PREVIEW_COUNT);
  const hasMore = totalEnvVars > preview.length && !!onViewAll;

  return (
    <Panel
      flush
      title={t('labels.variables')}
      meta={totalEnvVars}
      actions={
        hasMore ? (
          <Button variant="text" size="xs" onClick={onViewAll}>
            {t('labels.viewAll')} ({totalEnvVars})
          </Button>
        ) : undefined
      }
    >
      {preview.length > 0 ? (
        <>
          <ul className={styles.list}>
            {preview.map(variable => (
              <li key={variable.key} className={styles.line}>
                <span className={styles.assignment}>
                  <span className={styles.key}>{variable.key}</span>
                  <span className={styles.equals}>=</span>
                  <span className={styles.value}>{variable.value}</span>
                </span>
                <span className={styles.scopeTag}>{variable.scope}</span>
              </li>
            ))}
          </ul>
          {hasMore && notice && <p className={styles.notice}>{notice}</p>}
        </>
      ) : (
        <p className={styles.emptyText}>{t('labels.noItems')}</p>
      )}
    </Panel>
  );
}
