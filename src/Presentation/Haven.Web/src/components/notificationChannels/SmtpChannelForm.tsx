import { X } from 'lucide-react';
import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';

import type { SmtpNotificationConfig } from '@/api/types';
import { FormGroup, FormInput, FormLabel } from '@/components/ui/Form';
import styles from '@/styles/components/notifications/SmtpChannelForm.module.css';

import { Checkbox } from '../ui/Checkbox';
import type { ChannelFormProps } from './channelForms';

interface SmtpFormState {
  host: string;
  port: string;
  username: string;
  password: string;
  fromEmail: string;
  fromName: string;
  enableSsl: boolean;
  toEmails: string[];
}

const DEFAULTS: SmtpFormState = {
  host: '',
  port: '587',
  username: '',
  password: '',
  fromEmail: '',
  fromName: '',
  enableSsl: true,
  toEmails: [''],
};

function parseInitialConfig(configJson?: string): SmtpFormState {
  if (!configJson) return DEFAULTS;
  try {
    const parsed = JSON.parse(configJson) as Partial<SmtpNotificationConfig>;
    return {
      host: parsed.host ?? DEFAULTS.host,
      port: parsed.port ? String(parsed.port) : DEFAULTS.port,
      username: parsed.username ?? DEFAULTS.username,
      password: parsed.password ?? DEFAULTS.password,
      fromEmail: parsed.fromEmail ?? DEFAULTS.fromEmail,
      fromName: parsed.fromName ?? DEFAULTS.fromName,
      enableSsl: parsed.enableSsl ?? DEFAULTS.enableSsl,
      toEmails: parsed.toEmails && parsed.toEmails.length > 0 ? parsed.toEmails : DEFAULTS.toEmails,
    };
  } catch {
    return DEFAULTS;
  }
}

export function SmtpChannelForm({ onConfigChange, disabled, initialConfigJson }: ChannelFormProps) {
  const { t } = useTranslation('notificationChannels');

  const initial = parseInitialConfig(initialConfigJson);
  const [host, setHost] = useState(initial.host);
  const [port, setPort] = useState(initial.port);
  const [username, setUsername] = useState(initial.username);
  const [password, setPassword] = useState(initial.password);
  const [fromEmail, setFromEmail] = useState(initial.fromEmail);
  const [fromName, setFromName] = useState(initial.fromName);
  const [enableSsl, setEnableSsl] = useState(initial.enableSsl);
  const [toEmails, setToEmails] = useState<string[]>(initial.toEmails);

  useEffect(() => {
    const portNumber = Number(port);
    const recipients = toEmails.map(e => e.trim()).filter(Boolean);

    if (
      !host.trim() ||
      !Number.isInteger(portNumber) ||
      portNumber <= 0 ||
      !fromEmail.trim() ||
      recipients.length === 0
    ) {
      onConfigChange(null);
      return;
    }

    const config: SmtpNotificationConfig = {
      host: host.trim(),
      port: portNumber,
      username: username.trim(),
      password,
      fromEmail: fromEmail.trim(),
      fromName: fromName.trim(),
      enableSsl,
      toEmails: recipients,
    };
    onConfigChange(JSON.stringify(config));
  }, [host, port, username, password, fromEmail, fromName, enableSsl, toEmails]);

  const updateRecipient = (idx: number, value: string) =>
    setToEmails(prev => prev.map((email, i) => (i === idx ? value : email)));

  const addRecipient = () => setToEmails(prev => [...prev, '']);

  const removeRecipient = (idx: number) => setToEmails(prev => prev.filter((_, i) => i !== idx));

  return (
    <>
      <FormGroup>
        <FormLabel htmlFor="smtpHost" required>
          {t('smtp.hostLabel')}
        </FormLabel>
        <FormInput
          id="smtpHost"
          type="text"
          placeholder={t('smtp.hostPlaceholder')}
          value={host}
          onChange={e => setHost(e.target.value)}
          disabled={disabled}
        />
      </FormGroup>

      <FormGroup>
        <FormLabel htmlFor="smtpPort" required>
          {t('smtp.portLabel')}
        </FormLabel>
        <FormInput
          id="smtpPort"
          type="number"
          min={1}
          max={65535}
          placeholder={t('smtp.portPlaceholder')}
          value={port}
          onChange={e => setPort(e.target.value)}
          disabled={disabled}
        />
      </FormGroup>

      <FormGroup>
        <FormLabel htmlFor="smtpUsername" optional>
          {t('smtp.usernameLabel')}
        </FormLabel>
        <FormInput
          id="smtpUsername"
          type="text"
          placeholder={t('smtp.usernamePlaceholder')}
          value={username}
          onChange={e => setUsername(e.target.value)}
          disabled={disabled}
        />
      </FormGroup>

      <FormGroup>
        <FormLabel htmlFor="smtpPassword" optional>
          {t('smtp.passwordLabel')}
        </FormLabel>
        <FormInput
          id="smtpPassword"
          type="password"
          placeholder={t('smtp.passwordPlaceholder')}
          value={password}
          onChange={e => setPassword(e.target.value)}
          disabled={disabled}
        />
      </FormGroup>

      <FormGroup>
        <FormLabel htmlFor="smtpFromEmail" required>
          {t('smtp.fromEmailLabel')}
        </FormLabel>
        <FormInput
          id="smtpFromEmail"
          type="email"
          placeholder={t('smtp.fromEmailPlaceholder')}
          value={fromEmail}
          onChange={e => setFromEmail(e.target.value)}
          disabled={disabled}
        />
      </FormGroup>

      <FormGroup>
        <FormLabel htmlFor="smtpFromName" optional>
          {t('smtp.fromNameLabel')}
        </FormLabel>
        <FormInput
          id="smtpFromName"
          type="text"
          placeholder={t('smtp.fromNamePlaceholder')}
          value={fromName}
          onChange={e => setFromName(e.target.value)}
          disabled={disabled}
        />
      </FormGroup>

      <FormGroup>
        <Checkbox
          id="smtpEnableSsl"
          label={t('smtp.enableSslLabel')}
          description={t('smtp.enableSslDescription')}
          checked={enableSsl}
          onChange={e => setEnableSsl(e.target.checked)}
          disabled={disabled}
        />
      </FormGroup>

      <FormGroup>
        <FormLabel htmlFor="smtpRecipients" required>
          {t('smtp.toEmailsLabel')}
        </FormLabel>

        <div className={styles.recipientsContainer}>
          {toEmails.length === 0 ? (
            <p className={styles.emptyState}>{t('smtp.noRecipients')}</p>
          ) : (
            toEmails.map((email, idx) => (
              <div key={idx} className={styles.recipientRow}>
                <input
                  type="email"
                  className={styles.recipientInput}
                  placeholder={t('smtp.recipientPlaceholder')}
                  value={email}
                  onChange={e => updateRecipient(idx, e.target.value)}
                  disabled={disabled}
                />
                <button
                  type="button"
                  className={styles.removeButton}
                  onClick={() => removeRecipient(idx)}
                  disabled={disabled}
                >
                  <X size={14} />
                </button>
              </div>
            ))
          )}
        </div>

        <button
          type="button"
          className={styles.addButton}
          onClick={addRecipient}
          disabled={disabled}
        >
          {t('smtp.addRecipient')}
        </button>
      </FormGroup>
    </>
  );
}
