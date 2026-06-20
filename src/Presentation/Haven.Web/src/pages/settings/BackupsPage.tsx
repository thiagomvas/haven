import { useState } from 'react';
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
import { useBackupOptions, useUpdateBackupOptions, useCreateBackup } from '@/hooks/useBackups';
import { useGitCredentials } from '@/hooks/useGitCredentials';
import { BackupOptions } from '@/api/backups';

const CRON_PRESETS = [
  { label: 'schedule.presets.daily', value: '0 0 * * *' },
  { label: 'schedule.presets.twiceDaily', value: '0 0,12 * * *' },
  { label: 'schedule.presets.weekly', value: '0 0 * * 0' },
  { label: 'schedule.presets.monthly', value: '0 0 1 * *' },
  { label: 'schedule.presets.custom', value: 'custom' },
] as const;

function resolvePreset(cron: string): string {
  return CRON_PRESETS.find(p => p.value === cron)?.value ?? 'custom';
}

function BackupOptionsForm({ current }: { current: BackupOptions }) {
  const { t } = useTranslation('settings');
  const { mutateAsync: updateOptions } = useUpdateBackupOptions();

  const initialPreset = resolvePreset(current.cronExpression);

  const { values, fieldErrors, submitError, isLoading, handleSubmit, updateField } = useForm({
    initialValues: {
      enabled: current.enabled,
      backupsPath: current.backupsPath,
      retentionCount: current.retentionCount,
      cronExpression: current.cronExpression,
      cronPreset: initialPreset,
      gitEnabled: current.git.enabled,
      gitRemoteUrl: current.git.remoteUrl ?? '',
      gitBranch: current.git.branch,
      gitCredentialsId: current.git.gitCredentialsId ?? '',
    },
    onSubmit: async values => {
      const options: BackupOptions = {
        enabled: values.enabled,
        backupsPath: values.backupsPath,
        retentionCount: values.retentionCount,
        cronExpression: values.cronExpression,
        git: {
          enabled: values.gitEnabled,
          remoteUrl: values.gitRemoteUrl || undefined,
          branch: values.gitBranch,
          gitCredentialsId: values.gitCredentialsId || undefined,
        },
      };
      await updateOptions(options);
    },
  });

  function handlePresetChange(preset: string) {
    updateField('cronPreset', preset);
    if (preset !== 'custom') {
      updateField('cronExpression', preset);
    }
  }

  const { data: credentialsPage } = useGitCredentials({ pageNumber: 1, pageSize: 100 });
  const credentials = credentialsPage?.items ?? [];

  return (
    <Form onSubmit={handleSubmit} isLoading={isLoading}>
      <FormGroup>
        <Checkbox
          label={t('backups.fields.enabled')}
          description={t('backups.fields.enabledDescription')}
          checked={values.enabled}
          onChange={e => updateField('enabled', e.target.checked)}
        />
      </FormGroup>
      <FormGroup>
        <FormLabel htmlFor="backupsPath" required>
          {t('backups.fields.backupsPath')}
        </FormLabel>
        <FormInput
          id="backupsPath"
          type="text"
          value={values.backupsPath}
          onChange={e => updateField('backupsPath', e.target.value)}
          placeholder="/var/lib/haven/backups"
          fieldName="backupsPath"
          fieldErrors={fieldErrors}
          disabled={!values.enabled}
        />
      </FormGroup>
      <FormGroup>
        <FormLabel htmlFor="retentionCount" required>
          {t('backups.fields.retentionCount')}
        </FormLabel>
        <FormInput
          id="retentionCount"
          type="number"
          value={values.retentionCount}
          onChange={e => updateField('retentionCount', Number(e.target.value))}
          min={1}
          fieldName="retentionCount"
          fieldErrors={fieldErrors}
          disabled={!values.enabled}
        />
      </FormGroup>

      <div style={{ marginTop: 'var(--space-4)', marginBottom: 'var(--space-2)' }}>
        <Label variant="muted">{t('backups.schedule.sectionTitle')}</Label>
      </div>

      <FormGroup>
        <FormLabel htmlFor="cronPreset" required>
          {t('backups.schedule.preset')}
        </FormLabel>
        <FormSelect
          id="cronPreset"
          value={values.cronPreset}
          onChange={e => handlePresetChange(e.target.value)}
          fieldName="cronPreset"
          fieldErrors={fieldErrors}
          disabled={!values.enabled}
        >
          {CRON_PRESETS.map(p => (
            <option key={p.value} value={p.value}>
              {t(`backups.${p.label}`)}
            </option>
          ))}
        </FormSelect>
      </FormGroup>

      {values.cronPreset === 'custom' && (
        <FormGroup>
          <FormLabel htmlFor="cronExpression" required>
            {t('backups.schedule.customCron')}
          </FormLabel>
          <FormInput
            id="cronExpression"
            type="text"
            value={values.cronExpression}
            onChange={e => updateField('cronExpression', e.target.value)}
            placeholder="0 0 * * *"
            fieldName="cronExpression"
            fieldErrors={fieldErrors}
            disabled={!values.enabled}
          />
        </FormGroup>
      )}

      <div style={{ marginTop: 'var(--space-4)', marginBottom: 'var(--space-2)' }}>
        <Label variant="muted">{t('backups.git.sectionTitle')}</Label>
      </div>

      <FormGroup>
        <Checkbox
          label={t('backups.git.enabled')}
          description={t('backups.git.enabledDescription')}
          checked={values.gitEnabled}
          onChange={e => updateField('gitEnabled', e.target.checked)}
          disabled={!values.enabled}
        />
      </FormGroup>
      <FormGroup>
        <FormLabel htmlFor="gitRemoteUrl">{t('backups.git.remoteUrl')}</FormLabel>
        <FormInput
          id="gitRemoteUrl"
          type="text"
          value={values.gitRemoteUrl}
          onChange={e => updateField('gitRemoteUrl', e.target.value)}
          placeholder="https://github.com/org/repo.git"
          fieldName="gitRemoteUrl"
          fieldErrors={fieldErrors}
          disabled={!values.enabled || !values.gitEnabled}
        />
      </FormGroup>
      <FormGroup>
        <FormLabel htmlFor="gitBranch" required>
          {t('backups.git.branch')}
        </FormLabel>
        <FormInput
          id="gitBranch"
          type="text"
          value={values.gitBranch}
          onChange={e => updateField('gitBranch', e.target.value)}
          placeholder="main"
          fieldName="gitBranch"
          fieldErrors={fieldErrors}
          disabled={!values.enabled || !values.gitEnabled}
        />
      </FormGroup>
      <FormGroup>
        <FormLabel htmlFor="gitCredentialsId">{t('backups.git.credentials')}</FormLabel>
        <FormSelect
          id="gitCredentialsId"
          value={values.gitCredentialsId}
          onChange={e => updateField('gitCredentialsId', e.target.value)}
          fieldName="gitCredentialsId"
          fieldErrors={fieldErrors}
          disabled={!values.enabled || !values.gitEnabled}
        >
          <option value="">{t('backups.git.noCredentials')}</option>
          {credentials.map(cred => (
            <option key={cred.id} value={cred.id}>
              {cred.displayName}
            </option>
          ))}
        </FormSelect>
      </FormGroup>

      {submitError && <ErrorAlert message={submitError} variant="block" />}
      <Row justify="flex-end">
        <Button type="submit" variant="primary" isLoading={isLoading}>
          {t('backups.save')}
        </Button>
      </Row>
    </Form>
  );
}

