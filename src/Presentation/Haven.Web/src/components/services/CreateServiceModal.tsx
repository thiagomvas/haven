import { useState, useEffect } from 'react'
import { Container, FileCode, Layers, Terminal } from 'lucide-react'
import { servicesApi } from '../../api/services'
import {
  CreateServiceInput,
  DockerfileConfig,
  DockerfileSource,
  ExposureMode,
  RestartPolicy,
  ServiceType,
} from '../../api/types'
import { useBranchAutocomplete } from '../../hooks/useBranchAutocomplete'
import { useGitCredentials } from '../../hooks/useGitCredentials'
import { BranchInput } from '../ui/BranchInput'
import { SelectInput } from '../ui/SelectInput'
import { Modal } from '../ui/Modal'
import { Button } from '../ui/Button'
import styles from './CreateServiceModal.module.css'

interface CreateServiceModalProps {
  projectId: string
  environmentId: string
  isOpen: boolean
  onClose: () => void
  onSuccess?: (serviceId: string) => void
}

interface ServiceTypeOption {
  type: ServiceType
  label: string
  description: string
  icon: React.ReactNode
}

const SERVICE_TYPE_OPTIONS: ServiceTypeOption[] = [
  {
    type: 'DockerImage',
    label: 'Docker Image',
    description: 'Pull a pre-built image from a registry',
    icon: <Container size={28} />,
  },
  {
    type: 'Dockerfile',
    label: 'Dockerfile',
    description: 'Build from a Dockerfile source',
    icon: <FileCode size={28} />,
  },
  {
    type: 'Compose',
    label: 'Compose',
    description: 'Use a Docker Compose file',
    icon: <Layers size={28} />,
  },
  {
    type: 'Process',
    label: 'Process',
    description: 'Run a native process',
    icon: <Terminal size={28} />,
  },
]

const EXPOSURE_MODES: ExposureMode[] = ['None', 'Internal', 'External']
const RESTART_POLICIES: RestartPolicy[] = ['No', 'Always', 'UnlessStopped', 'OnFailure']

