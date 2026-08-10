import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { Modal } from '@/components/ui/Modal';
import { useCreateNetwork } from '@/hooks/useNetworks';

interface CreateNetworkModalProps {
  isOpen: boolean;
  onClose: () => void;
}

export function CreateNetworkModal({ isOpen, onClose }: CreateNetworkModalProps) {
  const { t } = useTranslation(['networks', 'common']);
  const createMutation = useCreateNetwork();

  const [name, setName] = useState('');
  const [error, setError] = useState<string | undefined>(undefined);

  const isLoading = createMutation.isPending;

  const handleClose = () => {
    setName('');
    setError(undefined);
    onClose();
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(undefined);

    if (!name.trim()) {
      setError(t('createModal.nameRequired'));
      return;
    }

    try {
      await createMutation.mutateAsync({ name: name.trim() });
      handleClose();
    } catch (err) {
      setError(err instanceof Error ? err.message : t('createModal.error'));
    }
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={handleClose}
      title={t('createModal.title')}
      description={t('createModal.description')}
      size="sm"
      closeOnEscape={!isLoading}
      closeOnBackdropClick={!isLoading}
      error={error}
      footer={
        <>
          <Button variant="ghost" onClick={handleClose} disabled={isLoading}>
            {t('common:actions.cancel')}
          </Button>
          <Button
            type="submit"
            form="create-network-form"
            isLoading={isLoading}
            disabled={!name.trim()}
          >
            {t('createModal.submit')}
          </Button>
        </>
      }
    >
      <form id="create-network-form" onSubmit={handleSubmit}>
        <Input
          label={t('createModal.nameLabel')}
          value={name}
          onChange={e => setName(e.target.value)}
          placeholder={t('createModal.namePlaceholder')}
          disabled={isLoading}
          autoFocus
        />
      </form>
    </Modal>
  );
}