function ManualBackupCard() {
  const { t } = useTranslation('settings');
  const { mutateAsync: createBackup, isPending } = useCreateBackup();
  const [lastSnapshot, setLastSnapshot] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function handleCreate() {
    setError(null);
    try {
      const result = await createBackup();
      setLastSnapshot(result.snapshotPath);
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : t('backups.manual.failed'));
    }
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>{t('backups.manual.title')}</CardTitle>
        <Label variant="muted">{t('backups.manual.description')}</Label>
      </CardHeader>
      <CardContent>
        {lastSnapshot && (
          <div style={{ marginBottom: 'var(--space-3)' }}>
            <Label variant="muted">
              {t('backups.manual.snapshotPath', { path: lastSnapshot })}
            </Label>
          </div>
        )}
        {error && <ErrorAlert message={error} variant="block" />}
        <Row justify="flex-end">
          <Button variant="secondary" onClick={handleCreate} isLoading={isPending}>
            {t('backups.manual.trigger')}
          </Button>
        </Row>
      </CardContent>
    </Card>
  );
}

export function BackupsPage() {
  const { t } = useTranslation('settings');
  const { data: options, isLoading } = useBackupOptions();

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-4)' }}>
      <Card>
        <CardHeader>
          <CardTitle>{t('backups.title')}</CardTitle>
          <Label variant="muted">{t('backups.description')}</Label>
        </CardHeader>
        <CardContent>
          {isLoading || !options ? (
            <Row justify="center">
              <Spinner />
            </Row>
          ) : (
            <BackupOptionsForm key={JSON.stringify(options)} current={options} />
          )}
        </CardContent>
      </Card>
      <ManualBackupCard />
    </div>
  );
}
