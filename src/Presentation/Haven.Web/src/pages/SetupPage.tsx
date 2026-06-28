import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';

import { backupsApi, RestoreBackupResult } from '@/api/backups';
import { setupApi, SetupStage, TimeFormat } from '@/api/setup';
import { Stack } from '@/components/layout';
import { CenteredPageLayout } from '@/components/layout/CenteredPageLayout';
import { Button } from '@/components/ui/Button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/Card';
import { Checkbox } from '@/components/ui/Checkbox';
import { CodeSpan } from '@/components/ui/CodeSpan';
import { ErrorAlert } from '@/components/ui/ErrorAlert';
import { Form, FormGroup, FormInput, FormLabel, FormSelect } from '@/components/ui/Form';
import { Spinner } from '@/components/ui/Spinner';
import { useForm } from '@/hooks/useForm';
import { tokenStorage } from '@/lib/tokenStorage';

const TIMEZONES: string[] =
  typeof (Intl as any).supportedValuesOf === 'function'
    ? (Intl as any).supportedValuesOf('timeZone')
    : [
        'UTC',
        'America/New_York',
        'America/Los_Angeles',
        'America/Chicago',
        'Europe/London',
        'Europe/Paris',
        'Europe/Berlin',
        'Asia/Tokyo',
        'Asia/Shanghai',
        'Asia/Kolkata',
        'Australia/Sydney',
        'Pacific/Auckland',
      ];

function StepIndicator({ current, labels }: { current: number; labels: string[] }) {
  return (
    <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '0' }}>
      {labels.map((label, i) => {
        const stepNum = i + 1;
        const isComplete = stepNum < current;
        const isActive = stepNum === current;
        return (
          <div key={label} style={{ display: 'flex', alignItems: 'center' }}>
            <div
              style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '4px' }}
            >
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
            {i < labels.length - 1 && (
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
        );
      })}
    </div>
  );
}

function InstanceStep({ onComplete }: { onComplete: () => void }) {
  const { t } = useTranslation('pages');
  const { values, fieldErrors, submitError, isLoading, handleSubmit, updateField } = useForm({
    initialValues: {
      instanceName: '',
      timezone: new Intl.DateTimeFormat().resolvedOptions().timeZone || 'UTC',
      timeFormat: TimeFormat.Hour12 as TimeFormat,
    },
    onSubmit: async values => {
      await setupApi.configureInstance(values);
    },
    onSuccess: onComplete,
  });

  return (
    <Form onSubmit={handleSubmit} isLoading={isLoading}>
      <FormGroup>
        <FormLabel htmlFor="instanceName" required>
          {t('setup.instance.instanceName')}
        </FormLabel>
        <FormInput
          id="instanceName"
          type="text"
          value={values.instanceName}
          onChange={e => updateField('instanceName', e.target.value)}
          placeholder={t('setup.instance.instanceNamePlaceholder')}
          fieldName="instanceName"
          fieldErrors={fieldErrors}
        />
      </FormGroup>
      <FormGroup>
        <FormLabel htmlFor="timezone" required>
          {t('setup.instance.timezone')}
        </FormLabel>
        <FormSelect
          id="timezone"
          value={values.timezone}
          onChange={e => updateField('timezone', e.target.value)}
          fieldName="timezone"
          fieldErrors={fieldErrors}
        >
          {TIMEZONES.map(tz => (
            <option key={tz} value={tz}>
              {tz}
            </option>
          ))}
        </FormSelect>
      </FormGroup>
      <FormGroup>
        <FormLabel htmlFor="timeFormat" required>
          {t('setup.instance.timeFormat')}
        </FormLabel>
        <FormSelect
          id="timeFormat"
          value={values.timeFormat}
          onChange={e => updateField('timeFormat', e.target.value as TimeFormat)}
          fieldName="timeFormat"
          fieldErrors={fieldErrors}
        >
          <option value={TimeFormat.Hour12}>{t('setup.instance.timeFormat12')}</option>
          <option value={TimeFormat.Hour24}>{t('setup.instance.timeFormat24')}</option>
        </FormSelect>
      </FormGroup>
      {submitError && <ErrorAlert message={submitError} variant="block" />}
      <Button type="submit" variant="primary" isLoading={isLoading} style={{ width: '100%' }}>
        {t('setup.instance.continue')}
      </Button>
    </Form>
  );
}

