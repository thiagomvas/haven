import { useNavigate, useSearchParams } from 'react-router-dom';

import { authApi } from '@/api/auth';
import { Stack } from '@/components/layout';
import { CenteredPageLayout } from '@/components/layout/CenteredPageLayout';
import { Button } from '@/components/ui/Button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/Card';
import { ErrorAlert } from '@/components/ui/ErrorAlert';
import { Form, FormGroup, FormInput, FormLabel } from '@/components/ui/Form';
import { Label } from '@/components/ui/Label';
import { useForm } from '@/hooks/useForm';
import { tokenStorage } from '@/lib/tokenStorage';

export function AcceptInvitePage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const token = searchParams.get('token');

  const { values, fieldErrors, submitError, isLoading, handleSubmit, updateField } = useForm({
    initialValues: { name: '', password: '', confirmPassword: '' },
    onSubmit: async values => {
      const result = await authApi.acceptInvite({ token: token ?? '', ...values });
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
            Set up your account
          </p>
        </div>

        <Card>
          <CardHeader>
            <CardTitle>Welcome to Haven</CardTitle>
            <Label variant="muted">Choose a name and password to activate your account.</Label>
          </CardHeader>
          <CardContent>
            {!token ? (
              <ErrorAlert
                message="This invite link is missing its token. Ask an admin to resend your invite."
                variant="block"
              />
            ) : (
              <Form onSubmit={handleSubmit} isLoading={isLoading}>
                <FormGroup>
                  <FormLabel htmlFor="name" required>
                    Name
                  </FormLabel>
                  <FormInput
                    id="name"
                    type="text"
                    value={values.name}
                    onChange={e => updateField('name', e.target.value)}
                    placeholder="John Doe"
                    autoComplete="name"
                    fieldName="name"
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
                    placeholder="Min. 8 characters"
                    autoComplete="new-password"
                    fieldName="password"
                    fieldErrors={fieldErrors}
                  />
                </FormGroup>
                <FormGroup>
                  <FormLabel htmlFor="confirmPassword" required>
                    Confirm password
                  </FormLabel>
                  <FormInput
                    id="confirmPassword"
                    type="password"
                    value={values.confirmPassword}
                    onChange={e => updateField('confirmPassword', e.target.value)}
                    placeholder="Repeat your password"
                    autoComplete="new-password"
                    fieldName="confirmPassword"
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
                  Activate account
                </Button>
              </Form>
            )}
          </CardContent>
        </Card>
      </Stack>
    </CenteredPageLayout>
  );
}
