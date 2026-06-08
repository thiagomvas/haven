import { useTranslation } from 'react-i18next'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/Card'
import { Form, FormGroup, FormLabel, FormInput, FormSelect } from '@/components/ui/Form'
import { Label } from '@/components/ui/Label'
import { Button } from '@/components/ui/Button'
import { Spinner } from '@/components/ui/Spinner'
import { Row } from '@/components/layout'
import { ErrorAlert } from '@/components/ui/ErrorAlert'
import { useForm } from '@/hooks/useForm'
import { useInstance, useUpdateInstance } from '@/hooks/useInstance'
import { InstanceDto } from '@/api/instance'
import { TimeFormat } from '@/api/setup'

// eslint-disable-next-line @typescript-eslint/no-explicit-any
const TIMEZONES: string[] = typeof (Intl as any).supportedValuesOf === 'function'
  ? (Intl as any).supportedValuesOf('timeZone')
  : ['UTC', 'America/New_York', 'America/Los_Angeles', 'America/Chicago', 'Europe/London',
     'Europe/Paris', 'Europe/Berlin', 'Asia/Tokyo', 'Asia/Shanghai', 'Asia/Kolkata',
     'Australia/Sydney', 'Pacific/Auckland']

function InstanceForm({ current }: { current: InstanceDto }) {
  const { t } = useTranslation('settings')
  const { mutateAsync: updateInstance } = useUpdateInstance()

  const { values, fieldErrors, submitError, isLoading, handleSubmit, updateField } = useForm({
    initialValues: {
      instanceName: current.instanceName,
      timezone: current.timezone,
      timeFormat: current.timeFormat,
    },
    onSubmit: async (values) => {
      await updateInstance(values)
    },
  })

  return (
    <Form onSubmit={handleSubmit} isLoading={isLoading}>
      <FormGroup>
        <FormLabel htmlFor="instanceName" required>{t('instance.fields.instanceName')}</FormLabel>
        <FormInput
          id="instanceName"
          type="text"
          value={values.instanceName}
          onChange={(e) => updateField('instanceName', e.target.value)}
          placeholder="My Haven"
          fieldName="instanceName"
          fieldErrors={fieldErrors}
        />
      </FormGroup>
      <div style={{ display: 'flex', gap: 'var(--space-4)' }}>
        <div style={{ flex: 1 }}>
          <FormGroup>
            <FormLabel htmlFor="timezone" required>{t('instance.fields.timezone')}</FormLabel>
            <FormSelect
              id="timezone"
              value={values.timezone}
              onChange={(e) => updateField('timezone', e.target.value)}
              fieldName="timezone"
              fieldErrors={fieldErrors}
            >
              {TIMEZONES.map((tz) => (
                <option key={tz} value={tz}>{tz}</option>
              ))}
            </FormSelect>
          </FormGroup>
        </div>
        <div style={{ flex: 1 }}>
          <FormGroup>
            <FormLabel htmlFor="timeFormat" required>{t('instance.fields.timeFormat')}</FormLabel>
            <FormSelect
              id="timeFormat"
              value={values.timeFormat}
              onChange={(e) => updateField('timeFormat', e.target.value as TimeFormat)}
              fieldName="timeFormat"
              fieldErrors={fieldErrors}
            >
              <option value={TimeFormat.Hour12}>{t('instance.timeFormats.hour12')}</option>
              <option value={TimeFormat.Hour24}>{t('instance.timeFormats.hour24')}</option>
            </FormSelect>
          </FormGroup>
        </div>
      </div>
      {submitError && <ErrorAlert message={submitError} variant="block" />}
      <Row justify="flex-end">
        <Button type="submit" variant="primary" isLoading={isLoading}>
          {t('instance.save')}
        </Button>
      </Row>
    </Form>
  )
}

export function InstancePage() {
  const { t } = useTranslation('settings')
  const { data: instance, isLoading } = useInstance()

  return (
    <Card>
      <CardHeader>
        <CardTitle>{t('instance.title')}</CardTitle>
        <Label variant="muted">{t('instance.description')}</Label>
      </CardHeader>
      <CardContent>
        {isLoading || !instance ? (
          <Row justify="center"><Spinner /></Row>
        ) : (
          <InstanceForm current={instance} />
        )}
      </CardContent>
    </Card>
  )
}