function SuperUserStep({ onComplete }: { onComplete: () => void }) {
  const { t } = useTranslation('pages');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [confirmError, setConfirmError] = useState<string>();

  const { values, fieldErrors, submitError, isLoading, handleSubmit, updateField } = useForm({
    initialValues: { name: '', email: '', password: '' },
    onSubmit: async values => {
      if (values.password !== confirmPassword) {
        setConfirmError(t('setup.superUser.passwordMismatch'));
        throw new Error(t('setup.superUser.passwordMismatch'));
      }
      setConfirmError(undefined);
      const result = await setupApi.register(values);
      tokenStorage.setTokens(result.accessToken, result.refreshToken);
    },
    onSuccess: onComplete,
  });

  return (
    <Form onSubmit={handleSubmit} isLoading={isLoading}>
      <FormGroup>
        <FormLabel htmlFor="name" required>
          {t('setup.superUser.name')}
        </FormLabel>
        <FormInput
          id="name"
          type="text"
          value={values.name}
          onChange={e => updateField('name', e.target.value)}
          placeholder={t('setup.superUser.namePlaceholder')}
          autoComplete="name"
          fieldName="name"
          fieldErrors={fieldErrors}
        />
      </FormGroup>
      <FormGroup>
        <FormLabel htmlFor="email" required>
          {t('setup.superUser.email')}
        </FormLabel>
        <FormInput
          id="email"
          type="email"
          value={values.email}
          onChange={e => updateField('email', e.target.value)}
          placeholder={t('setup.superUser.emailPlaceholder')}
          autoComplete="email"
          fieldName="email"
          fieldErrors={fieldErrors}
        />
      </FormGroup>
      <FormGroup>
        <FormLabel htmlFor="password" required>
          {t('setup.superUser.password')}
        </FormLabel>
        <FormInput
          id="password"
          type="password"
          value={values.password}
          onChange={e => updateField('password', e.target.value)}
          placeholder={t('setup.superUser.passwordPlaceholder')}
          autoComplete="new-password"
          fieldName="password"
          fieldErrors={fieldErrors}
        />
      </FormGroup>
      <FormGroup>
        <FormLabel htmlFor="confirmPassword" required>
          {t('setup.superUser.confirmPassword')}
        </FormLabel>
        <FormInput
          id="confirmPassword"
          type="password"
          value={confirmPassword}
          onChange={e => {
            setConfirmPassword(e.target.value);
            if (confirmError) setConfirmError(undefined);
          }}
          placeholder={t('setup.superUser.confirmPasswordPlaceholder')}
          autoComplete="new-password"
          error={confirmError}
        />
      </FormGroup>
      {submitError && <ErrorAlert message={submitError} variant="block" />}
      <Button type="submit" variant="primary" isLoading={isLoading} style={{ width: '100%' }}>
        {t('setup.superUser.continue')}
      </Button>
    </Form>
  );
}

function resolveEndpoint(domain: string, port: string, enableTls: boolean): string {
  const protocol = enableTls ? 'https' : 'http';
  const defaultPort = enableTls ? 443 : 80;
  const resolvedHost = domain.trim() || 'localhost';
  const resolvedPort = port ? parseInt(port, 10) : defaultPort;
  const portSuffix = resolvedPort !== defaultPort ? `:${resolvedPort}` : '';
  return `${protocol}://${resolvedHost}${portSuffix}`;
}

