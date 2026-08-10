import { useTranslation } from 'react-i18next';

import { DockerCleanupOptions } from '@/api/dockerCleanup';
import { RepositoryCleanupOptions } from '@/api/repositoryCleanup';
import { Row, Stack } from '@/components/layout';
import { Button } from '@/components/ui/Button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/Card';
import { Checkbox } from '@/components/ui/Checkbox';
import { ErrorAlert } from '@/components/ui/ErrorAlert';
import { Form, FormGroup, FormInput, FormLabel } from '@/components/ui/Form';
import { Label } from '@/components/ui/Label';
import { Spinner } from '@/components/ui/Spinner';
import { useDockerCleanupOptions, useUpdateDockerCleanupOptions } from '@/hooks/useDockerCleanup';
import { useForm } from '@/hooks/useForm';
import {
  useRepositoryCleanupOptions,
  useUpdateRepositoryCleanupOptions,
} from '@/hooks/useRepositoryCleanup';

function DockerCleanupForm({ current }: { current: DockerCleanupOptions }) {
  const { t } = useTranslation('settings');
  const { mutateAsync: updateDockerCleanup } = useUpdateDockerCleanupOptions();

  const { values, fieldErrors, submitError, isLoading, handleSubmit, updateField } = useForm({
    initialValues: {
      enabled: current.enabled,
      cronExpression: current.cronExpression,
      gracePeriodHours: current.gracePeriodHours,
      dryRun: current.dryRun,
    },
    onSubmit: async values => {
      const options: DockerCleanupOptions = {
        enabled: values.enabled,
        cronExpression: values.cronExpression,
        gracePeriodHours: values.gracePeriodHours,
        dryRun: values.dryRun,
      };
      await updateDockerCleanup(options);
    },
  });

  return (
    <Form onSubmit={handleSubmit} isLoading={isLoading}>
      <FormGroup>
        <Checkbox
          label={t('dockerCleanup.fields.enabled')}
          description={t('dockerCleanup.fields.enabledDescription')}
          checked={values.enabled}
          onChange={e => updateField('enabled', e.target.checked)}
        />
      </FormGroup>
      <FormGroup>
        <FormLabel htmlFor="cronExpression" required>
          {t('dockerCleanup.fields.cronExpression')}
        </FormLabel>
        <FormInput
          id="cronExpression"
          type="text"
          value={values.cronExpression}
          onChange={e => updateField('cronExpression', e.target.value)}
          placeholder="0 3 * * *"
          fieldName="cronExpression"
          fieldErrors={fieldErrors}
          disabled={!values.enabled}
        />
      </FormGroup>
      <FormGroup>
        <FormLabel htmlFor="gracePeriodHours" required>
          {t('dockerCleanup.fields.gracePeriodHours')}
        </FormLabel>
        <Label variant="muted">{t('dockerCleanup.fields.gracePeriodHoursDescription')}</Label>
        <FormInput
          id="gracePeriodHours"
          type="number"
          value={values.gracePeriodHours}
          onChange={e => updateField('gracePeriodHours', Number(e.target.value))}
          min={0}
          fieldName="gracePeriodHours"
          fieldErrors={fieldErrors}
          disabled={!values.enabled}
        />
      </FormGroup>
      <FormGroup>
        <Checkbox
          label={t('dockerCleanup.fields.dryRun')}
          description={t('dockerCleanup.fields.dryRunDescription')}
          checked={values.dryRun}
          onChange={e => updateField('dryRun', e.target.checked)}
          disabled={!values.enabled}
        />
      </FormGroup>
      {submitError && <ErrorAlert message={submitError} variant="block" />}
      <Row justify="flex-end">
        <Button type="submit" variant="primary" isLoading={isLoading}>
          {t('dockerCleanup.save')}
        </Button>
      </Row>
    </Form>
  );
}

function DockerCleanupCard() {
  const { t } = useTranslation('settings');
  const { data: dockerCleanup, isLoading } = useDockerCleanupOptions();

  return (
    <Card>
      <CardHeader>
        <CardTitle>{t('dockerCleanup.title')}</CardTitle>
        <Label variant="muted">{t('dockerCleanup.description')}</Label>
      </CardHeader>
      <CardContent>
        {isLoading || !dockerCleanup ? (
          <Row justify="center">
            <Spinner />
          </Row>
        ) : (
          <DockerCleanupForm key={JSON.stringify(dockerCleanup)} current={dockerCleanup} />
        )}
      </CardContent>
    </Card>
  );
}

