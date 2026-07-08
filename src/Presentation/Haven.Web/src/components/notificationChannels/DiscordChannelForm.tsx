import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';

import type { DiscordNotificationConfig } from '@/api/types';
import { FormGroup, FormInput, FormLabel } from '@/components/ui/Form';

import { Checkbox } from '../ui/Checkbox';
import type { ChannelFormProps } from './channelForms';

function parseInitialConfig(configJson?: string): { webhookUrl: string; embed: boolean } {
  if (!configJson) return { webhookUrl: '', embed: false };
  try {
    const parsed = JSON.parse(configJson) as { webhookUrl?: string; embed?: boolean };
    return {
      webhookUrl: parsed.webhookUrl ?? '',
      embed: parsed.embed ?? false,
    };
  } catch {
    return { webhookUrl: '', embed: false };
  }
}

export function DiscordChannelForm({
  onConfigChange,
  disabled,
  initialConfigJson,
}: ChannelFormProps) {
  const { t } = useTranslation('notificationChannels');

  const initial = parseInitialConfig(initialConfigJson);
  const [url, setUrl] = useState(initial.webhookUrl);
  const [embed, setEmbed] = useState(initial.embed);

  useEffect(() => {
    if (!url.trim()) {
      onConfigChange(null);
      return;
    }
    const config: DiscordNotificationConfig = {
      webhookUrl: url.trim(),
      embed: embed,
    };
    onConfigChange(JSON.stringify(config));
  }, [url, embed]);
  return (
    <>
      <FormGroup>
        <FormLabel htmlFor="webhookUrl" required>
          {t('discord.webhookUrlLabel')}
        </FormLabel>
        <FormInput
          id="webhookUrl"
          type="url"
          placeholder={t('discord.webhookUrlPlaceholder')}
          value={url}
          onChange={e => setUrl(e.target.value)}
          disabled={disabled}
        />
      </FormGroup>

      <FormGroup>
        <Checkbox
          id="embed"
          label={t('discord.embedLabel')}
          description={t('discord.embedDescription')}
          checked={embed}
          onChange={e => setEmbed(e.target.checked)}
        />
      </FormGroup>
    </>
  );
}
