import { useNavigate } from 'react-router-dom';
import { CenteredPageLayout } from '@/components/layout/CenteredPageLayout';
import { Stack } from '@/components/layout';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/Card';
import { Form, FormGroup, FormInput, FormLabel } from '@/components/ui/Form';
import { Button } from '@/components/ui/Button';
import { ErrorAlert } from '@/components/ui/ErrorAlert';
import { Label } from '@/components/ui/Label';
import { useForm } from '@/hooks/useForm';
import { authApi } from '@/api/auth';

export function SetPasswordPage() {
  const navigate = useNavigate();

  const { values, fieldErrors, submitError, isLoading, handleSubmit, updateField } = useForm({
    initialValues: { newPassword: '', confirmPassword: '' },
    onSubmit: async values => {
      await authApi.setPassword(values);
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
            Set a password to continue
          </p>
        </div>

        <Card>
          <CardHeader>
            <CardTitle>Set your password</CardTitle>
            <Label variant="muted">
              Your account requires a password before you can access Haven.
            </Label>
          </CardHeader>
          <CardContent>
            <Form onSubmit={handleSubmit} isLoading={isLoading}>
              <FormGroup>
                <FormLabel htmlFor="newPassword" required>
                  New password
                </FormLabel>
                <FormInput
                  id="newPassword"
                  type="password"
                  value={values.newPassword}
                  onChange={e => updateField('newPassword', e.target.value)}
                  placeholder="Min. 8 characters"
                  autoComplete="new-password"
                  fieldName="newPassword"
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
                Set password
              </Button>
            </Form>
          </CardContent>
        </Card>
      </Stack>
    </CenteredPageLayout>
  );
}
