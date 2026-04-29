import { useTranslation } from 'react-i18next'
import { projectsApi } from '../../api/projects'
import { CreateProjectInput } from '../../api/types'
import { Modal } from '../ui/Modal'
import { Form, FormGroup, FormLabel, FormInput, FormTextarea } from '../ui/Form'
import { Button } from '../ui/Button'
import { useForm } from '../../hooks/useForm'
import styles from './CreateProjectModal.module.css'

interface CreateProjectModalProps {
  isOpen: boolean
  onClose: () => void
  onSuccess?: (projectId: string) => void
}

export function CreateProjectModal({
  isOpen,
  onClose,
  onSuccess,
}: CreateProjectModalProps) {
  const { t } = useTranslation('projects')
  const form = useForm({
    initialValues: { name: '', description: '' },
    onSubmit: async (values) => {
      const input: CreateProjectInput = {
        name: values.name.trim(),
        description: values.description.trim() || undefined,
      }
      await projectsApi.create(input)
    },
    onSuccess: () => {
      onClose()
      onSuccess?.('') // Note: We might want to get the ID from the API response
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
      title="Create Project"
      description="Add a new project to manage your services"
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
            Create Project
          </Button>
        </div>
      }
    >
      <Form onSubmit={form.handleSubmit} isLoading={form.isLoading}>
        <FormGroup>
          <FormLabel htmlFor="project-name" required>
            Project Name
          </FormLabel>
          <FormInput
            id="project-name"
            type="text"
            placeholder="e.g., my-app, api-service"
            value={form.values.name}
            fieldName="name"
            fieldErrors={form.fieldErrors}
            onChange={(e) => form.updateField('name', e.target.value)}
            disabled={form.isLoading}
            maxLength={64}
          />
        </FormGroup>

        <FormGroup>
          <FormLabel htmlFor="project-description">Description</FormLabel>
          <FormTextarea
            id="project-description"
            placeholder="Describe what this project does..."
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
