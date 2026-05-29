import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Check } from 'lucide-react'
import { useSetBreadcrumbs } from '@/hooks/useSetBreadcrumbs'
import { projectsApi } from '../../api/projects'
import { CreateProjectInput } from '../../api/types'
import { Button } from '../ui/Button'
import styles from './CreateProjectPage.module.css'

export function CreateProjectPage() {
  const { t } = useTranslation('projects')
  const navigate = useNavigate()

  useSetBreadcrumbs([
    { label: 'Projects', to: '/dashboard' },
    { label: 'Create' },
  ])

  // Form state
  const [name, setName] = useState('')
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
        {error && <div className={styles.error}>{error}</div>}

        {status === 'success' ? (
          <div className={`${styles.card} ${styles.successCard}`}>
            <div className={styles.cardHeader}>
              <h2 className={styles.cardTitle}>{t('createPage.successTitle', 'Project Created')}</h2>
              <p className={styles.cardDescription}>{t('createPage.successDescription', 'Your project is ready to use')}</p>
            </div>

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

            <div className={styles.cardFooter}>
              <Button variant="secondary" onClick={handleViewProjects}>
                {t('createPage.viewProjects', 'View All Projects')}
              </Button>
              <Button variant="primary" onClick={handleViewProject}>
                {t('createPage.viewProject', 'View Project')}
              </Button>
            </div>
          </div>
        ) : (
          <>
            {/* Card 1: Project Details */}
            <div className={styles.card}>
              <div className={styles.cardHeader}>
                <h2 className={styles.cardTitle}>{t('createPage.projectDetails', 'Project Details')}</h2>
                <p className={styles.cardDescription}>{t('createPage.projectDetailsDescription', 'Enter the basic information for your project')}</p>
              </div>

              <div className={styles.formSection}>
                <div className={styles.formGroup}>
                  <label className={styles.label}>
                    {t('createPage.projectName', 'Project Name')} <span className={styles.required}>*</span>
                  </label>
                  <input
                    type="text"
                    className={styles.input}
                    placeholder={t('createPage.projectNamePlaceholder', 'e.g., my-app, api-service')}
                    value={name}
                    onChange={(e) => setName(e.target.value)}
                    disabled={isLoading}
                    maxLength={64}
                  />
                </div>

                <div className={styles.formGroup}>
                  <label className={styles.label}>{t('createPage.description', 'Description')}</label>
                  <textarea
                    className={styles.textarea}
                    placeholder={t('createPage.descriptionPlaceholder', 'Describe what this project does...')}
                    value={description}
                    onChange={(e) => setDescription(e.target.value)}
                    disabled={isLoading}
                    maxLength={250}
                    rows={4}
                  />
                  <span className={styles.charCount}>
                    {description.length}/250
                  </span>
                </div>
              </div>
            </div>

            {/* Card 2: Environment Variables */}
            <div className={styles.card}>
              <div className={styles.cardHeader}>
                <h2 className={styles.cardTitle}>{t('variablesPage.title', 'Project Variables')}</h2>
                <p className={styles.cardDescription}>{t('variablesPage.description', 'Set project-scoped environment variables available to all services')}</p>
              </div>

              <div className={styles.formSection}>
                <div className={styles.formGroup}>
                  <div className={styles.labelWithHelp}>
                    <label className={styles.label}>{t('variablesPage.variables', 'Variables')}</label>
                    <span className={styles.helpText}>{t('variablesPage.help', 'One variable per line in KEY=VALUE format')}</span>
                  </div>
                  <textarea
                    className={styles.textarea}
                    placeholder={t('variablesPage.placeholder', 'DATABASE_URL=postgres://localhost\nAPI_KEY=your-secret-key')}
                    value={envVarsText}
                    onChange={(e) => setEnvVarsText(e.target.value)}
                    disabled={isLoading}
                    rows={8}
                  />
                </div>
              </div>
            </div>

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
