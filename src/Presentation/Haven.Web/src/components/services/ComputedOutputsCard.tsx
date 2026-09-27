import { TFunction } from 'i18next';
import { Eye, EyeOff, Plug } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import { servicesApi } from '@/api/services';
import { ComputedOutputDto } from '@/api/types';
import { Row } from '@/components/layout';
import { Button } from '@/components/ui/Button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/Card';
import { CodeSpan } from '@/components/ui/CodeSpan';
import { KeyValueList, KeyValueRow } from '@/components/ui/KeyValueList';
import { Tooltip } from '@/components/ui/Tooltip';
import { usePermission } from '@/hooks/usePermission';

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

  if (!output.isAvailable) {
    return (
      <KeyValueRow label={output.label}>
        <span style={{ color: 'var(--color-text-muted)' }}>
          {unavailableMessage(output.unavailableReason, t)}
        </span>
      </KeyValueRow>
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

  if (!output.isSecret) {
    return (
      <KeyValueRow label={output.label}>
        <CodeSpan copyable>{output.preview ?? ''}</CodeSpan>
      </KeyValueRow>
    );
  }

  if (!canReveal) {
    return (
      <KeyValueRow label={output.label}>
        <Tooltip content={t('services:computedOutputs.requiresPermission')}>
          <CodeSpan>{output.preview ?? ''}</CodeSpan>
        </Tooltip>
      </KeyValueRow>
    );
  }

  const handleToggleReveal = async () => {
    if (!revealed) {
      await fetchValue();
    }
    setRevealed(!revealed);
  };

  return (
    <KeyValueRow label={output.label}>
      <Row gap="2" align="center">
        <CodeSpan copyable onBeforeCopy={fetchValue} style={{ flex: 1 }}>
          {revealed && value ? value : (output.preview ?? '')}
        </CodeSpan>
        <Button
          variant="ghost"
          size="sm"
          icon={revealed ? <EyeOff size={14} /> : <Eye size={14} />}
          onClick={handleToggleReveal}
          title={
            revealed ? t('services:computedOutputs.hide') : t('services:computedOutputs.reveal')
          }
        >
          {revealed ? t('services:computedOutputs.hide') : t('services:computedOutputs.reveal')}
        </Button>
      </Row>
    </KeyValueRow>
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
        <KeyValueList bare>
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
        </KeyValueList>
      </CardContent>
    </Card>
  );
}
