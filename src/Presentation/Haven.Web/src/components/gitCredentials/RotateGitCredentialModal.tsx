import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import { GitCredentialDto } from '@/api/types';
import { Row } from '@/components/layout/Row';
import { Button } from '@/components/ui/Button';
import { FormGroup, FormInput, FormLabel, FormTextarea } from '@/components/ui/Form';
import { Modal } from '@/components/ui/Modal';
import { useRotateGitCredential } from '@/hooks/useGitCredentials';

interface RotateGitCredentialModalProps {
  credential: GitCredentialDto | null;
  onClose: () => void;
}

type ManualAuthMethod = 'Token' | 'Ssh';

export function RotateGitCredentialModal({ credential, onClose }: RotateGitCredentialModalProps) {
  const { t } = useTranslation(['gitCredentials', 'common']);
  const rotateMutation = useRotateGitCredential();

  const [authMethod, setAuthMethod] = useState<ManualAuthMethod>(
    credential?.authMethod === 'Ssh' ? 'Ssh' : 'Token'
  );
  const [primaryCredential, setPrimaryCredential] = useState('');
  const [secondaryCredential, setSecondaryCredential] = useState('');
  const [webhookSecret, setWebhookSecret] = useState('');
  const [error, setError] = useState<string | null>(null);

  const handleSave = async () => {
    if (!credential) return;
    setError(null);

    if (!primaryCredential.trim()) {
      setError(t('errors.primaryCredentialRequired'));
      return;
    }

    try {
      await rotateMutation.mutateAsync({
        id: credential.id,
        data: {
          authMethod,
          primaryCredential: primaryCredential.trim(),
          secondaryCredential:
            authMethod === 'Ssh' && secondaryCredential.trim() ? secondaryCredential : undefined,
          webhookSecret: webhookSecret.trim() || undefined,
        },
      });
      onClose();
    } catch (err) {
      setError(err instanceof Error ? err.message : t('rotate.error'));
    }
  };

  return (
    <Modal
      isOpen={!!credential}
      onClose={onClose}
      title={t('rotate.title')}
      description={t('rotate.description')}
      size="md"
      closeOnEscape={!rotateMutation.isPending}
      closeOnBackdropClick={!rotateMutation.isPending}
      error={error ?? undefined}
      footer={
        <>
          <Button variant="ghost" onClick={onClose} disabled={rotateMutation.isPending}>
            {t('common:actions.cancel')}
          </Button>
          <Button
            onClick={handleSave}
            isLoading={rotateMutation.isPending}
            disabled={!primaryCredential.trim()}
          >
            {t('rotate.save')}
          </Button>
        </>
      }
    >
      <FormGroup>
        <FormLabel>{t('form.authMethod')}</FormLabel>
        <Row gap="2">
          <Button
            type="button"
            size="sm"
            variant={authMethod === 'Token' ? 'primary' : 'outline'}
            onClick={() => setAuthMethod('Token')}
            disabled={rotateMutation.isPending}
          >
            {t('auth.token')}
          </Button>
          <Button
            type="button"
            size="sm"
            variant={authMethod === 'Ssh' ? 'primary' : 'outline'}
            onClick={() => setAuthMethod('Ssh')}
            disabled={rotateMutation.isPending}
          >
            {t('auth.ssh')}
          </Button>
        </Row>
      </FormGroup>

      {authMethod === 'Token' ? (
        <FormGroup>
          <FormLabel htmlFor="rotateToken" required>
            {t('form.token')}
          </FormLabel>
          <FormInput
            id="rotateToken"
            type="password"
            placeholder={t('form.tokenPlaceholder')}
            value={primaryCredential}
            onChange={e => setPrimaryCredential(e.target.value)}
            disabled={rotateMutation.isPending}
          />
        </FormGroup>
      ) : (
        <>
          <FormGroup>
            <FormLabel htmlFor="rotateSshKey" required>
              {t('form.sshKey')}
            </FormLabel>
            <FormTextarea
              id="rotateSshKey"
              placeholder={t('form.sshKeyPlaceholder')}
              value={primaryCredential}
              onChange={e => setPrimaryCredential(e.target.value)}
              disabled={rotateMutation.isPending}
            />
          </FormGroup>

          <FormGroup>
            <FormLabel htmlFor="rotatePassphrase">{t('form.passphrase')}</FormLabel>
            <FormInput
              id="rotatePassphrase"
              type="password"
              value={secondaryCredential}
              onChange={e => setSecondaryCredential(e.target.value)}
              disabled={rotateMutation.isPending}
            />
          </FormGroup>
        </>
      )}

      <FormGroup>
        <FormLabel htmlFor="rotateWebhookSecret">{t('form.webhookSecret')}</FormLabel>
        <FormInput
          id="rotateWebhookSecret"
          type="password"
          value={webhookSecret}
          onChange={e => setWebhookSecret(e.target.value)}
          disabled={rotateMutation.isPending}
        />
      </FormGroup>
    </Modal>
  );
}
