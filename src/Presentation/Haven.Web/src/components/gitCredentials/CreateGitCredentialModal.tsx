import { ChevronDown } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import { CreateGitCredentialInput } from '@/api/types/git.types';
import { GitAuthMethod } from '@/api/types/git.types';
import { GitProviderType } from '@/api/types/git.types';
import { Button } from '@/components/ui/Button';
import { Modal } from '@/components/ui/Modal';
import { useCreateGitCredential } from '@/hooks/useGitCredentials';

import styles from './CreateGitCredentialModal.module.css';
import { ProviderBadge, ProviderIcon } from './ProviderIcon';

interface CreateGitCredentialModalProps {
  isOpen: boolean;
  onClose: () => void;
}

type ProviderTypeOption = GitProviderType;

const PROVIDERS: ProviderTypeOption[] = ['GitHub', 'GitLab', 'Bitbucket', 'Gitea', 'Generic'];

export function CreateGitCredentialModal({ isOpen, onClose }: CreateGitCredentialModalProps) {
  const { t } = useTranslation(['gitCredentials', 'common']);
  const createMutation = useCreateGitCredential();

  const [selectedProvider, setSelectedProvider] = useState<ProviderTypeOption>('GitHub');
  const [displayName, setDisplayName] = useState('');
  const [hostUrl, setHostUrl] = useState('');
  const [authMethod, setAuthMethod] = useState<GitAuthMethod>('Token');
  const [token, setToken] = useState('');
  const [sshKey, setSshKey] = useState('');
  const [passphrase, setPassphrase] = useState('');
  const [webhookSecret, setWebhookSecret] = useState('');
  const [showAdvanced, setShowAdvanced] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const isLoading = createMutation.isPending;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (!displayName.trim()) {
      setError(t('errors.displayNameRequired'));
      return;
    }

    const primaryCredential = authMethod === 'Token' ? token : sshKey;
    if (!primaryCredential.trim()) {
      setError(t('errors.primaryCredentialRequired'));
      return;
    }

    const data: CreateGitCredentialInput = {
      providerType: selectedProvider,
      hostUrl: hostUrl.trim() || undefined,
      authMethod,
      primaryCredential,
      secondaryCredential: authMethod === 'Ssh' && passphrase.trim() ? passphrase : undefined,
      webhookSecret: webhookSecret.trim() || undefined,
      displayName: displayName.trim(),
    };

    try {
      await createMutation.mutateAsync(data);
      handleClose();
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to create credential';
      setError(message);
    }
  };

  const handleClose = () => {
    setSelectedProvider('GitHub');
    setDisplayName('');
    setHostUrl('');
    setAuthMethod('Token');
    setToken('');
    setSshKey('');
    setPassphrase('');
    setWebhookSecret('');
    setShowAdvanced(false);
    setError(null);
    onClose();
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={handleClose}
      title={t('addCredential')}
      size="lg"
      closeOnEscape={!isLoading}
      closeOnBackdropClick={!isLoading}
    >
      <form onSubmit={handleSubmit} className={styles.content}>
        {/* Provider Selection Section */}
        <div className={styles.section}>
          <div>
            <h3 className={styles.sectionTitle}>{t('providerSelect.title')}</h3>
            <p className={styles.sectionDescription}>{t('providerSelect.description')}</p>
          </div>

          <div className={styles.providerGrid}>
            {PROVIDERS.map(provider => (
              <button
                key={provider}
                type="button"
                className={`${styles.providerCard} ${selectedProvider === provider ? styles.selected : ''}`}
                onClick={() => setSelectedProvider(provider)}
              >
                <div className={styles.providerIcon}>
                  <ProviderIcon provider={provider} size={32} />
                </div>
                <span>{provider}</span>
              </button>
            ))}
          </div>
        </div>

        {/* Form Section */}
        <div className={styles.section}>
          <div className={styles.formSection}>
            {/* Display Name */}
            <div className={styles.formGroup}>
              <label className={styles.label}>{t('form.displayName')}</label>
              <input
                type="text"
                className={styles.input}
                value={displayName}
                onChange={e => setDisplayName(e.target.value)}
                disabled={isLoading}
              />
              <span className={styles.helpText}>{t('form.displayNameHelp')}</span>
            </div>

            {/* Host URL */}
            <div className={styles.formGroup}>
              <div className={styles.labelWithHelp}>
                <label className={styles.label}>{t('form.hostUrl')}</label>
                <span className={styles.helpText}>{t('form.hostUrlHelp')}</span>
              </div>
              <input
                type="url"
                className={styles.input}
                value={hostUrl}
                onChange={e => setHostUrl(e.target.value)}
                placeholder={t('form.hostUrlPlaceholder')}
                disabled={isLoading}
              />
            </div>

            {/* Auth Method Toggle */}
            <div className={styles.formGroup}>
              <label className={styles.label}>{t('form.authMethod')}</label>
              <div className={styles.authMethodToggle}>
                <button
                  type="button"
                  className={`${styles.toggleButton} ${authMethod === 'Token' ? styles.active : ''}`}
                  onClick={() => setAuthMethod('Token')}
                  disabled={isLoading}
                >
                  {t('auth.token')}
                </button>
                <button
                  type="button"
                  className={`${styles.toggleButton} ${authMethod === 'Ssh' ? styles.active : ''}`}
                  onClick={() => setAuthMethod('Ssh')}
                  disabled={isLoading}
                >
                  {t('auth.ssh')}
                </button>
              </div>
            </div>

            {/* Credential Fields */}
            <div className={styles.credentialFields}>
              {authMethod === 'Token' ? (
                <div className={styles.formGroup}>
                  <label className={styles.label}>{t('form.token')}</label>
                  <input
                    type="password"
                    className={styles.input}
                    value={token}
                    onChange={e => setToken(e.target.value)}
                    placeholder={t('form.tokenPlaceholder')}
                    disabled={isLoading}
                  />
                </div>
              ) : (
                <>
                  <div className={styles.formGroup}>
                    <label className={styles.label}>{t('form.sshKey')}</label>
                    <textarea
                      className={styles.textarea}
                      value={sshKey}
                      onChange={e => setSshKey(e.target.value)}
                      placeholder={t('form.sshKeyPlaceholder')}
                      disabled={isLoading}
                    />
                  </div>

                  <div className={styles.formGroup}>
                    <div className={styles.labelWithHelp}>
                      <label className={styles.label}>{t('form.passphrase')}</label>
                      <span className={styles.helpText}>{t('form.passphraseHelp')}</span>
                    </div>
                    <input
                      type="password"
                      className={styles.input}
                      value={passphrase}
                      onChange={e => setPassphrase(e.target.value)}
                      disabled={isLoading}
                    />
                  </div>
                </>
              )}
            </div>

            {/* Advanced Section */}
            <button
              type="button"
              className={styles.advancedToggle}
              onClick={() => setShowAdvanced(!showAdvanced)}
              disabled={isLoading}
            >
              <span className={`${styles.advancedIcon} ${showAdvanced ? styles.open : ''}`}>
                <ChevronDown size={16} />
              </span>
              {t('form.advanced')}
            </button>

            {showAdvanced && (
              <div className={styles.advancedContent}>
                <div className={styles.formGroup}>
                  <div className={styles.labelWithHelp}>
                    <label className={styles.label}>{t('form.webhookSecret')}</label>
                    <span className={styles.helpText}>{t('form.webhookSecretHelp')}</span>
                  </div>
                  <input
                    type="password"
                    className={styles.input}
                    value={webhookSecret}
                    onChange={e => setWebhookSecret(e.target.value)}
                    disabled={isLoading}
                  />
                </div>
              </div>
            )}
          </div>

          {/* Error Message */}
          {error && <div className={styles.error}>{error}</div>}

          {/* Footer */}
          <div className={styles.footer}>
            <Button variant="secondary" onClick={handleClose} disabled={isLoading}>
              {t('common:actions.cancel')}
            </Button>
            <button
              type="submit"
              className={styles.primaryButton}
              disabled={isLoading || !displayName.trim()}
            >
              {isLoading ? t('form.saving') : t('form.create')}
            </button>
          </div>
        </div>
      </form>
    </Modal>
  );
}
