import { CheckCircle, XCircle } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import type { CreateNotificationChannelConfigInput } from '@/api/types/notification.types';
import type { NotificationChannelConfigDto } from '@/api/types/notification.types';
import type { NotificationChannel } from '@/api/types/notification.types';
import { Button } from '@/components/ui/Button';
import { Checkbox } from '@/components/ui/Checkbox';
import { FormGroup, FormInput, FormLabel } from '@/components/ui/Form';
import { Modal } from '@/components/ui/Modal';
import {
  useCreateNotificationChannel,
  useTestNotificationChannelInline,
  useUpdateNotificationChannel,
} from '@/hooks/useNotificationChannels';
import styles from '@/styles/components/notifications/CreateNotificationChannelModal.module.css';

import { DiscordChannelForm } from './DiscordChannelForm';
import { NotificationChannelPicker } from './NotificationChannelPicker';
import { WebhookChannelForm } from './WebhookChannelForm';

interface CreateNotificationChannelModalProps {
  isOpen: boolean;
  onClose: () => void;
  editConfig?: NotificationChannelConfigDto;
}

interface FormContentProps {
  editConfig?: NotificationChannelConfigDto;
  onClose: () => void;
}

function FormContent({ editConfig, onClose }: FormContentProps) {
  const { t } = useTranslation(['notificationChannels', 'common']);
  const createMutation = useCreateNotificationChannel();
  const updateMutation = useUpdateNotificationChannel();
  const testMutation = useTestNotificationChannelInline();

  const isEditing = !!editConfig;

  const [channel, setChannel] = useState<NotificationChannel>(editConfig?.channel ?? 'Webhook');
  const [name, setName] = useState(editConfig?.name ?? '');
  const [enabled, setEnabled] = useState(editConfig?.enabled ?? true);
  const [configJson, setConfigJson] = useState<string | null>(editConfig?.config ?? null);
  const [error, setError] = useState<string | null>(null);
  const [testResult, setTestResult] = useState<{
    success: boolean;
    response: string | null;
    errorMessage: string | null;
  } | null>(null);

  const isLoading = createMutation.isPending || updateMutation.isPending;
  const isTesting = testMutation.isPending;
  const canSubmit = !!name.trim() && configJson !== null;

  const handleChannelChange = (next: NotificationChannel) => {
    setChannel(next);
    setConfigJson(null);
    setTestResult(null);
  };

  const handleTest = async () => {
    if (!configJson) return;
    setTestResult(null);
    try {
      const result = await testMutation.mutateAsync({ channel, configJson });
      setTestResult(result);
    } catch {
      setTestResult({ success: false, response: null, errorMessage: t('test.error') });
    }
  };

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError(null);

    if (!canSubmit) return;

    try {
      if (isEditing) {
        await updateMutation.mutateAsync({
          id: editConfig.id,
          data: { name: name.trim(), configJson: configJson!, enabled },
        });
      } else {
        const data: CreateNotificationChannelConfigInput = {
          name: name.trim(),
          channel,
          configJson: configJson!,
          enabled,
        };
        await createMutation.mutateAsync(data);
      }
      onClose();
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : t(isEditing ? 'modal.updateError' : 'modal.createError')
      );
    }
  };

  return (
    <form onSubmit={handleSubmit} className={styles.content}>
      {!isEditing && (
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
      )}

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
            onConfigChange={setConfigJson}
            disabled={isLoading}
            initialConfigJson={editConfig?.config}
          />
        )}

        {channel === 'Discord' && (
          <DiscordChannelForm
            onConfigChange={setConfigJson}
            disabled={isLoading}
            initialConfigJson={editConfig?.config}
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
        <Button
          type="button"
          variant="outline"
          onClick={handleTest}
          disabled={!configJson || isLoading || isTesting}
          isLoading={isTesting}
        >
          {t('test.testConnection')}
        </Button>

        {testResult && !isTesting && (
          <span className={testResult.success ? styles.testSuccess : styles.testFailure}>
            {testResult.success ? (
              <>
                <CheckCircle size={14} /> {t('test.success')}
              </>
            ) : (
              <>
                <XCircle size={14} /> {testResult.errorMessage ?? t('test.error')}
              </>
            )}
          </span>
        )}

        <div className={styles.footerActions}>
          <Button type="button" variant="secondary" onClick={onClose} disabled={isLoading}>
            {t('common:actions.cancel')}
          </Button>
          <button type="submit" className={styles.primaryButton} disabled={isLoading || !canSubmit}>
            {isLoading
              ? t(isEditing ? 'modal.updating' : 'modal.submitting')
              : t(isEditing ? 'modal.update' : 'modal.submit')}
          </button>
        </div>
      </div>
    </form>
  );
}

export function CreateNotificationChannelModal({
  isOpen,
  onClose,
  editConfig,
}: CreateNotificationChannelModalProps) {
  const { t } = useTranslation('notificationChannels');
  const isEditing = !!editConfig;

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title={isEditing ? t('modal.editTitle') : t('modal.title')}
      size="lg"
      closeOnEscape
      closeOnBackdropClick
    >
      <FormContent key={editConfig?.id ?? 'create'} editConfig={editConfig} onClose={onClose} />
    </Modal>
  );
}
