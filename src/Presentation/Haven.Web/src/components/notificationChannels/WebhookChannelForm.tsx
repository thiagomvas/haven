import { X } from 'lucide-react';
import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';

import type { WebhookNotificationConfig } from '@/api/types/notification.types';
import { FormGroup, FormInput, FormLabel } from '@/components/ui/Form';

import type { ChannelFormProps } from './channelForms';
import styles from '@/styles/components/notifications/WebhookChannelForm.module.css';

type HeaderRow = { key: string; value: string };

function parseInitialConfig(configJson?: string): { url: string; headers: HeaderRow[] } {
  if (!configJson) return { url: '', headers: [] };
  try {
    const parsed = JSON.parse(configJson) as { url?: string; headers?: Record<string, string> };
    return {
      url: parsed.url ?? '',
      headers: Object.entries(parsed.headers ?? {}).map(([key, value]) => ({ key, value })),
    };
  } catch {
    return { url: '', headers: [] };
  }
}

export function WebhookChannelForm({
  onConfigChange,
  disabled,
  initialConfigJson,
}: ChannelFormProps) {
  const { t } = useTranslation('notificationChannels');

  const initial = parseInitialConfig(initialConfigJson);
  const [url, setUrl] = useState(initial.url);
  const [headers, setHeaders] = useState<HeaderRow[]>(initial.headers);

  useEffect(() => {
    if (!url.trim()) {
      onConfigChange(null);
      return;
    }
    const config: WebhookNotificationConfig = {
      url: url.trim(),
      headers: Object.fromEntries(
        headers.filter(h => h.key.trim()).map(h => [h.key.trim(), h.value])
      ),
    };
    onConfigChange(JSON.stringify(config));
  }, [url, headers]);

  const updateHeader = (idx: number, field: keyof HeaderRow, val: string) =>
    setHeaders(prev => prev.map((h, i) => (i === idx ? { ...h, [field]: val } : h)));

  const addHeader = () => setHeaders(prev => [...prev, { key: '', value: '' }]);

  const removeHeader = (idx: number) => setHeaders(prev => prev.filter((_, i) => i !== idx));

  return (
    <>
      <FormGroup>
        <FormLabel htmlFor="webhookUrl" required>
          {t('webhook.urlLabel')}
        </FormLabel>
        <FormInput
          id="webhookUrl"
          type="url"
          placeholder={t('webhook.urlPlaceholder')}
          value={url}
          onChange={e => setUrl(e.target.value)}
          disabled={disabled}
        />
      </FormGroup>

      <FormGroup>
        <FormLabel htmlFor="webhookHeaders">{t('webhook.headersLabel')}</FormLabel>

        <div className={styles.headersContainer}>
          {headers.length === 0 ? (
            <p className={styles.emptyState}>{t('webhook.noHeaders')}</p>
          ) : (
            headers.map((header, idx) => (
              <div key={idx} className={styles.headerRow}>
                <input
                  type="text"
                  className={styles.headerInput}
                  placeholder={t('webhook.headerNamePlaceholder')}
                  value={header.key}
                  onChange={e => updateHeader(idx, 'key', e.target.value)}
                  disabled={disabled}
                />
                <input
                  type="text"
                  className={styles.headerInput}
                  placeholder={t('webhook.headerValuePlaceholder')}
                  value={header.value}
                  onChange={e => updateHeader(idx, 'value', e.target.value)}
                  disabled={disabled}
                />
                <button
                  type="button"
                  className={styles.removeButton}
                  onClick={() => removeHeader(idx)}
                  disabled={disabled}
                >
                  <X size={14} />
                </button>
              </div>
            ))
          )}
        </div>

        <button type="button" className={styles.addButton} onClick={addHeader} disabled={disabled}>
          {t('webhook.addHeader')}
        </button>
      </FormGroup>
    </>
  );
}