export function CreateServiceModal({
  projectId,
  environmentId,
  isOpen,
  onClose,
  onSuccess,
}: CreateServiceModalProps) {
  const [selectedType, setSelectedType] = useState<ServiceType>('DockerImage')
  const [name, setName] = useState('')
  const [exposureMode, setExposureMode] = useState<ExposureMode>('None')

  // DockerImage fields
  const [dockerImage, setDockerImage] = useState('')
  const [dockerPorts, setDockerPorts] = useState('')
  const [dockerVolumes, setDockerVolumes] = useState('')
  const [dockerEnvVars, setDockerEnvVars] = useState('')
  const [restartPolicy, setRestartPolicy] = useState<RestartPolicy>('UnlessStopped')

  // Dockerfile fields
  const [dockerfileSource, setDockerfileSource] = useState<DockerfileSource>('Git')
  const [repository, setRepository] = useState('')
  const [branch, setBranch] = useState('')
  const [filePath, setFilePath] = useState('')
  const [rawContent, setRawContent] = useState('')
  const [gitCredentialId, setGitCredentialId] = useState<string | undefined>(undefined)

  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const { data: credentialsPage } = useGitCredentials({ pageNumber: 1, pageSize: 100 })
  const credentials = credentialsPage?.items ?? []

  const { branches, isLoading: branchesLoading } = useBranchAutocomplete(
    dockerfileSource === 'Git' ? repository : '',
    gitCredentialId,
  )

  useEffect(() => {
    if (isOpen) handleReset()
  }, [isOpen, environmentId])

  const handleReset = () => {
    setSelectedType('DockerImage')
    setName('')
    setExposureMode('None')
    setDockerImage('')
    setDockerPorts('')
    setDockerVolumes('')
    setDockerEnvVars('')
    setRestartPolicy('UnlessStopped')
    setDockerfileSource('Git')
    setRepository('')
    setBranch('')
    setFilePath('')
    setRawContent('')
    setGitCredentialId(undefined)
    setError(null)
  }

  const handleClose = () => {
    handleReset()
    onClose()
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)

    if (!name.trim()) {
      setError('Service name is required.')
      return
    }

    let dockerfileConfig: DockerfileConfig | undefined
    if (selectedType === 'Dockerfile') {
      if (dockerfileSource === 'Git') {
        if (!repository.trim() || !branch.trim()) {
          setError('Repository URL and branch are required for a Git-sourced Dockerfile.')
          return
        }
        dockerfileConfig = {
          source: 'Git',
          repository: repository.trim(),
          branch: branch.trim(),
          filePath: filePath.trim() || undefined,
          gitCredentialId: gitCredentialId || undefined,
        }
      } else {
        if (!rawContent.trim()) {
          setError('Dockerfile content is required.')
          return
        }
        dockerfileConfig = { source: 'Raw', content: rawContent.trim() }
      }
    }

    const input: CreateServiceInput = {
      name: name.trim(),
      type: selectedType,
      exposureMode,
      dockerConfig:
        selectedType === 'DockerImage'
          ? {
              image: dockerImage.trim(),
              ports: dockerPorts.split('\n').filter((p) => p.trim()),
              volumes: dockerVolumes.split('\n').filter((v) => v.trim()),
              environmentVariables: dockerEnvVars.split('\n').filter((e) => e.trim()),
              restartPolicy,
            }
          : undefined,
      dockerfileConfig,
    }

    setIsLoading(true)
    try {
      await servicesApi.create(projectId, environmentId, input)
      handleClose()
      onSuccess?.('')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create service')
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <Modal
      isOpen={isOpen}
      onClose={handleClose}
      title="Create Service"
      size="lg"
      closeOnEscape={!isLoading}
      closeOnBackdropClick={!isLoading}
    >
      <form onSubmit={handleSubmit} className={styles.content}>
        {/* Service Type Selection */}
        <div className={styles.section}>
          <div>
            <h3 className={styles.sectionTitle}>Service Type</h3>
            <p className={styles.sectionDescription}>
              Choose how this service will be sourced and run
            </p>
          </div>
          <div className={styles.typeGrid}>
            {SERVICE_TYPE_OPTIONS.map((opt) => (
              <button
                key={opt.type}
                type="button"
                className={`${styles.typeCard} ${selectedType === opt.type ? styles.selected : ''}`}
                onClick={() => setSelectedType(opt.type)}
                disabled={isLoading}
              >
                <div className={styles.typeIcon}>{opt.icon}</div>
                <span className={styles.typeLabel}>{opt.label}</span>
                <span className={styles.typeDesc}>{opt.description}</span>
              </button>
            ))}
          </div>
        </div>

        {/* Common Fields */}
        <div className={styles.section}>
          <div className={styles.formSection}>
            <div className={styles.formGroup}>
              <label className={styles.label}>
                Service Name <span className={styles.required}>*</span>
              </label>
              <input
                type="text"
                className={styles.input}
                placeholder="e.g., my-api, web-server"
                value={name}
                onChange={(e) => setName(e.target.value)}
                disabled={isLoading}
                maxLength={64}
              />
            </div>

            <div className={styles.formGroup}>
              <SelectInput
                label="Exposure Mode"
                value={exposureMode}
                onChange={(v) => setExposureMode(v as ExposureMode)}
                options={EXPOSURE_MODES.map((m) => ({ value: m, label: m }))}
                disabled={isLoading}
              />
            </div>

            {/* DockerImage Config */}
            {selectedType === 'DockerImage' && (
              <div className={styles.configFields}>
                <div className={styles.formGroup}>
                  <label className={styles.label}>Docker Image</label>
                  <input
                    type="text"
                    className={styles.input}
                    placeholder="e.g., nginx:latest, ubuntu:22.04"
                    value={dockerImage}
                    onChange={(e) => setDockerImage(e.target.value)}
                    disabled={isLoading}
                  />
                </div>
                <div className={styles.formGroup}>
                  <div className={styles.labelWithHelp}>
                    <label className={styles.label}>Ports</label>
                    <span className={styles.helpText}>One port mapping per line</span>
                  </div>
                  <textarea
                    className={styles.textarea}
                    placeholder={'e.g., 8080:80\n3000:3000'}
                    value={dockerPorts}
                    onChange={(e) => setDockerPorts(e.target.value)}
                    disabled={isLoading}
                  />
                </div>
                <div className={styles.formGroup}>
                  <div className={styles.labelWithHelp}>
                    <label className={styles.label}>Volumes</label>
                    <span className={styles.helpText}>One volume mount per line</span>
                  </div>
                  <textarea
                    className={styles.textarea}
                    placeholder={'e.g., /data:/data\n./config:/etc/config'}
                    value={dockerVolumes}
                    onChange={(e) => setDockerVolumes(e.target.value)}
                    disabled={isLoading}
                  />
                </div>
                <div className={styles.formGroup}>
                  <div className={styles.labelWithHelp}>
                    <label className={styles.label}>Environment Variables</label>
                    <span className={styles.helpText}>One variable per line (KEY=VALUE)</span>
                  </div>
                  <textarea
                    className={styles.textarea}
                    placeholder={'e.g., DEBUG=true\nNODE_ENV=production'}
                    value={dockerEnvVars}
                    onChange={(e) => setDockerEnvVars(e.target.value)}
                    disabled={isLoading}
                  />
                </div>
                <div className={styles.formGroup}>
                  <SelectInput
                    label="Restart Policy"
                    value={restartPolicy}
                    onChange={(v) => setRestartPolicy(v as RestartPolicy)}
                    options={RESTART_POLICIES.map((p) => ({ value: p, label: p }))}
                    disabled={isLoading}
                  />
                </div>
              </div>
            )}

            {/* Dockerfile Config */}
            {selectedType === 'Dockerfile' && (
              <div className={styles.configFields}>
                <div className={styles.formGroup}>
                  <label className={styles.label}>Source</label>
                  <div className={styles.sourceToggle}>
                    <button
                      type="button"
                      className={`${styles.toggleButton} ${dockerfileSource === 'Git' ? styles.active : ''}`}
                      onClick={() => setDockerfileSource('Git')}
                      disabled={isLoading}
                    >
                      Git Repository
                    </button>
                    <button
                      type="button"
                      className={`${styles.toggleButton} ${dockerfileSource === 'Raw' ? styles.active : ''}`}
                      onClick={() => setDockerfileSource('Raw')}
                      disabled={isLoading}
                    >
                      Raw Content
                    </button>
                  </div>
                </div>

                {dockerfileSource === 'Git' ? (
                  <>
                    <div className={styles.formGroup}>
                      <SelectInput
                        label="Git Credential"
                        value={gitCredentialId ?? ''}
                        onChange={(v) => setGitCredentialId(v || undefined)}
                        options={credentials.map((c) => ({ value: c.id, label: c.displayName }))}
                        placeholder="None (public repository)"
                        disabled={isLoading}
                      />
                    </div>

                    <div className={styles.formGroup}>
                      <label className={styles.label}>
                        Repository URL <span className={styles.required}>*</span>
                      </label>
                      <input
                        type="url"
                        className={styles.input}
                        placeholder="https://github.com/org/repo"
                        value={repository}
                        onChange={(e) => setRepository(e.target.value)}
                        disabled={isLoading}
                      />
                    </div>
                    <div className={styles.formGroup}>
                      <BranchInput
                        label="Branch *"
                        value={branch}
                        onChange={setBranch}
                        branches={branches}
                        isLoadingBranches={branchesLoading}
                        disabled={isLoading}
                      />
                    </div>
                    <div className={styles.formGroup}>
                      <div className={styles.labelWithHelp}>
                        <label className={styles.label}>Dockerfile Path</label>
                        <span className={styles.helpText}>Optional — defaults to ./Dockerfile</span>
                      </div>
                      <input
                        type="text"
                        className={styles.input}
                        placeholder="e.g., docker/Dockerfile"
                        value={filePath}
                        onChange={(e) => setFilePath(e.target.value)}
                        disabled={isLoading}
                      />
                    </div>
                  </>
                ) : (
                  <div className={styles.formGroup}>
                    <label className={styles.label}>
                      Dockerfile Content <span className={styles.required}>*</span>
                    </label>
                    <textarea
                      className={styles.dockerfileTextarea}
                      placeholder={'FROM node:20-alpine\nWORKDIR /app\nCOPY . .\nRUN npm install\nCMD ["node", "index.js"]'}
                      value={rawContent}
                      onChange={(e) => setRawContent(e.target.value)}
                      disabled={isLoading}
                    />
                  </div>
                )}
              </div>
            )}

            {/* Compose / Process placeholders */}
            {(selectedType === 'Compose' || selectedType === 'Process') && (
              <div className={styles.comingSoon}>
                Configuration for {selectedType} services is coming soon.
              </div>
            )}
          </div>

          {error && <div className={styles.error}>{error}</div>}

          <div className={styles.footer}>
            <Button variant="secondary" onClick={handleClose} disabled={isLoading}>
              Cancel
            </Button>
            <button
              type="submit"
              className={styles.primaryButton}
              disabled={isLoading || !name.trim()}
            >
              {isLoading ? 'Creating…' : 'Create Service'}
            </button>
          </div>
        </div>
      </form>
    </Modal>
  )
}
