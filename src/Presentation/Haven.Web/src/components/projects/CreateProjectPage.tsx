import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Check } from 'lucide-react'
import { useSetBreadcrumbs } from '@/hooks/useSetBreadcrumbs'
import { projectsApi } from '../../api/projects'
import { CreateProjectInput } from '../../api/types'
import { Button } from '../ui/Button'
import { Card, CardHeader, CardTitle, CardContent, CardFooter } from '../ui/Card'
import { FormGroup, FormLabel, FormInput, FormTextarea } from '../ui/Form'
import { ErrorAlert } from '../ui/ErrorAlert'
import styles from './CreateProjectPage.module.css'

export function CreateProjectPage() {
  const { t } = useTranslation('projects')
  const { t: tCommon } = useTranslation('common')
  const navigate = useNavigate()

  useSetBreadcrumbs([
    { label: 'Projects', to: '/dashboard' },
    { label: 'Create' },
  ])

  // Form state
  const [name, setName] = useState('')
  const [alias, setAlias] = useState('')
  const [description, setDescription] = useState('')
  const [envVarsText, setEnvVarsText] = useState('')

  // UI state
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [status, setStatus] = useState<'idle' | 'creating' | 'success' | 'error'>('idle')
  const [createdProjectId, setCreatedProjectId] = useState<string | null>(null)

  const isFormValid = () => {
    return name.trim().length > 0
  }

  const handleSubmit = async () => {
    setError(null)

    if (!isFormValid()) {
      setError(t('createPage.fillRequiredFields', 'Please fill in all required fields'))
      return
    }

    const input: CreateProjectInput = {
      name: name.trim(),
      alias: alias.trim() || undefined,
      description: description.trim() || undefined,
    }

    setIsLoading(true)
    setStatus('creating')
    try {
      const projectId = await projectsApi.create(input)
      setCreatedProjectId(projectId)

      if (envVarsText.trim()) {
        await projectsApi.setEnvironmentVariables(projectId, envVarsText)
      }

      setStatus('success')
    } catch (err) {
      setError(err instanceof Error ? err.message : t('createPage.failedToCreate', 'Failed to create project'))
      setStatus('error')
    } finally {
      setIsLoading(false)
    }
  }

  const handleViewProject = () => {
    if (createdProjectId) {
      navigate(`/projects/${createdProjectId}`)
    }
  }

  const handleViewProjects = () => {
    navigate('/dashboard')
  }

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <h1>{t('createPage.title', 'Create Project')}</h1>
        <p>{t('createPage.description', 'Add a new project to organize your services')}</p>
      </div>

      {status !== 'idle' && (
        <div className={`${styles.statusBar} ${styles[`status${status.charAt(0).toUpperCase() + status.slice(1)}`]}`}>
          <div className={styles.statusContent}>
            <span className={styles.statusIndicator}>
              {status === 'creating' && <span className={styles.spinner} />}
              {status === 'success' && <Check size={16} />}
              {status === 'error' && <span>!</span>}
            </span>
            <span className={styles.statusText}>
              {status === 'creating' && t('createPage.creating', 'Creating project...')}
              {status === 'success' && t('createPage.createdSuccessfully', 'Project created successfully')}
              {status === 'error' && t('createPage.failedToCreate', 'Failed to create project')}
            </span>
          </div>
        </div>
      )}

      <div className={styles.content}>
        {error && <ErrorAlert message={error} variant="block" />}

        {status === 'success' ? (
          <Card className={styles.successCard}>
            <CardHeader>
              <CardTitle>{t('createPage.successTitle', 'Project Created')}</CardTitle>
              <p className={styles.cardDescription}>{t('createPage.successDescription', 'Your project is ready to use')}</p>
            </CardHeader>

            <div className={styles.successContent}>
              <div className={styles.successIcon}>
                <Check size={40} />
              </div>
              <p
                className={styles.successMessage}
                dangerouslySetInnerHTML={{
                  __html: t('createPage.successMessage', 'Project {{name}} has been created successfully').replace('{{name}}', `<strong>${name}</strong>`),
                }}
              />
            </div>

            <CardFooter>
              <Button variant="secondary" onClick={handleViewProjects}>
                {t('createPage.viewProjects', 'View All Projects')}
              </Button>
              <Button variant="primary" onClick={handleViewProject}>
                {t('createPage.viewProject', 'View Project')}
              </Button>
            </CardFooter>
          </Card>
        ) : (
          <>
            {/* Card 1: Project Details */}
            <Card>
              <CardHeader>
                <CardTitle>{t('createPage.projectDetails', 'Project Details')}</CardTitle>
                <p className={styles.cardDescription}>{t('createPage.projectDetailsDescription', 'Enter the basic information for your project')}</p>
              </CardHeader>

              <CardContent>
                <div className={styles.formSection}>
                <FormGroup>
                  <FormLabel htmlFor="projectName" required>
                    {t('createPage.projectName', 'Project Name')}
                  </FormLabel>
                  <FormInput
                    id="projectName"
                    type="text"
                    placeholder={t('createPage.projectNamePlaceholder', 'e.g., my-app, api-service')}
                    value={name}
                    onChange={(e) => setName(e.target.value)}
                    disabled={isLoading}
                    maxLength={64}
                    style={{backgroundColor: "var(--color-surface-2)"}}
                  />
                </FormGroup>

                <FormGroup>
                  <FormLabel htmlFor="projectAlias">
                    Alias <span style={{ fontSize: 'var(--text-xs)', color: 'var(--color-text-secondary)', fontWeight: 'normal' }}>— used in Docker names, e.g. <code>haven-myapp-...</code> (2–8 chars)</span>
                  </FormLabel>
                  <FormInput
                    id="projectAlias"
                    type="text"
                    placeholder="e.g., myapp, backend"
                    value={alias}
                    onChange={(e) => setAlias(e.target.value.toLowerCase())}
                    disabled={isLoading}
                    maxLength={8}
                    style={{backgroundColor: "var(--color-surface-2)"}}
                  />
                </FormGroup>

                <FormGroup>
                  <FormLabel htmlFor="projectDescription">
                    {tCommon('labels.description', 'Description')}
                  </FormLabel>
                  <FormTextarea
                    id="projectDescription"
                    placeholder={t('createPage.descriptionPlaceholder', 'Describe what this project does...')}
                    value={description}
                    onChange={(e) => setDescription(e.target.value)}
                    disabled={isLoading}
                    maxLength={250}
                    rows={4}
                    style={{backgroundColor: "var(--color-surface-2)"}}
                  />
                  <span className={styles.charCount}>
                    {description.length}/250
                  </span>
                </FormGroup>
              </div>
              </CardContent>
            </Card>

            {/* Card 2: Environment Variables */}
            <Card>
              <CardHeader>
                <CardTitle>{t('variablesPage.title', 'Project Variables')}</CardTitle>
                <p className={styles.cardDescription}>{t('variablesPage.description', 'Set project-scoped environment variables available to all services')}</p>
              </CardHeader>

              <CardContent>
                <div className={styles.formSection}>
                <FormGroup>
                  <div className={styles.labelWithHelp}>
                    <FormLabel htmlFor="projectVars">
                      {t('variablesPage.variables', 'Variables')}
                    </FormLabel>
                    <span className={styles.helpText}>{t('variablesPage.help', 'One variable per line in KEY=VALUE format')}</span>
                  </div>
                  <FormTextarea
                    id="projectVars"
                    placeholder={t('variablesPage.placeholder', 'DATABASE_URL=postgres://localhost\nAPI_KEY=your-secret-key')}
                    value={envVarsText}
                    onChange={(e) => setEnvVarsText(e.target.value)}
                    disabled={isLoading}
                    style={{backgroundColor: "var(--color-surface-2)"}}
                    rows={8}
                  />
                </FormGroup>
              </div>
              </CardContent>
            </Card>

            {/* Submit Section */}
            <div className={styles.submitSection}>
              <Button variant="secondary" onClick={() => navigate(-1)} disabled={isLoading}>
                {t('createPage.cancel', 'Cancel')}
              </Button>
              <Button
                variant="primary"
                onClick={handleSubmit}
                isLoading={isLoading}
                disabled={!isFormValid()}
              >
                {t('createPage.createButton', 'Create Project')}
              </Button>
            </div>
          </>
        )}
      </div>
    </div>
  )
}
