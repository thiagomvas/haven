import { useState } from 'react';
import { Modal } from '@/components/ui/Modal';
import { Button } from '@/components/ui/Button';
import { Checkbox } from '@/components/ui/Checkbox';
import { FormGroup, FormLabel, FormInput } from '@/components/ui/Form';
import { NotificationChannelPicker } from './NotificationChannelPicker';
import { WebhookConfigFields, type WebhookConfig } from './WebhookConfigFields';
import { useCreateNotificationChannel } from '@/hooks/useNotificationChannels';
import type {
  NotificationChannel,
  CreateNotificationChannelConfigInput,
  WebhookNotificationConfig,
} from '@/api/types';
import styles from './CreateNotificationChannelModal.module.css';

interface CreateNotificationChannelModalProps {
  isOpen: boolean;
  onClose: () => void;
}

function buildConfigJson(channel: NotificationChannel, webhookConfig: WebhookConfig): string {
  if (channel === 'Webhook') {
    const config: WebhookNotificationConfig = {
      url: webhookConfig.url,
      headers: Object.fromEntries(
        webhookConfig.headers
          .filter(h => h.key.trim())
          .map(h => [h.key.trim(), h.value])
      ),
    };
    return JSON.stringify(config);
  }
  return '{}';
}

function isConfigValid(channel: NotificationChannel, webhookConfig: WebhookConfig): boolean {
  if (channel === 'Webhook') return !!webhookConfig.url.trim();
  return false;
}

export function CreateNotificationChannelModal({ isOpen, onClose }: CreateNotificationChannelModalProps) {
  const createMutation = useCreateNotificationChannel();

  const [channel, setChannel] = useState<NotificationChannel>('Webhook');
  const [name, setName] = useState('');
  const [enabled, setEnabled] = useState(true);
  const [webhookConfig, setWebhookConfig] = useState<WebhookConfig>({ url: '', headers: [] });
  const [error, setError] = useState<string | null>(null);

  const isLoading = createMutation.isPending;

  const handleChannelChange = (next: NotificationChannel) => {
    setChannel(next);
    setWebhookConfig({ url: '', headers: [] });
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (!name.trim()) {
      setError('Name is required.');
      return;
    }
    if (!isConfigValid(channel, webhookConfig)) {
      setError('Please fill in all required channel fields.');
      return;
    }

    const data: CreateNotificationChannelConfigInput = {
      name: name.trim(),
      channel,
      configJson: buildConfigJson(channel, webhookConfig),
      enabled,
    };

    try {
      await createMutation.mutateAsync(data);
      handleClose();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create notification channel');
    }
  };

  const handleClose = () => {
    setChannel('Webhook');
    setName('');
    setEnabled(true);
    setWebhookConfig({ url: '', headers: [] });
    setError(null);
    onClose();
  };

  const canSubmit = !!name.trim() && isConfigValid(channel, webhookConfig);

  return (
    <Modal
      isOpen={isOpen}
      onClose={handleClose}
      title="Add Notification Channel"
      size="lg"
      closeOnEscape={!isLoading}
      closeOnBackdropClick={!isLoading}
    >
      <form onSubmit={handleSubmit} className={styles.content}>
        <div className={styles.section}>
          <div>
            <h3 className={styles.sectionTitle}>Channel Type</h3>
            <p className={styles.sectionDescription}>
              Choose how notifications will be delivered.
            </p>
          </div>
          <NotificationChannelPicker
            value={channel}
            onChange={handleChannelChange}
            disabled={isLoading}
          />
        </div>

        <div className={styles.section}>
          <h3 className={styles.sectionTitle}>Configuration</h3>

          <FormGroup>
            <FormLabel htmlFor="channelName" required>
              Name
            </FormLabel>
            <FormInput
              id="channelName"
              type="text"
              placeholder="My Webhook"
              value={name}
              onChange={e => setName(e.target.value)}
              disabled={isLoading}
            />
          </FormGroup>

          {channel === 'Webhook' && (
            <WebhookConfigFields
              config={webhookConfig}
              onChange={setWebhookConfig}
              disabled={isLoading}
            />
          )}

          <FormGroup>
            <Checkbox
              label="Enabled"
              description="Receive notifications through this channel"
              checked={enabled}
              onChange={e => setEnabled(e.target.checked)}
              disabled={isLoading}
            />
          </FormGroup>
        </div>

        {error && <div className={styles.error}>{error}</div>}

        <div className={styles.footer}>
          <Button variant="secondary" onClick={handleClose} disabled={isLoading}>
            Cancel
          </Button>
          <button
            type="submit"
            className={styles.primaryButton}
            disabled={isLoading || !canSubmit}
          >
            {isLoading ? 'Creating...' : 'Create Channel'}
          </button>
        </div>
      </form>
    </Modal>
  );
}
