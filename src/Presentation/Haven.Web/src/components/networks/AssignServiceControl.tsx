import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import { Row, Stack } from '@/components/layout';
import { Button } from '@/components/ui/Button';
import { ErrorAlert } from '@/components/ui/ErrorAlert';
import { Input } from '@/components/ui/Input';
import { Spinner } from '@/components/ui/Spinner';
import { useFuzzySearch } from '@/hooks/useFuzzySearch';
import { useAssignServiceToNetwork } from '@/hooks/useNetworks';
import styles from '@/styles/pages/NetworksPage.module.css';

interface AssignServiceControlProps {
  networkId: string;
  assignedServiceIds: Set<string>;
}

export function AssignServiceControl({ networkId, assignedServiceIds }: AssignServiceControlProps) {
  const { t } = useTranslation('networks');
  const [query, setQuery] = useState('');
  const [error, setError] = useState<string | undefined>(undefined);
  const { results, isLoading } = useFuzzySearch(query, 10, ['Service']);
  const assignMutation = useAssignServiceToNetwork();

  const serviceResults = results.filter(r => !assignedServiceIds.has(r.id));

  const handleAssign = async (serviceId: string) => {
    setError(undefined);
    try {
      await assignMutation.mutateAsync({ networkId, serviceId });
      setQuery('');
    } catch (err) {
      setError(err instanceof Error ? err.message : t('assign.assignError'));
    }
  };

  return (
    <Stack gap="2" className={styles.assignControl}>
      <Input
        value={query}
        onChange={e => {
          setQuery(e.target.value);
          setError(undefined);
        }}
        placeholder={t('assign.searchPlaceholder')}
      />
      {error && <ErrorAlert message={error} variant="block" />}
      {query.length > 0 && (
        <Stack gap="1" className={styles.assignResults}>
          {isLoading && <Spinner size="sm" />}
          {!isLoading && serviceResults.length === 0 && (
            <p className={styles.emptyServicesText}>{t('assign.noResults')}</p>
          )}
          {!isLoading &&
            serviceResults.map(result => (
              <Row key={result.id} align="center" className={styles.assignResultRow}>
                <span className={styles.serviceName}>{result.label}</span>
                <Button
                  variant="secondary"
                  size="sm"
                  isLoading={assignMutation.isPending}
                  onClick={() => handleAssign(result.id)}
                >
                  {t('assign.assignButton')}
                </Button>
              </Row>
            ))}
        </Stack>
      )}
    </Stack>
  );
}
