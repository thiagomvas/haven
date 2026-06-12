import { X } from 'lucide-react';
import { FormGroup, FormLabel, FormInput } from '@/components/ui/Form';
import styles from './WebhookConfigFields.module.css';

export interface WebhookConfig {
  url: string;
  headers: { key: string; value: string }[];
}

interface WebhookConfigFieldsProps {
  config: WebhookConfig;
  onChange: (config: WebhookConfig) => void;
  disabled?: boolean;
}

export function WebhookConfigFields({ config, onChange, disabled }: WebhookConfigFieldsProps) {
  const updateUrl = (url: string) => onChange({ ...config, url });

  const updateHeader = (idx: number, field: 'key' | 'value', val: string) => {
    const updated = config.headers.map((h, i) => (i === idx ? { ...h, [field]: val } : h));
    onChange({ ...config, headers: updated });
  };

  const addHeader = () => onChange({ ...config, headers: [...config.headers, { key: '', value: '' }] });

  const removeHeader = (idx: number) =>
    onChange({ ...config, headers: config.headers.filter((_, i) => i !== idx) });

  return (
    <>
      <FormGroup>
        <FormLabel htmlFor="webhookUrl" required>
          Webhook URL
        </FormLabel>
        <FormInput
          id="webhookUrl"
          type="url"
          placeholder="https://hooks.example.com/..."
          value={config.url}
          onChange={e => updateUrl(e.target.value)}
          disabled={disabled}
        />
      </FormGroup>

      <FormGroup>
        <div className={styles.headersLabel}>
          <FormLabel htmlFor="webhookHeaders">Headers</FormLabel>
        </div>

        <div className={styles.headersContainer}>
          {config.headers.length === 0 ? (
            <p className={styles.emptyState}>No custom headers</p>
          ) : (
            config.headers.map((header, idx) => (
              <div key={idx} className={styles.headerRow}>
                <input
                  type="text"
                  className={styles.headerInput}
                  placeholder="Header name"
                  value={header.key}
                  onChange={e => updateHeader(idx, 'key', e.target.value)}
                  disabled={disabled}
                />
                <input
                  type="text"
                  className={styles.headerInput}
                  placeholder="Value"
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
          + Add header
        </button>
      </FormGroup>
    </>
  );
}
