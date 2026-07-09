import { useTranslation } from 'react-i18next';

import { GitHubAppSettingsDto } from '@/api/githubApp';
import { Row, Stack } from '@/components/layout';
import { Banner } from '@/components/ui/Banner';
import { Button } from '@/components/ui/Button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/Card';
import { CodeSpan } from '@/components/ui/CodeSpan';
import { ErrorAlert } from '@/components/ui/ErrorAlert';
import { Form, FormGroup, FormInput, FormLabel } from '@/components/ui/Form';
import { Label } from '@/components/ui/Label';
import { Spinner } from '@/components/ui/Spinner';
import { useForm } from '@/hooks/useForm';
import { useGitHubAppSettings, useUpdateGitHubAppSettings } from '@/hooks/useGitHubApp';

function GitHubAppForm({ current }: { current: GitHubAppSettingsDto }) {
  const { t } = useTranslation('settings');
  const { mutateAsync: updateGitHubApp } = useUpdateGitHubAppSettings();

  const { values, fieldErrors, submitError, isLoading, handleSubmit, updateField } = useForm({
    initialValues: {
      clientId: current.clientId,
      clientSecret: '',
    },
    onSubmit: async values => {
      await updateGitHubApp({
        clientId: values.clientId,
        clientSecret: values.clientSecret.trim() || undefined,
      });
    },
  });

  return (
    <Form onSubmit={handleSubmit} isLoading={isLoading}>
      {!current.redirectUri && (
        <Banner variant="warning" description={t('githubApp.noDomainWarning')} />
      )}
      <FormGroup>
        <FormLabel readOnly>{t('githubApp.fields.redirectUri')}</FormLabel>
        {current.redirectUri ? (
          <CodeSpan copyable>{current.redirectUri}</CodeSpan>
        ) : (
          <Label variant="muted">{t('githubApp.noDomainConfigured')}</Label>
        )}
        <Label variant="muted">{t('githubApp.fields.redirectUriHelp')}</Label>
      </FormGroup>
      <FormGroup>
        <FormLabel htmlFor="clientId" required>
          {t('githubApp.fields.clientId')}
        </FormLabel>
        <FormInput
          id="clientId"
          type="text"
          value={values.clientId}
          onChange={e => updateField('clientId', e.target.value)}
          fieldName="clientId"
          fieldErrors={fieldErrors}
        />
      </FormGroup>
      <FormGroup>
        <FormLabel htmlFor="clientSecret" required={!current.isConfigured}>
          {t('githubApp.fields.clientSecret')}
        </FormLabel>
        <FormInput
          id="clientSecret"
          type="password"
          value={values.clientSecret}
          onChange={e => updateField('clientSecret', e.target.value)}
          placeholder={
            current.isConfigured ? t('githubApp.fields.clientSecretKeepPlaceholder') : undefined
          }
          fieldName="clientSecret"
          fieldErrors={fieldErrors}
        />
        <Label variant="muted">
          {current.isConfigured
            ? t('githubApp.fields.clientSecretKeepHelp')
            : t('githubApp.fields.clientSecretHelp')}
        </Label>
      </FormGroup>
      {submitError && <ErrorAlert message={submitError} variant="block" />}
      <Row justify="flex-end">
        <Button type="submit" variant="primary" isLoading={isLoading}>
          {t('githubApp.save')}
        </Button>
      </Row>
    </Form>
  );
}

export function GitHubAppPage() {
  const { t } = useTranslation('settings');
  const { data: githubApp, isLoading } = useGitHubAppSettings();

  return (
    <Card>
      <CardHeader>
        <CardTitle>{t('githubApp.title')}</CardTitle>
        <Label variant="muted">{t('githubApp.description')}</Label>
      </CardHeader>
      <CardContent>
        <Stack gap='2'>

        <Banner variant="info" description={t('githubApp.explanation')} />
        {isLoading || !githubApp ? (
          <Row justify="center">
            <Spinner />
          </Row>
        ) : (
          <GitHubAppForm key={JSON.stringify(githubApp)} current={githubApp} />
        )}
        </Stack>
      </CardContent>
    </Card>
  );
}
