import { useTranslation } from 'react-i18next';

import { ServiceDto } from '@/api/types';
import { Button } from '@/components/ui/Button';
import { Modal } from '@/components/ui/Modal';

import { BulkAction } from './BulkActionToolbar';

interface BulkActionConfirmModalProps {
  action: BulkAction | null;
  services: ServiceDto[];
  isSubmitting: boolean;
  error?: string;
  onCancel: () => void;
  onConfirm: () => void;
}

const titleKeyByAction: Record<BulkAction, string> = {
  deploy: 'bulkActions.confirmDeployTitle',
  restart: 'bulkActions.confirmRestartTitle',
  stop: 'bulkActions.confirmStopTitle',
};

const descriptionKeyByAction: Record<BulkAction, string> = {
  deploy: 'bulkActions.confirmDeployDescription',
  restart: 'bulkActions.confirmRestartDescription',
  stop: 'bulkActions.confirmStopDescription',
};

export function BulkActionConfirmModal({
  action,
  services,
  isSubmitting,
  error,
  onCancel,
  onConfirm,
}: BulkActionConfirmModalProps) {
  const { t } = useTranslation(['environments', 'common']);

  return (
    <Modal
      isOpen={action !== null}
      onClose={onCancel}
      title={
        action
          ? (t as (key: string, options?: object) => string)(titleKeyByAction[action], {
              count: services.length,
            })
          : undefined
      }
      description={action ? t(descriptionKeyByAction[action] as any) : undefined}
      error={error}
      size="sm"
      footer={
        <>
          <Button variant="ghost" onClick={onCancel} disabled={isSubmitting}>
            {t('common:actions.cancel')}
          </Button>
          <Button
            variant={action === 'stop' ? 'danger' : 'primary'}
            onClick={onConfirm}
            isLoading={isSubmitting}
          >
            {action ? t(`common:actions.${action}Selected` as any) : ''}
          </Button>
        </>
      }
    >
      <ul>
        {services.map(service => (
          <li key={service.id}>{service.name}</li>
        ))}
      </ul>
    </Modal>
  );
}
