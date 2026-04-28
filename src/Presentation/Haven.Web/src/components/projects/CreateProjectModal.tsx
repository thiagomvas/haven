import { FormEvent, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { projectsApi } from '../../api/projects'
import { CreateProjectInput } from '../../api/types'
import { Modal } from '../ui/Modal'
import { Form, FormGroup, FormLabel, FormInput, FormTextarea } from '../ui/Form'
import { Button } from '../ui/Button'
import styles from './CreateProjectModal.module.css'

interface CreateProjectModalProps {
  isOpen: boolean
  onClose: () => void
  onSuccess?: (projectId: string) => void
}

interface FormState {
  name: string
  description: string
}

interface FormErrors {
  name?: string
  description?: string
  submit?: string
}

export function CreateProjectModal({
  isOpen,
  onClose,
  onSuccess,
}: CreateProjectModalProps) {
  const { t } = useTranslation('projects')
  const [formState, setFormState] = useState<FormState>({
    name: '',
    description: '',
  })
  const [errors, setErrors] = useState<FormErrors>({})
  const [isLoading, setIsLoading] = useState(false)

  const validateForm = (): boolean => {
    const newErrors: FormErrors = {}

    if (!formState.name.trim()) {
      newErrors.name = 'Project name is required'
    } else if (formState.name.length > 64) {
      newErrors.name = 'Project name must be 64 characters or less'
    }

    if (formState.description.length > 250) {
      newErrors.description = 'Description must be 250 characters or less'
    }

    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault()

    if (!validateForm()) return

    try {
      setIsLoading(true)
      setErrors({})

      const input: CreateProjectInput = {
        name: formState.name.trim(),
        description: formState.description.trim() || undefined,
      }

      const projectId = await projectsApi.create(input)

      // Reset form
      setFormState({ name: '', description: '' })
      onClose()
      onSuccess?.(projectId)
    } catch (err) {
      setErrors({
        submit: err instanceof Error ? err.message : 'Failed to create project',
      })
    } finally {
      setIsLoading(false)
    }
  }

  const handleClose = () => {
    setFormState({ name: '', description: '' })
    setErrors({})
    onClose()
  }

  return (
    <Modal
      isOpen={isOpen}
      onClose={handleClose}
      title="Create Project"
      description="Add a new project to manage your services"
      size="md"
      footer={
        <div className={styles.footer}>
          <Button variant="ghost" onClick={handleClose} disabled={isLoading}>
            Cancel
          </Button>
          <Button
            variant="primary"
            onClick={() => {
              const form = document.querySelector(
                'form',
              ) as HTMLFormElement
              form?.dispatchEvent(
                new Event('submit', { bubbles: true, cancelable: true }),
              )
            }}
            isLoading={isLoading}
          >
            Create Project
          </Button>
        </div>
      }
    >
      <Form onSubmit={handleSubmit} isLoading={isLoading}>
        {errors.submit && (
          <div className={styles.submitError}>{errors.submit}</div>
        )}

        <FormGroup>
          <FormLabel htmlFor="project-name" required>
            Project Name
          </FormLabel>
          <FormInput
            id="project-name"
            type="text"
            placeholder="e.g., my-app, api-service"
            value={formState.name}
            onChange={(e) => {
              setFormState((prev) => ({ ...prev, name: e.target.value }))
              if (errors.name) {
                setErrors((prev) => ({ ...prev, name: undefined }))
              }
            }}
            error={errors.name}
            disabled={isLoading}
            maxLength={64}
          />
        </FormGroup>

        <FormGroup>
          <FormLabel htmlFor="project-description">Description</FormLabel>
          <FormTextarea
            id="project-description"
            placeholder="Describe what this project does..."
            value={formState.description}
            onChange={(e) => {
              setFormState((prev) => ({
                ...prev,
                description: e.target.value,
              }))
              if (errors.description) {
                setErrors((prev) => ({ ...prev, description: undefined }))
              }
            }}
            error={errors.description}
            disabled={isLoading}
            maxLength={250}
          />
          <span className={styles.charCount}>
            {formState.description.length}/250
          </span>
        </FormGroup>
      </Form>
    </Modal>
  )
}
