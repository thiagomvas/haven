import { useNavigate } from 'react-router-dom';
import { CenteredPageLayout } from '@/components/layout/CenteredPageLayout';
import { Stack } from '@/components/layout';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/Card';
import { Form, FormGroup, FormInput, FormLabel } from '@/components/ui/Form';
import { Button } from '@/components/ui/Button';
import { ErrorAlert } from '@/components/ui/ErrorAlert';
import { useForm } from '@/hooks/useForm';
import { authApi } from '@/api/auth';
import { tokenStorage } from '@/lib/tokenStorage';

export function LoginPage() {
  const navigate = useNavigate();

  const { values, fieldErrors, submitError, isLoading, handleSubmit, updateField } = useForm({
    initialValues: { email: '', password: '' },
    onSubmit: async values => {
      const result = await authApi.login(values);
      tokenStorage.setTokens(result.accessToken, result.refreshToken);
    },
    onSuccess: () => navigate('/dashboard', { replace: true }),
  });

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
            Haven
          </h1>
          <p
            style={{
              margin: '4px 0 0',
              color: 'var(--color-text-muted)',
              fontSize: 'var(--font-size-sm)',
            }}
          >
            Sign in to your account
          </p>
        </div>

        <Card>
          <CardHeader>
            <CardTitle>Sign in</CardTitle>
          </CardHeader>
          <CardContent>
            <Form onSubmit={handleSubmit} isLoading={isLoading}>
              <FormGroup>
                <FormLabel htmlFor="email" required>
                  Email
                </FormLabel>
                <FormInput
                  id="email"
                  type="email"
                  value={values.email}
                  onChange={e => updateField('email', e.target.value)}
                  placeholder="you@example.com"
                  autoComplete="email"
                  fieldName="email"
                  fieldErrors={fieldErrors}
                />
              </FormGroup>
              <FormGroup>
                <FormLabel htmlFor="password" required>
                  Password
                </FormLabel>
                <FormInput
                  id="password"
                  type="password"
                  value={values.password}
                  onChange={e => updateField('password', e.target.value)}
                  placeholder="••••••••"
                  autoComplete="current-password"
                  fieldName="password"
                  fieldErrors={fieldErrors}
                />
              </FormGroup>
              {submitError && <ErrorAlert message={submitError} variant="block" />}
              <Button
                type="submit"
                variant="primary"
                isLoading={isLoading}
                style={{ width: '100%' }}
              >
                Sign in
              </Button>
            </Form>
          </CardContent>
        </Card>
      </Stack>
    </CenteredPageLayout>
  );
}
