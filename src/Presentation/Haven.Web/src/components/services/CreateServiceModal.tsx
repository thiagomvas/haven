import { useEffect } from 'react'
import { servicesApi } from '../../api/services'
import { CreateServiceInput, ServiceDto, DockerConfig, ServiceType, ExposureMode } from '../../api/types'
import { Modal } from '../ui/Modal'
import { Form, FormGroup, FormLabel, FormInput, FormTextarea, FormSelect } from '../ui/Form'
import { Button } from '../ui/Button'
import { useForm } from '../../hooks/useForm'
import styles from './CreateServiceModal.module.css'

interface CreateServiceModalProps {
  projectId: string
  environmentId: string
  isOpen: boolean
  onClose: () => void
  onSuccess?: (serviceId: string) => void
  service?: ServiceDto
}

const SERVICE_TYPES: ServiceType[] = ['DockerImage', 'Compose', 'Process']
const EXPOSURE_MODES: ExposureMode[] = ['None', 'Internal', 'External']
const RESTART_POLICIES = ['No', 'Always', 'UnlessStopped', 'OnFailure']

export function CreateServiceModal({
  projectId,
  environmentId,
  isOpen,
  onClose,
  onSuccess,
  service,
}: CreateServiceModalProps) {
  const isEditMode = !!service

  const form = useForm({
    initialValues: {
      name: service?.name || '',
      type: (service?.type || 'DockerImage') as ServiceType,
      exposureMode: (service?.sourceConfig && 'exposureMode' in service ? service.exposureMode : 'None') as ExposureMode,
      image: (service?.sourceConfig && 'image' in service?.sourceConfig ? (service.sourceConfig as any).image : '') || '',
      ports: (service?.sourceConfig && 'ports' in service?.sourceConfig ? (service.sourceConfig as any).ports?.join('\n') : '') || '',
      volumes: (service?.sourceConfig && 'volumes' in service?.sourceConfig ? (service.sourceConfig as any).volumes?.join('\n') : '') || '',
      environmentVariables: (service?.sourceConfig && 'environmentVariables' in service?.sourceConfig ? (service.sourceConfig as any).environmentVariables?.join('\n') : '') || '',
      restartPolicy: (service?.sourceConfig && 'restartPolicy' in service?.sourceConfig ? (service.sourceConfig as any).restartPolicy : 'No') || 'No',
    },
    onSubmit: async (values) => {
      const input: CreateServiceInput = {
        name: values.name.trim(),
        type: values.type,
        exposureMode: values.exposureMode,
        dockerConfig: values.type === 'DockerImage' ? {
          image: values.image.trim(),
          ports: values.ports.split('\n').filter(p => p.trim()),
          volumes: values.volumes.split('\n').filter(v => v.trim()),
          environmentVariables: values.environmentVariables.split('\n').filter(e => e.trim()),
          restartPolicy: values.restartPolicy as any,
        } : undefined,
      }
      await servicesApi.create(projectId, environmentId, input)
    },
    onSuccess: () => {
      onClose()
      onSuccess?.('')
    },
  })

  useEffect(() => {
    if (isOpen) {
      form.reset()
    }
  }, [isOpen, environmentId])

  const handleClose = () => {
    form.reset()
    onClose()
  }

  const title = 'Create Service'
  const description = 'Add a new containerized service to this environment'

  return (
    <Modal
      isOpen={isOpen}
      onClose={handleClose}
      title={title}
      description={description}
      size="lg"
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
            Create Service
          </Button>
        </div>
      }
    >
      <Form onSubmit={form.handleSubmit} isLoading={form.isLoading}>
        <FormGroup>
          <FormLabel htmlFor="service-name" required>
            Service Name
          </FormLabel>
          <FormInput
            id="service-name"
            type="text"
            placeholder="e.g., my-api, web-server"
            value={form.values.name}
            fieldName="name"
            fieldErrors={form.fieldErrors}
            onChange={(e) => form.updateField('name', e.target.value)}
            disabled={form.isLoading}
            maxLength={64}
          />
        </FormGroup>

        <FormGroup>
          <FormLabel htmlFor="service-type" required>
            Service Type
          </FormLabel>
          <FormSelect
            id="service-type"
            value={form.values.type}
            fieldName="type"
            fieldErrors={form.fieldErrors}
            onChange={(e) => form.updateField('type', e.target.value)}
            disabled={form.isLoading}
          >
            <option value="">Select a service type</option>
            {SERVICE_TYPES.map(type => (
              <option key={type} value={type}>{type}</option>
            ))}
          </FormSelect>
        </FormGroup>

        <FormGroup>
          <FormLabel htmlFor="exposure-mode" required>
            Exposure Mode
          </FormLabel>
          <FormSelect
            id="exposure-mode"
            value={form.values.exposureMode}
            fieldName="exposureMode"
            fieldErrors={form.fieldErrors}
            onChange={(e) => form.updateField('exposureMode', e.target.value)}
            disabled={form.isLoading}
          >
            <option value="">Select exposure mode</option>
            {EXPOSURE_MODES.map(mode => (
              <option key={mode} value={mode}>{mode}</option>
            ))}
          </FormSelect>
        </FormGroup>

        {form.values.type === 'DockerImage' && (
          <>
            <FormGroup>
              <FormLabel htmlFor="docker-image">
                Docker Image
              </FormLabel>
              <FormInput
                id="docker-image"
                type="text"
                placeholder="e.g., nginx:latest, ubuntu:22.04"
                value={form.values.image}
                fieldName="image"
                fieldErrors={form.fieldErrors}
                onChange={(e) => form.updateField('image', e.target.value)}
                disabled={form.isLoading}
              />
            </FormGroup>

            <FormGroup>
              <FormLabel htmlFor="docker-ports">
                Ports
              </FormLabel>
              <FormTextarea
                id="docker-ports"
                placeholder="e.g., 8080:80&#10;3000:3000"
                value={form.values.ports}
                fieldName="ports"
                fieldErrors={form.fieldErrors}
                onChange={(e) => form.updateField('ports', e.target.value)}
                disabled={form.isLoading}
              />
              <span className={styles.hint}>One port mapping per line</span>
            </FormGroup>

            <FormGroup>
              <FormLabel htmlFor="docker-volumes">
                Volumes
              </FormLabel>
              <FormTextarea
                id="docker-volumes"
                placeholder="e.g., /data:/data&#10;./config:/etc/config"
                value={form.values.volumes}
                fieldName="volumes"
                fieldErrors={form.fieldErrors}
                onChange={(e) => form.updateField('volumes', e.target.value)}
                disabled={form.isLoading}
              />
              <span className={styles.hint}>One volume mount per line</span>
            </FormGroup>

            <FormGroup>
              <FormLabel htmlFor="docker-env">
                Environment Variables
              </FormLabel>
              <FormTextarea
                id="docker-env"
                placeholder="e.g., DEBUG=true&#10;NODE_ENV=production"
                value={form.values.environmentVariables}
                fieldName="environmentVariables"
                fieldErrors={form.fieldErrors}
                onChange={(e) => form.updateField('environmentVariables', e.target.value)}
                disabled={form.isLoading}
              />
              <span className={styles.hint}>One variable per line (KEY=VALUE format)</span>
            </FormGroup>

            <FormGroup>
              <FormLabel htmlFor="restart-policy">
                Restart Policy
              </FormLabel>
              <FormSelect
                id="restart-policy"
                value={form.values.restartPolicy}
                fieldName="restartPolicy"
                fieldErrors={form.fieldErrors}
                onChange={(e) => form.updateField('restartPolicy', e.target.value)}
                disabled={form.isLoading}
              >
                {RESTART_POLICIES.map(policy => (
                  <option key={policy} value={policy}>{policy}</option>
                ))}
              </FormSelect>
            </FormGroup>
          </>
        )}
      </Form>
    </Modal>
  )
}