function NetworkStep({ onComplete }: { onComplete: () => void }) {
  const { t } = useTranslation('pages');
  const { values, fieldErrors, submitError, isLoading, handleSubmit, updateField } = useForm({
    initialValues: { domain: '', port: '', enableTls: false },
    onSubmit: async values => {
      await setupApi.configureNetwork({
        domain: values.domain || undefined,
        port: values.port ? parseInt(values.port, 10) : undefined,
        enableTls: values.enableTls,
      });
    },
    onSuccess: onComplete,
  });

  const resolvedEndpoint = resolveEndpoint(values.domain, values.port, values.enableTls);

  return (
    <Form onSubmit={handleSubmit} isLoading={isLoading}>
      <FormGroup>
        <FormLabel htmlFor="domain" optional>
          {t('setup.network.domain')}
        </FormLabel>
        <FormInput
          id="domain"
          type="text"
          value={values.domain}
          onChange={e => updateField('domain', e.target.value)}
          placeholder={t('setup.network.domainPlaceholder')}
          fieldName="domain"
          fieldErrors={fieldErrors}
        />
      </FormGroup>
      <FormGroup>
        <FormLabel htmlFor="port" optional>
          {t('setup.network.port')}
        </FormLabel>
        <FormInput
          id="port"
          type="number"
          value={values.port}
          onChange={e => updateField('port', e.target.value)}
          placeholder={t('setup.network.portPlaceholder')}
          min={1}
          max={65535}
          fieldName="port"
          fieldErrors={fieldErrors}
        />
      </FormGroup>
      <FormGroup>
        <Checkbox
          id="enableTls"
          label={t('setup.network.enableTls')}
          description={t('setup.network.enableTlsDescription')}
          checked={values.enableTls}
          onChange={e => updateField('enableTls', e.target.checked)}
        />
      </FormGroup>
      <div
        style={{
          display: 'flex',
          flexDirection: 'column',
          gap: 'var(--space-2)',
          padding: 'var(--space-3)',
          background: 'var(--color-surface)',
          border: '1px solid var(--color-border)',
          borderRadius: 'var(--radius-md)',
          marginBottom: 'var(--space-4)',
        }}
      >
        <span
          style={{
            fontSize: 'var(--font-size-xs)',
            color: 'var(--color-text-muted)',
            fontWeight: 500,
          }}
        >
          {t('setup.network.resolvedEndpoint')}
        </span>
        <CodeSpan copyable>{resolvedEndpoint}</CodeSpan>
      </div>
      {submitError && <ErrorAlert message={submitError} variant="block" />}
      <Button type="submit" variant="primary" isLoading={isLoading} style={{ width: '100%' }}>
        {t('setup.network.finish')}
      </Button>
    </Form>
  );
}

function RestoreChangeList({ label, items }: { label: string; items: { name: string }[] }) {
  if (items.length === 0) return null;
  return (
    <div>
      <span
        style={{
          fontSize: 'var(--font-size-xs)',
          color: 'var(--color-text-muted)',
          fontWeight: 500,
        }}
      >
        {label} ({items.length})
      </span>
      <ul
        style={{
          margin: '4px 0 0',
          paddingLeft: 16,
          fontSize: 'var(--font-size-sm)',
          color: 'var(--color-text)',
        }}
      >
        {items.map(item => (
          <li key={item.name}>{item.name}</li>
        ))}
      </ul>
    </div>
  );
}

function ManifestRestoreStep({
  projectCount,
  onComplete,
}: {
  projectCount: number;
  onComplete: () => void;
}) {
  const { t } = useTranslation('pages');
  const [preview, setPreview] = useState<RestoreBackupResult | null>(null);
  const [isLoadingPreview, setIsLoadingPreview] = useState(false);
  const [isRestoring, setIsRestoring] = useState(false);
  const [error, setError] = useState<string>();

  const loadPreview = async () => {
    setIsLoadingPreview(true);
    setError(undefined);
    try {
      const result = await backupsApi.restore({ source: 'Manifest', dryRun: true });
      setPreview(result);
    } catch (e: any) {
      setError(e?.message ?? t('setup.restore.previewFailed'));
    } finally {
      setIsLoadingPreview(false);
    }
  };

  const handleRestore = async () => {
    setIsRestoring(true);
    setError(undefined);
    try {
      await backupsApi.restore({ source: 'Manifest', dryRun: false });
      onComplete();
    } catch (e: any) {
      setError(e?.message ?? t('setup.restore.restoreFailed'));
      setIsRestoring(false);
    }
  };

  const totalChanges = preview
    ? preview.projects.created.length +
      preview.projects.updated.length +
      preview.environments.created.length +
      preview.environments.updated.length +
      preview.services.created.length +
      preview.services.updated.length
    : 0;

  return (
    <Stack gap="4">
      <p style={{ margin: 0, fontSize: 'var(--font-size-sm)', color: 'var(--color-text-muted)' }}>
        {t('setup.restore.description', { count: projectCount })}
      </p>

      {!preview && (
        <Button
          type="button"
          variant="secondary"
          isLoading={isLoadingPreview}
          onClick={loadPreview}
          style={{ width: '100%' }}
        >
          {t('setup.restore.previewChanges')}
        </Button>
      )}

      {preview && (
        <div
          style={{
            display: 'flex',
            flexDirection: 'column',
            gap: 'var(--space-3)',
            padding: 'var(--space-3)',
            background: 'var(--color-surface)',
            border: '1px solid var(--color-border)',
            borderRadius: 'var(--radius-md)',
          }}
        >
          <span
            style={{
              fontSize: 'var(--font-size-xs)',
              color: 'var(--color-text-muted)',
              fontWeight: 500,
            }}
          >
            {t('setup.restore.previewTitle', { count: totalChanges })}
          </span>
          <RestoreChangeList
            label={t('setup.restore.projectsToCreate')}
            items={preview.projects.created}
          />
          <RestoreChangeList
            label={t('setup.restore.environmentsToCreate')}
            items={preview.environments.created}
          />
          <RestoreChangeList
            label={t('setup.restore.servicesToCreate')}
            items={preview.services.created}
          />
          {totalChanges === 0 && (
            <span style={{ fontSize: 'var(--font-size-sm)', color: 'var(--color-text-muted)' }}>
              {t('setup.restore.noChanges')}
            </span>
          )}
        </div>
      )}

      {error && <ErrorAlert message={error} variant="block" />}

      <div style={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-2)' }}>
        <Button
          type="button"
          variant="primary"
          isLoading={isRestoring}
          onClick={handleRestore}
          style={{ width: '100%' }}
        >
          {t('setup.restore.import')}
        </Button>
        <Button
          type="button"
          variant="ghost"
          onClick={onComplete}
          disabled={isRestoring}
          style={{ width: '100%' }}
        >
          {t('setup.restore.skip')}
        </Button>
      </div>
    </Stack>
  );
}

