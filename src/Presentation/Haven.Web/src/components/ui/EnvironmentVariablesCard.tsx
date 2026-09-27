import { SquareAsterisk } from 'lucide-react';
import { useTranslation } from 'react-i18next';

import { EnvironmentVariableDto } from '@/api/types';
import { Row, Stack } from '@/components/layout';
import styles from '@/styles/components/ui/EnvironmentVariablesCard.module.css';

import { Button } from './Button';
import { Card, CardContent, CardHeader } from './Card';
import { CardTitle } from './Card';
import { Chip } from './Chip';

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

  return (
    <Card padding="var(--space-4)">
      <CardHeader>
        <CardTitle>
          <Row gap="2" align="center">
            <SquareAsterisk size={16} />
            {t('labels.variables')}
            <Chip variant="default" size="sm" content={totalEnvVars} />
          </Row>
        </CardTitle>
      </CardHeader>
      <CardContent>
        {preview.length > 0 ? (
          <Stack gap="2">
            <div className={styles.list}>
              {preview.map(variable => (
                <div key={variable.key} className={styles.line}>
                  <span className={styles.assignment}>
                    <span className={styles.key}>{variable.key}</span>
                    <span className={styles.equals}>=</span>
                    <span className={styles.value}>{variable.value}</span>
                  </span>
                  <span className={styles.scopeTag}>{variable.scope}</span>
                </div>
              ))}
            </div>
            {totalEnvVars > preview.length && onViewAll && (
              <Row gap="2" align="center" justify="space-between" wrap>
                <Button variant="secondary" size="sm" onClick={onViewAll}>
                  {t('labels.viewAll')} ({totalEnvVars})
                </Button>
                {notice && <span className={styles.notice}>{notice}</span>}
              </Row>
            )}
          </Stack>
        ) : (
          <p className={styles.emptyText}>{t('labels.noItems')}</p>
        )}
      </CardContent>
    </Card>
  );
}