function RepositoryCleanupForm({ current }: { current: RepositoryCleanupOptions }) {
  const { t } = useTranslation('settings');
  const { mutateAsync: updateRepositoryCleanup } = useUpdateRepositoryCleanupOptions();

  const { values, fieldErrors, submitError, isLoading, handleSubmit, updateField } = useForm({
    initialValues: {
      enabled: current.enabled,
      cronExpression: current.cronExpression,
      gracePeriodHours: current.gracePeriodHours,
      dryRun: current.dryRun,
    },
    onSubmit: async values => {
      const options: RepositoryCleanupOptions = {
        enabled: values.enabled,
        cronExpression: values.cronExpression,
        gracePeriodHours: values.gracePeriodHours,
        dryRun: values.dryRun,
      };
      await updateRepositoryCleanup(options);
    },
  });

  return (
    <Form onSubmit={handleSubmit} isLoading={isLoading}>
      <FormGroup>
        <Checkbox
          label={t('repositoryCleanup.fields.enabled')}
          description={t('repositoryCleanup.fields.enabledDescription')}
          checked={values.enabled}
          onChange={e => updateField('enabled', e.target.checked)}
        />
      </FormGroup>
      <FormGroup>
        <FormLabel htmlFor="repositoryCronExpression" required>
          {t('repositoryCleanup.fields.cronExpression')}
        </FormLabel>
        <FormInput
          id="repositoryCronExpression"
          type="text"
          value={values.cronExpression}
          onChange={e => updateField('cronExpression', e.target.value)}
          placeholder="0 4 * * *"
          fieldName="cronExpression"
          fieldErrors={fieldErrors}
          disabled={!values.enabled}
        />
      </FormGroup>
      <FormGroup>
        <FormLabel htmlFor="repositoryGracePeriodHours" required>
          {t('repositoryCleanup.fields.gracePeriodHours')}
        </FormLabel>
        <Label variant="muted">{t('repositoryCleanup.fields.gracePeriodHoursDescription')}</Label>
        <FormInput
          id="repositoryGracePeriodHours"
          type="number"
          value={values.gracePeriodHours}
          onChange={e => updateField('gracePeriodHours', Number(e.target.value))}
          min={0}
          fieldName="gracePeriodHours"
          fieldErrors={fieldErrors}
          disabled={!values.enabled}
        />
      </FormGroup>
      <FormGroup>
        <Checkbox
          label={t('repositoryCleanup.fields.dryRun')}
          description={t('repositoryCleanup.fields.dryRunDescription')}
          checked={values.dryRun}
          onChange={e => updateField('dryRun', e.target.checked)}
          disabled={!values.enabled}
        />
      </FormGroup>
      {submitError && <ErrorAlert message={submitError} variant="block" />}
      <Row justify="flex-end">
        <Button type="submit" variant="primary" isLoading={isLoading}>
          {t('repositoryCleanup.save')}
        </Button>
      </Row>
    </Form>
  );
}

function RepositoryCleanupCard() {
  const { t } = useTranslation('settings');
  const { data: repositoryCleanup, isLoading } = useRepositoryCleanupOptions();

  return (
    <Card>
      <CardHeader>
        <CardTitle>{t('repositoryCleanup.title')}</CardTitle>
        <Label variant="muted">{t('repositoryCleanup.description')}</Label>
      </CardHeader>
      <CardContent>
        {isLoading || !repositoryCleanup ? (
          <Row justify="center">
            <Spinner />
          </Row>
        ) : (
          <RepositoryCleanupForm
            key={JSON.stringify(repositoryCleanup)}
            current={repositoryCleanup}
          />
        )}
      </CardContent>
    </Card>
  );
}

/**
 * Groups small, independent maintenance-related configs as separate cards on one tab, so future
 * additions in this space don't each need their own settings tab.
 */
export function MaintenancePage() {
  return (
    <Stack gap="4">
      <DockerCleanupCard />
      <RepositoryCleanupCard />
    </Stack>
  );
}
