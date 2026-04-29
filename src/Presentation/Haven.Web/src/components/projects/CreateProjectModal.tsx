import { useTranslation } from 'react-i18next'
import { projectsApi } from '../../api/projects'
import { CreateProjectInput, ProjectDto, UpdateProjectInput } from '../../api/types'
import { Modal } from '../ui/Modal'
import { Form, FormGroup, FormLabel, FormInput, FormTextarea } from '../ui/Form'
import { Button } from '../ui/Button'
import { useForm } from '../../hooks/useForm'
import styles from './CreateProjectModal.module.css'

interface CreateProjectModalProps {
  isOpen: boolean
  onClose: () => void
  onSuccess?: (projectId: string) => void
  project?: ProjectDto
}

export function CreateProjectModal({
  isOpen,
  onClose,
  onSuccess,
  project,
}: CreateProjectModalProps) {
  const { t } = useTranslation('projects')
  const isEditMode = !!project

  const form = useForm({
    initialValues: {
      name: project?.name || '',
      description: project?.description || ''
    },
    onSubmit: async (values) => {
      if (isEditMode && project) {
        const input: UpdateProjectInput = {
          name: values.name.trim() || undefined,
          description: values.description.trim() || undefined,
        }
        await projectsApi.update(project.id, input)
      } else {
        const input: CreateProjectInput = {
          name: values.name.trim(),
          description: values.description.trim() || undefined,
        }
        await projectsApi.create(input)
      }
    },
    onSuccess: () => {
      onClose()
      onSuccess?.(project?.id || '')
    },
  })

  const handleClose = () => {
    form.reset()
    onClose()
  }

  const title = isEditMode ? 'Edit Project' : 'Create Project'
  const description = isEditMode
    ? 'Update the project details'
    : 'Add a new project to manage your services'
  const submitLabel = isEditMode ? 'Save Changes' : 'Create Project'

  return (
    <Modal
      isOpen={isOpen}
      onClose={handleClose}
      title={title}
      description={description}
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
            {submitLabel}
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
