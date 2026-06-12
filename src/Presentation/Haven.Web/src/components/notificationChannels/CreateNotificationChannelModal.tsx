import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Modal } from '@/components/ui/Modal';
import { Button } from '@/components/ui/Button';
import { Checkbox } from '@/components/ui/Checkbox';
import { FormGroup, FormLabel, FormInput } from '@/components/ui/Form';
import { NotificationChannelPicker } from './NotificationChannelPicker';
import { WebhookChannelForm } from './WebhookChannelForm';
import { useCreateNotificationChannel } from '@/hooks/useNotificationChannels';
import type { NotificationChannel, CreateNotificationChannelConfigInput } from '@/api/types';
import styles from './CreateNotificationChannelModal.module.css';

interface CreateNotificationChannelModalProps {
  isOpen: boolean;
  onClose: () => void;
}

export function CreateNotificationChannelModal({ isOpen, onClose }: CreateNotificationChannelModalProps) {
  const { t } = useTranslation(['notificationChannels', 'common']);
  const createMutation = useCreateNotificationChannel();

  const [channel, setChannel] = useState<NotificationChannel>('Webhook');
  const [name, setName] = useState('');
  const [enabled, setEnabled] = useState(true);
  const [configJson, setConfigJson] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const isLoading = createMutation.isPending;
  const canSubmit = !!name.trim() && configJson !== null;

  const handleChannelChange = (next: NotificationChannel) => {
    setChannel(next);
    setConfigJson(null);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (!canSubmit) return;

    const data: CreateNotificationChannelConfigInput = {
      name: name.trim(),
      channel,
      configJson: configJson!,
      enabled,
    };

    try {
      await createMutation.mutateAsync(data);
      handleClose();
    } catch (err) {
      setError(err instanceof Error ? err.message : t('modal.createError'));
    }
  };

  const handleClose = () => {
    setChannel('Webhook');
    setName('');
    setEnabled(true);
    setConfigJson(null);
    setError(null);
    onClose();
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={handleClose}
      title={t('modal.title')}
      size="lg"
      closeOnEscape={!isLoading}
      closeOnBackdropClick={!isLoading}
    >
      <form onSubmit={handleSubmit} className={styles.content}>
        <div className={styles.section}>
          <div>
            <h3 className={styles.sectionTitle}>{t('modal.channelType.title')}</h3>
            <p className={styles.sectionDescription}>{t('modal.channelType.description')}</p>
          </div>
          <NotificationChannelPicker
            value={channel}
            onChange={handleChannelChange}
            disabled={isLoading}
          />
        </div>

        <div className={styles.section}>
          <h3 className={styles.sectionTitle}>{t('modal.configuration')}</h3>

          <FormGroup>
            <FormLabel htmlFor="channelName" required>
              {t('common:labels.name')}
            </FormLabel>
            <FormInput
              id="channelName"
              type="text"
              placeholder={t('modal.namePlaceholder')}
              value={name}
              onChange={e => setName(e.target.value)}
              disabled={isLoading}
            />
          </FormGroup>

          {channel === 'Webhook' && (
            <WebhookChannelForm
              key="webhook"
              onConfigChange={setConfigJson}
              disabled={isLoading}
            />
          )}

          <FormGroup>
            <Checkbox
              label={t('modal.enabledLabel')}
              description={t('modal.enabledDescription')}
              checked={enabled}
              onChange={e => setEnabled(e.target.checked)}
              disabled={isLoading}
            />
          </FormGroup>
        </div>

        {error && <div className={styles.error}>{error}</div>}

        <div className={styles.footer}>
          <Button variant="secondary" onClick={handleClose} disabled={isLoading}>
            {t('common:actions.cancel')}
          </Button>
          <button
            type="submit"
            className={styles.primaryButton}
            disabled={isLoading || !canSubmit}
          >
            {isLoading ? t('modal.submitting') : t('modal.submit')}
          </button>
        </div>
      </form>
    </Modal>
  );
}
