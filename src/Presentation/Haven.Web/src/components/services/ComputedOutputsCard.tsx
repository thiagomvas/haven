import { TFunction } from 'i18next';
import { Check, Copy, Eye, EyeOff, Plug } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import { servicesApi } from '@/api/services';
import { ComputedOutputDto } from '@/api/types';
import { Row } from '@/components/layout';
import { Button } from '@/components/ui/Button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/Card';
import { Tooltip } from '@/components/ui/Tooltip';
import { usePermission } from '@/hooks/usePermission';
import styles from '@/styles/components/services/ComputedOutputsCard.module.css';

interface ComputedOutputsCardProps {
  projectId: string;
  environmentId: string;
  serviceId: string;
  outputs: ComputedOutputDto[];
}

interface ComputedOutputRowProps {
  projectId: string;
  environmentId: string;
  serviceId: string;
  output: ComputedOutputDto;
  canReveal: boolean;
}

async function copyToClipboard(text: string): Promise<void> {
  if (navigator.clipboard) {
    await navigator.clipboard.writeText(text);
    return;
  }
  const textarea = document.createElement('textarea');
  textarea.value = text;
  textarea.style.position = 'fixed';
  textarea.style.opacity = '0';
  document.body.appendChild(textarea);
  textarea.focus();
  textarea.select();
  try {
    if (!document.execCommand('copy')) {
      throw new Error('Copy command was unsuccessful');
    }
  } finally {
    document.body.removeChild(textarea);
  }
}

function unavailableMessage(reason: string | undefined, t: TFunction<['services']>): string {
  if (reason?.startsWith('MissingVariable')) return t('services:computedOutputs.missingVariable');
  return t('services:computedOutputs.notRunning');
}

function ComputedOutputRow({
  projectId,
  environmentId,
  serviceId,
  output,
  canReveal,
}: ComputedOutputRowProps) {
  const { t } = useTranslation(['services']);
  const [revealed, setRevealed] = useState(false);
  const [value, setValue] = useState<string | undefined>(undefined);
  const [copied, setCopied] = useState(false);

  if (!output.isAvailable) {
    return (
      <div className={styles.line}>
        <span className={styles.label}>{output.label}</span>
        <span className={styles.unavailable}>
          {unavailableMessage(output.unavailableReason, t)}
        </span>
      </div>
    );
  }

  const fetchValue = async (): Promise<string> => {
    if (value) return value;
    const result = await servicesApi.getComputedOutputValue(
      projectId,
      environmentId,
      serviceId,
      output.key
    );
    setValue(result.value);
    return result.value;
  };

  const isMasked = output.isSecret && (!canReveal || !revealed);
  const displayValue = isMasked ? (output.preview ?? '') : (value ?? output.preview ?? '');

  const handleToggleReveal = async () => {
    if (!revealed) {
      await fetchValue();
    }
    setRevealed(r => !r);
  };

  const handleCopy = async () => {
    const text = output.isSecret ? await fetchValue() : (output.preview ?? '');
    await copyToClipboard(text);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  const valueSpan = (
    <span className={styles.value} title={isMasked ? undefined : displayValue}>
      {displayValue}
    </span>
  );

  return (
    <div className={styles.line}>
      <span className={styles.label}>{output.label}</span>
      {output.isSecret && !canReveal ? (
        <Tooltip content={t('services:computedOutputs.requiresPermission')}>{valueSpan}</Tooltip>
      ) : (
        valueSpan
      )}
      <Row gap="1" className={styles.actions}>
        {output.isSecret && canReveal && (
          <Button
            variant="ghost"
            size="xs"
            icon={revealed ? <EyeOff size={14} /> : <Eye size={14} />}
            onClick={handleToggleReveal}
            title={
              revealed ? t('services:computedOutputs.hide') : t('services:computedOutputs.reveal')
            }
          />
        )}
        {(!output.isSecret || canReveal) && (
          <Button
            variant="ghost"
            size="xs"
            icon={copied ? <Check size={14} /> : <Copy size={14} />}
            onClick={handleCopy}
            title={t('services:computedOutputs.copy')}
          />
        )}
      </Row>
    </div>
  );
}

export function ComputedOutputsCard({
  projectId,
  environmentId,
  serviceId,
  outputs,
}: ComputedOutputsCardProps) {
  const { t } = useTranslation(['services']);
  const canReveal = usePermission('projects.manage_secrets');

  if (outputs.length === 0) return null;

  return (
    <Card padding="var(--space-4)">
      <CardHeader>
        <CardTitle>
          <Row gap="2" align="center">
            <Plug size={16} />
            {t('services:computedOutputs.title')}
          </Row>
        </CardTitle>
      </CardHeader>
      <CardContent>
        <div className={styles.list}>
          {outputs.map(output => (
            <ComputedOutputRow
              key={output.key}
              projectId={projectId}
              environmentId={environmentId}
              serviceId={serviceId}
              output={output}
              canReveal={canReveal}
            />
          ))}
        </div>
      </CardContent>
    </Card>
  );
}
