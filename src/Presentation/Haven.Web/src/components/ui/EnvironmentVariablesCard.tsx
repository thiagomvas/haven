import { SquareAsterisk } from 'lucide-react';
import { useTranslation } from 'react-i18next';

import { EnvironmentVariableDto } from '@/api/types/environmentVariables.types';
import { Row } from '@/components/layout';

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

export function EnvironmentVariablesCard({
  variables,
  totalEnvVars,
  onViewAll,
  notice,
}: EnvironmentVariablesCardProps) {
  const { t } = useTranslation('common');

  return (
    <Card padding="var(--space-3)">
      <CardHeader>
        <CardTitle>
          <Row gap="2" align="center">
            <SquareAsterisk size={16} />
            {t('labels.variables')} <Chip variant="default" size="sm" content={totalEnvVars} />
          </Row>
        </CardTitle>
      </CardHeader>
      <CardContent>
        {variables.length > 0 ? (
          <div style={{ marginTop: 'var(--space-3)' }}>
            <table
              style={{
                width: '100%',
                borderCollapse: 'collapse',
                tableLayout: 'auto',
              }}
            >
              <tbody>
                {variables.slice(0, 5).map(variable => (
                  <tr key={variable.key} style={{ borderBottom: '1px solid var(--color-border)' }}>
                    <td
                      style={{
                        padding: 'var(--space-2)',
                        maxWidth: '120px',
                        overflow: 'hidden',
                        textOverflow: 'ellipsis',
                        whiteSpace: 'nowrap',
                      }}
                      title={variable.key}
                    >
                      {variable.key}
                    </td>
                    <td
                      style={{
                        padding: 'var(--space-2)',
                        maxWidth: '200px',
                        overflow: 'hidden',
                        textOverflow: 'ellipsis',
                        whiteSpace: 'nowrap',
                        textAlign: 'right',
                        color: 'var(--color-text-secondary)',
                      }}
                      title={variable.value}
                    >
                      {variable.value}
                    </td>
                    <td
                      style={{
                        padding: 'var(--space-2)',
                        width: 'fit-content',
                        textAlign: 'right',
                        color: 'var(--color-text-muted)',
                        fontSize: 'var(--font-size-xs)',
                        whiteSpace: 'nowrap',
                      }}
                    >
                      {variable.scope.toUpperCase()}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
            {variables.length > 5 && onViewAll && (
              <Row>
                <Button variant="secondary" size="sm" onClick={onViewAll}>
                  {t('labels.viewAll')} ({totalEnvVars})
                </Button>
                {notice && (
                  <p
                    style={{
                      marginTop: 'var(--space-2)',
                      color: 'var(--color-text-muted)',
                      fontSize: 'var(--font-size-xs)',
                    }}
                  >
                    {notice}
                  </p>
                )}
              </Row>
            )}
          </div>
        ) : (
          <p
            style={{
              padding: 'var(--space-3)',
              color: 'var(--color-text-secondary)',
              marginTop: 'var(--space-3)',
            }}
          >
            No variables yet.
          </p>
        )}
      </CardContent>
    </Card>
  );
}
