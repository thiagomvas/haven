import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import { GitCredentialDto } from '@/api/types';
import { Button } from '@/components/ui/Button';
import { FormGroup, FormInput, FormLabel } from '@/components/ui/Form';
import { Modal } from '@/components/ui/Modal';
import { useUpdateGitCredential } from '@/hooks/useGitCredentials';

interface EditGitCredentialModalProps {
  credential: GitCredentialDto | null;
  onClose: () => void;
}

export function EditGitCredentialModal({ credential, onClose }: EditGitCredentialModalProps) {
  const { t } = useTranslation('gitCredentials');
  const updateMutation = useUpdateGitCredential();

  const [displayName, setDisplayName] = useState(credential?.displayName ?? '');
  const [error, setError] = useState<string | null>(null);

  const handleSave = async () => {
    if (!credential) return;

    const trimmed = displayName.trim();
    if (!trimmed) return;

    try {
      await updateMutation.mutateAsync({ id: credential.id, data: { displayName: trimmed } });
      onClose();
    } catch (err) {
      setError(err instanceof Error ? err.message : t('edit.error'));
    }
  };

  return (
    <Modal
      isOpen={!!credential}
      onClose={onClose}
      title={t('edit.title')}
      size="sm"
      closeOnEscape={!updateMutation.isPending}
      closeOnBackdropClick={!updateMutation.isPending}
      error={error ?? undefined}
      footer={
        <>
          <Button variant="ghost" onClick={onClose} disabled={updateMutation.isPending}>
            {t('edit.cancel')}
          </Button>
          <Button
            onClick={handleSave}
            isLoading={updateMutation.isPending}
            disabled={!displayName.trim()}
          >
            {t('edit.save')}
          </Button>
        </>
      }
    >
      <FormGroup>
        <FormLabel htmlFor="gitCredentialDisplayName" required>
          {t('edit.displayName')}
        </FormLabel>
        <FormInput
          id="gitCredentialDisplayName"
          type="text"
          value={displayName}
          onChange={e => setDisplayName(e.target.value)}
          onKeyDown={e => e.key === 'Enter' && handleSave()}
          disabled={updateMutation.isPending}
          autoFocus
        />
      </FormGroup>
    </Modal>
  );
}
