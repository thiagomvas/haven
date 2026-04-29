import { environmentsApi } from '../../api/environments'
import { CreateEnvironmentInput } from '../../api/types'
import { Modal } from '../ui/Modal'
import { Form, FormGroup, FormLabel, FormInput, FormTextarea } from '../ui/Form'
import { Button } from '../ui/Button'
import { useForm } from '../../hooks/useForm'
import styles from './CreateEnvironmentModal.module.css'

interface CreateEnvironmentModalProps {
  projectId: string
  isOpen: boolean
  onClose: () => void
  onSuccess?: () => void
}

export function CreateEnvironmentModal({
  projectId,
  isOpen,
  onClose,
  onSuccess,
}: CreateEnvironmentModalProps) {
  const form = useForm({
    initialValues: { name: '', description: '' },
    onSubmit: async (values) => {
      const input: CreateEnvironmentInput = {
        name: values.name.trim(),
        description: values.description.trim() || undefined,
      }
      await environmentsApi.create(projectId, input)
    },
    onSuccess: () => {
      onClose()
      onSuccess?.()
    },
  })

  const handleClose = () => {
    form.reset()
    onClose()
  }

  return (
    <Modal
      isOpen={isOpen}
      onClose={handleClose}
      title="Create Environment"
      description="Add a new deployment environment for your project"
      size="md"
      error={form.submitError}
      footer={
        <div className={styles.footer}>
          <Button variant="ghost" onClick={handleClose} disabled={form.isLoading}>
            Cancel
          </Button>
          <Button
            variant="primary"
            onClick={() => {
              const formEl = document.querySelector(
                'form',
              ) as HTMLFormElement
              formEl?.dispatchEvent(
                new Event('submit', { bubbles: true, cancelable: true }),
              )
            }}
            isLoading={form.isLoading}
          >
            Create Environment
          </Button>
        </div>
      }
    >
      <Form onSubmit={form.handleSubmit} isLoading={form.isLoading}>
        <FormGroup>
          <FormLabel htmlFor="env-name" required>
            Environment Name
          </FormLabel>
          <FormInput
            id="env-name"
            type="text"
            placeholder="e.g., development, staging, production"
            value={form.values.name}
            fieldName="name"
            fieldErrors={form.fieldErrors}
            onChange={(e) => form.updateField('name', e.target.value)}
            disabled={form.isLoading}
            maxLength={64}
          />
        </FormGroup>

        <FormGroup>
          <FormLabel htmlFor="env-description">Description</FormLabel>
          <FormTextarea
            id="env-description"
            placeholder="Describe the purpose of this environment..."
            value={form.values.description}
            fieldName="description"
            fieldErrors={form.fieldErrors}
            onChange={(e) => form.updateField('description', e.target.value)}
            disabled={form.isLoading}
            maxLength={250}
          />
          <span className={styles.charCount}>
            {form.values.description.length}/250
          </span>
        </FormGroup>
      </Form>
    </Modal>
  )
}
