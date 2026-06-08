import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { CenteredPageLayout } from '@/components/layout/CenteredPageLayout'
import { Stack } from '@/components/layout'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/Card'
import { Form, FormGroup, FormInput, FormLabel, FormSelect } from '@/components/ui/Form'
import { Checkbox } from '@/components/ui/Checkbox'
import { CodeSpan } from '@/components/ui/CodeSpan'
import { Button } from '@/components/ui/Button'
import { ErrorAlert } from '@/components/ui/ErrorAlert'
import { useForm } from '@/hooks/useForm'
import { setupApi, SetupStage, TimeFormat } from '@/api/setup'
import { tokenStorage } from '@/lib/tokenStorage'

const STEP_LABELS = ['Instance', 'Super User', 'Network']

// eslint-disable-next-line @typescript-eslint/no-explicit-any
const TIMEZONES: string[] = typeof (Intl as any).supportedValuesOf === 'function'
  ? (Intl as any).supportedValuesOf('timeZone')
  : ['UTC', 'America/New_York', 'America/Los_Angeles', 'America/Chicago', 'Europe/London',
     'Europe/Paris', 'Europe/Berlin', 'Asia/Tokyo', 'Asia/Shanghai', 'Asia/Kolkata',
     'Australia/Sydney', 'Pacific/Auckland']

function StepIndicator({ current }: { current: number }) {
  return (
    <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '0' }}>
      {STEP_LABELS.map((label, i) => {
        const stepNum = i + 1
        const isComplete = stepNum < current
        const isActive = stepNum === current
        return (
          <div key={label} style={{ display: 'flex', alignItems: 'center' }}>
            <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '4px' }}>
              <div
                style={{
                  width: 28,
                  height: 28,
                  borderRadius: '50%',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  fontSize: 'var(--font-size-sm)',
                  fontWeight: 600,
                  background: isComplete
                    ? 'var(--color-primary)'
                    : isActive
                      ? 'var(--color-primary)'
                      : 'var(--color-surface-raised)',
                  color: isComplete || isActive ? '#fff' : 'var(--color-text-muted)',
                  border: isActive ? '2px solid var(--color-primary)' : '2px solid transparent',
                  opacity: isComplete || isActive ? 1 : 0.5,
                }}
              >
                {isComplete ? '✓' : stepNum}
              </div>
              <span
                style={{
                  fontSize: 'var(--font-size-xs)',
                  color: isActive ? 'var(--color-primary)' : 'var(--color-text-muted)',
                  fontWeight: isActive ? 600 : 400,
                }}
              >
                {label}
              </span>
            </div>
            {i < STEP_LABELS.length - 1 && (
              <div
                style={{
                  width: 48,
                  height: 2,
                  background: stepNum < current ? 'var(--color-primary)' : 'var(--color-border)',
                  margin: '0 4px',
                  marginBottom: 20,
                }}
              />
            )}
          </div>
        )
      })}
    </div>
  )
}

function InstanceStep({ onComplete }: { onComplete: () => void }) {
  const { values, fieldErrors, submitError, isLoading, handleSubmit, updateField } = useForm({
    initialValues: {
      instanceName: '',
      timezone: new Intl.DateTimeFormat().resolvedOptions().timeZone || 'UTC',
      timeFormat: TimeFormat.Hour12 as TimeFormat,
    },
    onSubmit: async (values) => {
      await setupApi.configureInstance(values)
    },
    onSuccess: onComplete,
  })

  return (
    <Form onSubmit={handleSubmit} isLoading={isLoading}>
      <FormGroup>
        <FormLabel htmlFor="instanceName" required>Instance Name</FormLabel>
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
      <FormGroup>
        <FormLabel htmlFor="timezone" required>Timezone</FormLabel>
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
      <FormGroup>
        <FormLabel htmlFor="timeFormat" required>Time Format</FormLabel>
        <FormSelect
          id="timeFormat"
          value={values.timeFormat}
          onChange={(e) => updateField('timeFormat', e.target.value as TimeFormat)}
          fieldName="timeFormat"
          fieldErrors={fieldErrors}
        >
          <option value={TimeFormat.Hour12}>12-hour (1:00 PM)</option>
          <option value={TimeFormat.Hour24}>24-hour (13:00)</option>
        </FormSelect>
      </FormGroup>
      {submitError && <ErrorAlert message={submitError} variant="block" />}
      <Button type="submit" variant="primary" isLoading={isLoading} style={{ width: '100%' }}>
        Continue
      </Button>
    </Form>
  )
}

