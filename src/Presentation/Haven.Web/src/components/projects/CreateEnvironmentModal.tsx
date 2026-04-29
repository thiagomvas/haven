import { FormEvent, useState } from 'react'
import { environmentsApi } from '../../api/environments'
import { CreateEnvironmentInput } from '../../api/types'
import { Modal } from '../ui/Modal'
import { Form, FormGroup, FormLabel, FormInput, FormTextarea } from '../ui/Form'
import { Button } from '../ui/Button'
import styles from './CreateEnvironmentModal.module.css'

interface CreateEnvironmentModalProps {
  projectId: string
  isOpen: boolean
  onClose: () => void
  onSuccess?: () => void
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

export function CreateEnvironmentModal({
  projectId,
  isOpen,
  onClose,
  onSuccess,
}: CreateEnvironmentModalProps) {
  const [formState, setFormState] = useState<FormState>({
    name: '',
    description: '',
  })
  const [errors, setErrors] = useState<FormErrors>({})
  const [isLoading, setIsLoading] = useState(false)

  const validateForm = (): boolean => {
    const newErrors: FormErrors = {}

    if (!formState.name.trim()) {
      newErrors.name = 'Environment name is required'
    } else if (formState.name.length > 64) {
      newErrors.name = 'Environment name must be 64 characters or less'
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

      const input: CreateEnvironmentInput = {
        name: formState.name.trim(),
        description: formState.description.trim() || undefined,
      }

      await environmentsApi.create(projectId, input)

      // Reset form
      setFormState({ name: '', description: '' })
      onClose()
      onSuccess?.()
    } catch (err) {
      setErrors({
        submit: err instanceof Error ? err.message : 'Failed to create environment',
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
      title="Create Environment"
      description="Add a new deployment environment for your project"
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
            Create Environment
          </Button>
        </div>
      }
    >
      <Form onSubmit={handleSubmit} isLoading={isLoading}>
        {errors.submit && (
          <div className={styles.submitError}>{errors.submit}</div>
        )}

        <FormGroup>
          <FormLabel htmlFor="env-name" required>
            Environment Name
          </FormLabel>
          <FormInput
            id="env-name"
            type="text"
            placeholder="e.g., development, staging, production"
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
          <FormLabel htmlFor="env-description">Description</FormLabel>
          <FormTextarea
            id="env-description"
            placeholder="Describe the purpose of this environment..."
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