export function SetupPage() {
  const navigate = useNavigate();
  const { t } = useTranslation('pages');
  const [step, setStep] = useState<1 | 2 | 3 | 4 | null>(null);
  const [manifestProjectCount, setManifestProjectCount] = useState(0);

  useEffect(() => {
    setupApi
      .getStatus()
      .then(res => {
        const stage = res.stage;
        if (stage === SetupStage.Completed) {
          navigate('/login', { replace: true });
        } else if (stage === SetupStage.SuperUserCreated) {
          setStep(3);
        } else if (stage === SetupStage.InstanceConfigured) {
          setStep(2);
        } else {
          setStep(1);
        }
      })
      .catch(() => {
        setStep(1);
      });
  }, [navigate]);

  const handleNetworkComplete = async () => {
    try {
      const manifests = await setupApi.checkManifests();
      if (manifests.available) {
        setManifestProjectCount(manifests.projectCount);
        setStep(4);
        return;
      }
    } catch {
      // ignore — if check fails, proceed to dashboard
    }
    navigate('/dashboard', { replace: true });
  };

  const hasRestoreStep = step === 4;

  const stepLabels = hasRestoreStep
    ? [
        t('setup.steps.instance'),
        t('setup.steps.superUser'),
        t('setup.steps.network'),
        t('setup.steps.restore'),
      ]
    : [t('setup.steps.instance'), t('setup.steps.superUser'), t('setup.steps.network')];

  const stepTitles = hasRestoreStep
    ? [
        t('setup.titles.instance'),
        t('setup.titles.superUser'),
        t('setup.titles.network'),
        t('setup.titles.restore'),
      ]
    : [t('setup.titles.instance'), t('setup.titles.superUser'), t('setup.titles.network')];

  if (step === null) {
    return (
      <CenteredPageLayout>
        <div style={{ display: 'flex', justifyContent: 'center' }}>
          <Spinner size="md" />
        </div>
      </CenteredPageLayout>
    );
  }

  return (
    <CenteredPageLayout>
      <Stack gap="6">
        <div style={{ textAlign: 'center' }}>
          <h1
            style={{
              margin: 0,
              fontFamily: "'Poppins', sans-serif",
              color: 'var(--color-primary)',
            }}
          >
            {t('setup.title')}
          </h1>
          <p
            style={{
              margin: '4px 0 0',
              color: 'var(--color-text-muted)',
              fontSize: 'var(--font-size-sm)',
            }}
          >
            {t('setup.subtitle')}
          </p>
        </div>

        <StepIndicator current={step} labels={stepLabels} />

        <Card>
          <CardHeader>
            <CardTitle>{stepTitles[step - 1]}</CardTitle>
          </CardHeader>
          <CardContent>
            {step === 1 && <InstanceStep onComplete={() => setStep(2)} />}
            {step === 2 && <SuperUserStep onComplete={() => setStep(3)} />}
            {step === 3 && <NetworkStep onComplete={handleNetworkComplete} />}
            {step === 4 && (
              <ManifestRestoreStep
                projectCount={manifestProjectCount}
                onComplete={() => navigate('/dashboard', { replace: true })}
              />
            )}
          </CardContent>
        </Card>
      </Stack>
    </CenteredPageLayout>
  );
}