function SuperUserStep({ onComplete }: { onComplete: () => void }) {
  const [confirmPassword, setConfirmPassword] = useState('')
  const [confirmError, setConfirmError] = useState<string>()

  const { values, fieldErrors, submitError, isLoading, handleSubmit, updateField } = useForm({
    initialValues: { name: '', email: '', password: '' },
    onSubmit: async (values) => {
      if (values.password !== confirmPassword) {
        setConfirmError('Passwords do not match.')
        throw new Error('Passwords do not match.')
      }
      setConfirmError(undefined)
      const result = await setupApi.register(values)
      tokenStorage.setTokens(result.accessToken, result.refreshToken)
    },
    onSuccess: onComplete,
  })

  return (
    <Form onSubmit={handleSubmit} isLoading={isLoading}>
      <FormGroup>
        <FormLabel htmlFor="name" required>Name</FormLabel>
        <FormInput
          id="name"
          type="text"
          value={values.name}
          onChange={(e) => updateField('name', e.target.value)}
          placeholder="Your name"
          autoComplete="name"
          fieldName="name"
          fieldErrors={fieldErrors}
        />
      </FormGroup>
      <FormGroup>
        <FormLabel htmlFor="email" required>Email</FormLabel>
        <FormInput
          id="email"
          type="email"
          value={values.email}
          onChange={(e) => updateField('email', e.target.value)}
          placeholder="you@example.com"
          autoComplete="email"
          fieldName="email"
          fieldErrors={fieldErrors}
        />
      </FormGroup>
      <FormGroup>
        <FormLabel htmlFor="password" required>Password</FormLabel>
        <FormInput
          id="password"
          type="password"
          value={values.password}
          onChange={(e) => updateField('password', e.target.value)}
          placeholder="Min. 8 characters"
          autoComplete="new-password"
          fieldName="password"
          fieldErrors={fieldErrors}
        />
      </FormGroup>
      <FormGroup>
        <FormLabel htmlFor="confirmPassword" required>Confirm Password</FormLabel>
        <FormInput
          id="confirmPassword"
          type="password"
          value={confirmPassword}
          onChange={(e) => {
            setConfirmPassword(e.target.value)
            if (confirmError) setConfirmError(undefined)
          }}
          placeholder="Repeat your password"
          autoComplete="new-password"
          error={confirmError}
        />
      </FormGroup>
      {submitError && <ErrorAlert message={submitError} variant="block" />}
      <Button type="submit" variant="primary" isLoading={isLoading} style={{ width: '100%' }}>
        Continue
      </Button>
    </Form>
  )
}

function resolveEndpoint(domain: string, port: string, enableTls: boolean): string {
  const protocol = enableTls ? 'https' : 'http'
  const defaultPort = enableTls ? 443 : 80
  const resolvedHost = domain.trim() || 'localhost'
  const resolvedPort = port ? parseInt(port, 10) : defaultPort
  const portSuffix = resolvedPort !== defaultPort ? `:${resolvedPort}` : ''
  return `${protocol}://${resolvedHost}${portSuffix}`
}

