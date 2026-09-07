import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import { BulkServiceActionResponse, ServiceDto } from '@/api/types';
import { Banner } from '@/components/ui/Banner';
import { Button } from '@/components/ui/Button';

interface BulkActionResultBannerProps {
  result: BulkServiceActionResponse;
  services: ServiceDto[];
  onDismiss: () => void;
}

export function BulkActionResultBanner({
  result,
  services,
  onDismiss,
}: BulkActionResultBannerProps) {
  const { t } = useTranslation(['environments', 'common']);
  const [showFailures, setShowFailures] = useState(false);

  const { succeededCount, failedCount, results } = result;
  const total = succeededCount + failedCount;
  const failures = results.filter(r => !r.success);

  const variant = failedCount === 0 ? 'success' : succeededCount === 0 ? 'error' : 'warning';
  const summary =
    failedCount === 0
      ? t('bulkActions.resultSummaryAllSucceeded', { total })
      : succeededCount === 0
        ? t('bulkActions.resultSummaryAllFailed', { total })
        : t('bulkActions.resultSummary', { succeeded: succeededCount, total });

  const serviceName = (serviceId: string) =>
    services.find(s => s.id === serviceId)?.name ?? serviceId;

  return (
    <Banner variant={variant} title={summary}>
      {failures.length > 0 && (
        <Button variant="text" size="sm" onClick={() => setShowFailures(v => !v)}>
          {showFailures ? t('bulkActions.hideFailures') : t('bulkActions.viewFailures')}
        </Button>
      )}
      {showFailures && (
        <ul>
          {failures.map(f => (
            <li key={f.serviceId}>
              {serviceName(f.serviceId)}
              {f.errorMessage ? `: ${f.errorMessage}` : ''}
            </li>
          ))}
        </ul>
      )}
      <Button variant="text" size="sm" onClick={onDismiss}>
        {t('common:actions.dismiss')}
      </Button>
    </Banner>
  );
}
