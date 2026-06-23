import { useTranslation } from 'react-i18next';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/Card';
import { Form, FormGroup, FormLabel, FormInput, FormSelect } from '@/components/ui/Form';
import { Label } from '@/components/ui/Label';
import { Button } from '@/components/ui/Button';
import { Spinner } from '@/components/ui/Spinner';
import { Row } from '@/components/layout';
import { ErrorAlert } from '@/components/ui/ErrorAlert';
import { Checkbox } from '@/components/ui/Checkbox';
import { useForm } from '@/hooks/useForm';
import { useTelemetry, useUpdateTelemetry } from '@/hooks/useTelemetry';
import { TelemetryOptions, OtlpProtocol } from '@/api/telemetry';

function TelemetryForm({ current }: { current: TelemetryOptions }) {
  const { t } = useTranslation('settings');
  const { mutateAsync: updateTelemetry } = useUpdateTelemetry();

  const { values, fieldErrors, submitError, isLoading, handleSubmit, updateField } = useForm({
    initialValues: {
      enabled: current.enabled,
      otlpEndpoint: current.otlpEndpoint,
      serviceName: current.serviceName,
      protocol: current.protocol,
    },
    onSubmit: async values => {
      const options: TelemetryOptions = {
        enabled: values.enabled,
        otlpEndpoint: values.otlpEndpoint,
        serviceName: values.serviceName,
        protocol: values.protocol,
      };
      await updateTelemetry(options);
    },
  });

  return (
    <Form onSubmit={handleSubmit} isLoading={isLoading}>
      <FormGroup>
        <Checkbox
          label={t('telemetry.fields.enabled')}
          description={t('telemetry.fields.enabledDescription')}
          checked={values.enabled}
          onChange={e => updateField('enabled', e.target.checked)}
        />
      </FormGroup>
      <FormGroup>
        <FormLabel htmlFor="otlpEndpoint" required>
          {t('telemetry.fields.otlpEndpoint')}
        </FormLabel>
        <FormInput
          id="otlpEndpoint"
          type="text"
          value={values.otlpEndpoint}
          onChange={e => updateField('otlpEndpoint', e.target.value)}
          placeholder="http://localhost:4317"
          fieldName="otlpEndpoint"
          fieldErrors={fieldErrors}
          disabled={!values.enabled}
        />
      </FormGroup>
      <FormGroup>
        <FormLabel htmlFor="serviceName" required>
          {t('telemetry.fields.serviceName')}
        </FormLabel>
        <FormInput
          id="serviceName"
          type="text"
          value={values.serviceName}
          onChange={e => updateField('serviceName', e.target.value)}
          placeholder="haven"
          fieldName="serviceName"
          fieldErrors={fieldErrors}
          disabled={!values.enabled}
        />
      </FormGroup>
      <FormGroup>
        <FormLabel htmlFor="protocol" required>
          {t('telemetry.fields.protocol')}
        </FormLabel>
        <FormSelect
          id="protocol"
          value={values.protocol}
          onChange={e => updateField('protocol', e.target.value as OtlpProtocol)}
          fieldName="protocol"
          fieldErrors={fieldErrors}
          disabled={!values.enabled}
        >
          <option value="HttpProtobuf">{t('telemetry.protocols.httpProtobuf')}</option>
          <option value="Grpc">{t('telemetry.protocols.grpc')}</option>
        </FormSelect>
      </FormGroup>
      {submitError && <ErrorAlert message={submitError} variant="block" />}
      <Row justify="flex-end">
        <Button type="submit" variant="primary" isLoading={isLoading}>
          {t('telemetry.save')}
        </Button>
      </Row>
    </Form>
  );
}

export function TelemetryPage() {
  const { t } = useTranslation('settings');
  const { data: telemetry, isLoading } = useTelemetry();

  return (
    <Card>
      <CardHeader>
        <CardTitle>{t('telemetry.title')}</CardTitle>
        <Label variant="muted">{t('telemetry.description')}</Label>
        <Label variant="muted">{t('telemetry.disclaimer')}</Label>
      </CardHeader>
      <CardContent>
        {isLoading || !telemetry ? (
          <Row justify="center">
            <Spinner />
          </Row>
        ) : (
          <TelemetryForm key={JSON.stringify(telemetry)} current={telemetry} />
        )}
      </CardContent>
    </Card>
  );
}