function NetworkStep({ onComplete }: { onComplete: () => void }) {
  const { values, fieldErrors, submitError, isLoading, handleSubmit, updateField } = useForm({
    initialValues: { domain: '', port: '', enableTls: false },
    onSubmit: async (values) => {
      await setupApi.configureNetwork({
        domain: values.domain || undefined,
        port: values.port ? parseInt(values.port, 10) : undefined,
        enableTls: values.enableTls,
      })
    },
    onSuccess: onComplete,
  })

  const resolvedEndpoint = resolveEndpoint(values.domain, values.port, values.enableTls)

  return (
    <Form onSubmit={handleSubmit} isLoading={isLoading}>
      <FormGroup>
        <FormLabel htmlFor="domain" optional>Domain</FormLabel>
        <FormInput
          id="domain"
          type="text"
          value={values.domain}
          onChange={(e) => updateField('domain', e.target.value)}
          placeholder="haven.example.com"
          fieldName="domain"
          fieldErrors={fieldErrors}
        />
      </FormGroup>
      <FormGroup>
        <FormLabel htmlFor="port" optional>Port</FormLabel>
        <FormInput
          id="port"
          type="number"
          value={values.port}
          onChange={(e) => updateField('port', e.target.value)}
          placeholder="8080"
          min={1}
          max={65535}
          fieldName="port"
          fieldErrors={fieldErrors}
        />
      </FormGroup>
      <FormGroup>
        <Checkbox
          id="enableTls"
          label="Enable TLS"
          description="Serve the application over HTTPS. Used for URL generation and webhook addresses."
          checked={values.enableTls}
          onChange={(e) => updateField('enableTls', e.target.checked)}
        />
      </FormGroup>
      <div style={{
        display: 'flex',
        flexDirection: 'column',
        gap: 'var(--space-2)',
        padding: 'var(--space-3)',
        background: 'var(--color-surface)',
        border: '1px solid var(--color-border)',
        borderRadius: 'var(--radius-md)',
        marginBottom: 'var(--space-4)',
      }}>
        <span style={{ fontSize: 'var(--font-size-xs)', color: 'var(--color-text-muted)', fontWeight: 500 }}>
          Resolved endpoint
        </span>
        <CodeSpan copyable>{resolvedEndpoint}</CodeSpan>
      </div>
      {submitError && <ErrorAlert message={submitError} variant="block" />}
      <Button type="submit" variant="primary" isLoading={isLoading} style={{ width: '100%' }}>
        Finish Setup
      </Button>
    </Form>
  )
}

const STEP_TITLES = ['Instance Configuration', 'Super User Setup', 'Network & Access']

export function SetupPage() {
  const navigate = useNavigate()
  const [step, setStep] = useState<1 | 2 | 3 | null>(null)

  useEffect(() => {
    setupApi.getStatus().then((res) => {
      const stage = res.stage
      if (stage === SetupStage.Completed) {
        navigate('/login', { replace: true })
      } else if (stage === SetupStage.SuperUserCreated) {
        setStep(3)
      } else if (stage === SetupStage.InstanceConfigured) {
        setStep(2)
      } else {
        setStep(1)
      }
    }).catch(() => {
      setStep(1)
    })
  }, [navigate])

  if (step === null) {
    return (
      <CenteredPageLayout>
        <div style={{ textAlign: 'center', color: 'var(--color-text-muted)' }}>Loading...</div>
      </CenteredPageLayout>
    )
  }

  return (
    <CenteredPageLayout>
      <Stack gap="6">
        <div style={{ textAlign: 'center' }}>
          <h1 style={{ margin: 0, fontFamily: "'Poppins', sans-serif", color: 'var(--color-primary)' }}>
            Haven
          </h1>
          <p style={{ margin: '4px 0 0', color: 'var(--color-text-muted)', fontSize: 'var(--font-size-sm)' }}>
            Complete the setup to get started
          </p>
        </div>

        <StepIndicator current={step} />

        <Card>
          <CardHeader>
            <CardTitle>{STEP_TITLES[step - 1]}</CardTitle>
          </CardHeader>
          <CardContent>
            {step === 1 && <InstanceStep onComplete={() => setStep(2)} />}
            {step === 2 && <SuperUserStep onComplete={() => setStep(3)} />}
            {step === 3 && <NetworkStep onComplete={() => navigate('/dashboard', { replace: true })} />}
          </CardContent>
        </Card>
      </Stack>
    </CenteredPageLayout>
  )
}
