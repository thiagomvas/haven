import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Save } from 'lucide-react'
import { servicesApi } from '../../api/services'
import { Button } from '../ui/Button'
import styles from './ServiceVariablesEditor.module.css'

interface ServiceVariablesEditorProps {
  projectId: string
  environmentId: string
  serviceId: string
}

export function ServiceVariablesEditor({
  projectId,
  environmentId,
  serviceId,
}: ServiceVariablesEditorProps) {
  const { t } = useTranslation('services')
  const [envContent, setEnvContent] = useState('')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [isSaving, setIsSaving] = useState(false)
  const [isDirty, setIsDirty] = useState(false)
  const [savedMessage, setSavedMessage] = useState(false)

  useEffect(() => {
    loadEnvironmentVariables()
  }, [projectId, environmentId, serviceId])

  const loadEnvironmentVariables = async () => {
    try {
      setLoading(true)
      setError(null)
      const content = await servicesApi.getEnvironmentVariables(
        projectId,
        environmentId,
        serviceId,
      )
      setEnvContent(content || '')
      setIsDirty(false)
    } catch (err) {
      setError(err instanceof Error ? err.message : t('error'))
    } finally {
      setLoading(false)
    }
  }

  const handleSave = async () => {
    try {
      setIsSaving(true)
      await servicesApi.setEnvironmentVariables(
        projectId,
        environmentId,
        serviceId,
        envContent,
      )
      setIsDirty(false)
      setSavedMessage(true)
      setTimeout(() => setSavedMessage(false), 3000)
    } catch (err) {
      setError(err instanceof Error ? err.message : t('error'))
    } finally {
      setIsSaving(false)
    }
  }

  if (loading) {
    return (
      <div className={styles.container}>
        <p>{t('loading')}</p>
      </div>
    )
  }

  return (
    <div className={styles.container}>
      {error && <div className={styles.error}>{error}</div>}
      {savedMessage && <div className={styles.success}>{t('variablesSaved')}</div>}

      <textarea
        className={styles.editor}
        value={envContent}
        onChange={(e) => {
          setEnvContent(e.target.value)
          setIsDirty(true)
        }}
        placeholder={'.env file format\nKEY=value\nDATABASE_URL=postgresql://...'}
        spellCheck="false"
      />

      <div className={styles.footer}>
        <Button
          variant="primary"
          icon={<Save size={18} />}
          onClick={handleSave}
          disabled={!isDirty || isSaving}
          isLoading={isSaving}
        >
          {t('save') || 'Save'}
        </Button>
      </div>
    </div>
  )
}
