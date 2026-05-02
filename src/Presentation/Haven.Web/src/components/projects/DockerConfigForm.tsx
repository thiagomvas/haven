import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { X, Plus } from 'lucide-react'
import { DockerConfig } from '../../api/types'
import { Button } from '../ui/Button'
import styles from './DockerConfigForm.module.css'

interface DockerConfigFormProps {
  config: DockerConfig | undefined
  onSave: (config: DockerConfig) => Promise<void>
  isLoading?: boolean
}

export function DockerConfigForm({
  config,
  onSave,
  isLoading = false,
}: DockerConfigFormProps) {
  const { t } = useTranslation('services')
  const [formData, setFormData] = useState<DockerConfig>(
    config || {
      image: '',
      ports: [],
      volumes: [],
      environmentVariables: [],
      restartPolicy: 'UnlessStopped',
    },
  )
  const [errors, setErrors] = useState<Record<string, string>>({})

  const handleImageChange = (value: string) => {
    setFormData((prev) => ({ ...prev, image: value }))
    if (value.trim()) setErrors((prev) => ({ ...prev, image: '' }))
  }

  const handleAddPort = () => {
    setFormData((prev) => ({
      ...prev,
      ports: [...prev.ports, ''],
    }))
  }

  const handleRemovePort = (index: number) => {
    setFormData((prev) => ({
      ...prev,
      ports: prev.ports.filter((_, i) => i !== index),
    }))
  }

  const handlePortChange = (index: number, value: string) => {
    setFormData((prev) => {
      const newPorts = [...prev.ports]
      newPorts[index] = value
      return { ...prev, ports: newPorts }
    })
  }

  const handleAddVolume = () => {
    setFormData((prev) => ({
      ...prev,
      volumes: [...prev.volumes, ''],
    }))
  }

  const handleRemoveVolume = (index: number) => {
    setFormData((prev) => ({
      ...prev,
      volumes: prev.volumes.filter((_, i) => i !== index),
    }))
  }

  const handleVolumeChange = (index: number, value: string) => {
    setFormData((prev) => {
      const newVolumes = [...prev.volumes]
      newVolumes[index] = value
      return { ...prev, volumes: newVolumes }
    })
  }

  const handleAddEnvVar = () => {
    setFormData((prev) => ({
      ...prev,
      environmentVariables: [...prev.environmentVariables, ''],
    }))
  }

  const handleRemoveEnvVar = (index: number) => {
    setFormData((prev) => ({
      ...prev,
      environmentVariables: prev.environmentVariables.filter(
        (_, i) => i !== index,
      ),
    }))
  }

  const handleEnvVarChange = (index: number, value: string) => {
    setFormData((prev) => {
      const newEnvVars = [...prev.environmentVariables]
      newEnvVars[index] = value
      return { ...prev, environmentVariables: newEnvVars }
    })
  }

  const handleRestartPolicyChange = (value: string) => {
    setFormData((prev) => ({
      ...prev,
      restartPolicy: value as 'No' | 'Always' | 'UnlessStopped' | 'OnFailure',
    }))
  }

  const validate = (): boolean => {
    const newErrors: Record<string, string> = {}

    if (!formData.image.trim()) {
      newErrors.image = 'Image is required'
    }

    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  const handleSubmit = async () => {
    if (!validate()) return

    try {
      await onSave(formData)
    } catch (err) {
      console.error('Failed to save configuration', err)
    }
  }

  return (
    <div className={styles.form}>
      <div className={styles.section}>
        <label className={styles.label}>
          <span className={styles.labelText}>Docker Image *</span>
          <input
            type="text"
            className={`${styles.input} ${errors.image ? styles.inputError : ''}`}
            value={formData.image}
            onChange={(e) => handleImageChange(e.target.value)}
            placeholder="e.g., nginx:latest"
            disabled={isLoading}
          />
          {errors.image && (
            <span className={styles.error}>{errors.image}</span>
          )}
        </label>
      </div>

      <div className={styles.section}>
        <div className={styles.sectionHeader}>
          <h3 className={styles.sectionTitle}>Ports</h3>
          <Button
            variant="secondary"
            size="sm"
            icon={<Plus size={16} />}
            onClick={handleAddPort}
            disabled={isLoading}
          >
            Add Port
          </Button>
        </div>
        {formData.ports.length === 0 ? (
          <p className={styles.emptyText}>No ports configured</p>
        ) : (
          <div className={styles.itemList}>
            {formData.ports.map((port, index) => (
              <div key={index} className={styles.item}>
                <input
                  type="text"
                  className={styles.input}
                  value={port}
                  onChange={(e) => handlePortChange(index, e.target.value)}
                  placeholder="e.g., 8080:80"
                  disabled={isLoading}
                />
                <button
                  className={styles.removeButton}
                  onClick={() => handleRemovePort(index)}
                  disabled={isLoading}
                  title="Remove"
                >
                  <X size={16} />
                </button>
              </div>
            ))}
          </div>
        )}
      </div>

      <div className={styles.section}>
        <div className={styles.sectionHeader}>
          <h3 className={styles.sectionTitle}>Volumes</h3>
          <Button
            variant="secondary"
            size="sm"
            icon={<Plus size={16} />}
            onClick={handleAddVolume}
            disabled={isLoading}
          >
            Add Volume
          </Button>
        </div>
        {formData.volumes.length === 0 ? (
          <p className={styles.emptyText}>No volumes configured</p>
        ) : (
          <div className={styles.itemList}>
            {formData.volumes.map((volume, index) => (
              <div key={index} className={styles.item}>
                <input
                  type="text"
                  className={styles.input}
                  value={volume}
                  onChange={(e) => handleVolumeChange(index, e.target.value)}
                  placeholder="e.g., /data:/data"
                  disabled={isLoading}
                />
                <button
                  className={styles.removeButton}
                  onClick={() => handleRemoveVolume(index)}
                  disabled={isLoading}
                  title="Remove"
                >
                  <X size={16} />
                </button>
              </div>
            ))}
          </div>
        )}
      </div>

      <div className={styles.section}>
        <div className={styles.sectionHeader}>
          <h3 className={styles.sectionTitle}>Environment Variables</h3>
          <Button
            variant="secondary"
            size="sm"
            icon={<Plus size={16} />}
            onClick={handleAddEnvVar}
            disabled={isLoading}
          >
            Add Variable
          </Button>
        </div>
        {formData.environmentVariables.length === 0 ? (
          <p className={styles.emptyText}>No environment variables configured</p>
        ) : (
          <div className={styles.itemList}>
            {formData.environmentVariables.map((envVar, index) => (
              <div key={index} className={styles.item}>
                <input
                  type="text"
                  className={styles.input}
                  value={envVar}
                  onChange={(e) => handleEnvVarChange(index, e.target.value)}
                  placeholder="e.g., LOG_LEVEL=debug"
                  disabled={isLoading}
                />
                <button
                  className={styles.removeButton}
                  onClick={() => handleRemoveEnvVar(index)}
                  disabled={isLoading}
                  title="Remove"
                >
                  <X size={16} />
                </button>
              </div>
            ))}
          </div>
        )}
      </div>

      <div className={styles.section}>
        <label className={styles.label}>
          <span className={styles.labelText}>Restart Policy</span>
          <select
            className={styles.select}
            value={formData.restartPolicy}
            onChange={(e) => handleRestartPolicyChange(e.target.value)}
            disabled={isLoading}
          >
            <option value="No">No</option>
            <option value="Always">Always</option>
            <option value="UnlessStopped">Unless Stopped</option>
            <option value="OnFailure">On Failure</option>
          </select>
        </label>
      </div>

      <div className={styles.actions}>
        <Button
          variant="primary"
          onClick={handleSubmit}
          isLoading={isLoading}
          disabled={isLoading}
        >
          Save Configuration
        </Button>
      </div>
    </div>
  )
}
