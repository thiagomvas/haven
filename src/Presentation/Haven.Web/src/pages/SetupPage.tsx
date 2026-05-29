import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { CenteredPageLayout } from '@/components/layout/CenteredPageLayout'
import { Stack } from '@/components/layout'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/Card'
import { Form, FormGroup, FormInput, FormLabel } from '@/components/ui/Form'
import { Button } from '@/components/ui/Button'
import { ErrorAlert } from '@/components/ui/ErrorAlert'
import { useForm } from '@/hooks/useForm'
import { setupApi } from '@/api/setup'

export function SetupPage() {
  const navigate = useNavigate()
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
      await setupApi.register(values)
    },
    onSuccess: () => navigate('/dashboard', { replace: true }),
  })

  return (
    <CenteredPageLayout>
      <Stack gap="6">
        <div style={{ textAlign: 'center' }}>
          <h1 style={{ margin: 0, fontFamily: "'Poppins', sans-serif", color: 'var(--color-primary)' }}>
            Haven
          </h1>
          <p style={{ margin: '4px 0 0', color: 'var(--color-text-muted)', fontSize: 'var(--font-size-sm)' }}>
            Create your admin account to get started
          </p>
        </div>

        <Card>
          <CardHeader>
            <CardTitle>First-time setup</CardTitle>
          </CardHeader>
          <CardContent>
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
                <FormLabel htmlFor="confirmPassword" required>Confirm password</FormLabel>
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
                Create account
              </Button>
            </Form>
          </CardContent>
        </Card>
      </Stack>
    </CenteredPageLayout>
  )
}
