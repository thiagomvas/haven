import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';

import type { NtfyNotificationConfig } from '@/api/types';
import { FormGroup, FormInput, FormLabel } from '@/components/ui/Form';

import { Checkbox } from '../ui/Checkbox';
import type { ChannelFormProps } from './channelForms';

function parseInitialConfig(configJson?: string): {
  host: string;
  queue: string;
  enableSSL: boolean;
} {
  if (!configJson) return { host: 'ntfy.sh', queue: '', enableSSL: false };
  try {
    const parsed = JSON.parse(configJson) as Partial<NtfyNotificationConfig>;
    return {
      host: parsed.host ?? 'ntfy.sh',
      queue: parsed.queue ?? '',
      enableSSL: parsed.enableSSL ?? false,
    };
  } catch {
    return { host: 'ntfy.sh', queue: '', enableSSL: false };
  }
}

export function NtfyChannelForm({ onConfigChange, disabled, initialConfigJson }: ChannelFormProps) {
  const { t } = useTranslation('notificationChannels');

  const initial = parseInitialConfig(initialConfigJson);
  const [host, setHost] = useState(initial.host);
  const [queue, setQueue] = useState(initial.queue);
  const [enableSSL, setEnableSSL] = useState(initial.enableSSL);

  useEffect(() => {
    if (!host.trim() || !queue.trim()) {
      onConfigChange(null);
      return;
    }
    const config: NtfyNotificationConfig = {
      host: host.trim(),
      queue: queue.trim(),
      enableSSL,
    };
    onConfigChange(JSON.stringify(config));
  }, [host, queue, enableSSL]);

  return (
    <>
      <FormGroup>
        <FormLabel htmlFor="ntfyHost" required>
          {t('ntfy.hostLabel')}
        </FormLabel>
        <FormInput
          id="ntfyHost"
          type="text"
          placeholder={t('ntfy.hostPlaceholder')}
          value={host}
          onChange={e => setHost(e.target.value)}
          disabled={disabled}
        />
      </FormGroup>

      <FormGroup>
        <FormLabel htmlFor="ntfyQueue" required>
          {t('ntfy.queueLabel')}
        </FormLabel>
        <FormInput
          id="ntfyQueue"
          type="text"
          placeholder={t('ntfy.queuePlaceholder')}
          value={queue}
          onChange={e => setQueue(e.target.value)}
          disabled={disabled}
        />
      </FormGroup>

      <FormGroup>
        <Checkbox
          id="ntfyEnableSSL"
          label={t('ntfy.enableSSLLabel')}
          description={t('ntfy.enableSSLDescription')}
          checked={enableSSL}
          onChange={e => setEnableSSL(e.target.checked)}
          disabled={disabled}
        />
      </FormGroup>
    </>
  );
}
