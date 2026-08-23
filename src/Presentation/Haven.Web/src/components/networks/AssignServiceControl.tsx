import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import { Row, Stack } from '@/components/layout';
import { Button } from '@/components/ui/Button';
import { ErrorAlert } from '@/components/ui/ErrorAlert';
import { Input } from '@/components/ui/Input';
import { Spinner } from '@/components/ui/Spinner';
import { useAssignServiceToNetwork, useAttachableServices } from '@/hooks/useNetworks';
import styles from '@/styles/pages/NetworksPage.module.css';

interface AssignServiceControlProps {
  networkId: string;
}

export function AssignServiceControl({ networkId }: AssignServiceControlProps) {
  const { t } = useTranslation('networks');
  const [query, setQuery] = useState('');
  const [isFocused, setIsFocused] = useState(false);
  const [error, setError] = useState<string | undefined>(undefined);
  const { results, isLoading } = useAttachableServices(networkId, query);
  const assignMutation = useAssignServiceToNetwork();

  const showResults = isFocused || query.length > 0;

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
        onFocus={() => setIsFocused(true)}
        onBlur={() => setTimeout(() => setIsFocused(false), 150)}
        placeholder={t('assign.searchPlaceholder')}
      />
      {error && <ErrorAlert message={error} variant="block" />}
      {showResults && (
        <Stack gap="1" className={styles.assignResults}>
          {isLoading && <Spinner size="sm" />}
          {!isLoading && results.length === 0 && (
            <p className={styles.emptyServicesText}>{t('assign.noResults')}</p>
          )}
          {!isLoading &&
            results.map(result => (
              <Row key={result.id} align="center" className={styles.assignResultRow}>
                <Stack gap="1" className={styles.assignResultInfo}>
                  <span className={styles.serviceName}>{result.name}</span>
                  <span className={styles.assignResultMeta}>
                    {t('assign.resultMeta', {
                      project: result.projectName,
                      environment: result.environmentName,
                    })}
                  </span>
                </Stack>
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
