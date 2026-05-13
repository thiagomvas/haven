import { useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { Plus, Trash2, Check, X } from 'lucide-react'
import { featureFlagsApi } from '../../api/featureFlags'
import { FeatureFlagDto, CreateFeatureFlagInput, FeatureFlagValueType, FeatureFlagType } from '../../api/types'
import { Button } from '../ui/Button'
import { Spinner } from '../ui/Spinner'
import styles from './FeatureFlagsEditor.module.css'

interface FeatureFlagsEditorProps {
  projectId: string
  environmentId: string
  serviceId: string
}

type FlagEdit = {
  [key: string]: Partial<FeatureFlagDto>
}

interface NewFlagFormState {
  name: string
  description: string
  type: FeatureFlagType
  key: string
  value: string
  valueType: FeatureFlagValueType
}

const BoolSwitch = ({ value, onChange }: { value: boolean; onChange: (v: boolean) => void }) => (
  <button
    type="button"
    className={`${styles.switch} ${value ? styles.switchOn : ''}`}
    onClick={() => onChange(!value)}
  >
    <span className={styles.switchThumb} />
  </button>
)

export function FeatureFlagsEditor({
  projectId,
  environmentId,
  serviceId,
}: FeatureFlagsEditorProps) {
  const { t } = useTranslation(['services'])
  const [flags, setFlags] = useState<FeatureFlagDto[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [edits, setEdits] = useState<FlagEdit>({})
  const [actionLoading, setActionLoading] = useState(false)
  const [newFlagForm, setNewFlagForm] = useState<NewFlagFormState>({
    name: '',
    description: '',
    type: 'EnvironmentVariable',
    key: '',
    value: '',
    valueType: 'String',
  })

  useEffect(() => {
    loadFlags()
  }, [projectId, environmentId, serviceId])

  const loadFlags = async () => {
    try {
      setLoading(true)
      const result = await featureFlagsApi.list(projectId, environmentId, serviceId)
      if (result && result.items) {
        setFlags(result.items)
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : t('error'))
    } finally {
      setLoading(false)
    }
  }

  const updateFlagValue = (flagId: string, updates: Partial<FeatureFlagDto>) => {
    setEdits((prev) => ({
      ...prev,
      [flagId]: { ...prev[flagId], ...updates },
    }))
  }

  const saveChanges = async () => {
    if (Object.keys(edits).length === 0) return

    try {
      setActionLoading(true)
      const updates = Object.entries(edits).map(([flagId, data]) => ({
        flagId,
        name: data.name,
        type: data.type,
        key: data.key,
        description: data.description,
        value: data.value,
        valueType: data.valueType,
      }))

      await featureFlagsApi.batchUpdate(projectId, environmentId, serviceId, updates as any)
      setEdits({})
      await loadFlags()
    } catch (err) {
      setError(err instanceof Error ? err.message : t('error'))
    } finally {
      setActionLoading(false)
    }
  }

  const createFlag = async () => {
    if (!newFlagForm.name || !newFlagForm.value) return

    try {
      setActionLoading(true)
      await featureFlagsApi.create(projectId, environmentId, serviceId, {
        name: newFlagForm.name,
        type: newFlagForm.type,
        key: newFlagForm.type === 'EnvironmentVariable' ? newFlagForm.key : undefined,
        description: newFlagForm.description,
        value: newFlagForm.value,
        valueType: newFlagForm.valueType,
      })
      setNewFlagForm({
        name: '',
        description: '',
        type: 'EnvironmentVariable',
        key: '',
        value: '',
        valueType: 'String',
      })
      await loadFlags()
    } catch (err) {
      setError(err instanceof Error ? err.message : t('error'))
    } finally {
      setActionLoading(false)
    }
  }

  const deleteFlag = async (flagId: string) => {
    if (!confirm(t('services:confirmDelete'))) return

    try {
      setActionLoading(true)
      await featureFlagsApi.delete(projectId, environmentId, serviceId, flagId)
      await loadFlags()
    } catch (err) {
      setError(err instanceof Error ? err.message : t('error'))
    } finally {
      setActionLoading(false)
    }
  }

  const getFlagValue = (flag: FeatureFlagDto, fieldName: keyof Omit<FeatureFlagDto, 'id' | 'serviceId' | 'type'>) => {
    return edits[flag.id]?.[fieldName] ?? flag[fieldName]
  }

  if (loading) {
    return (
      <div className={styles.container}>
        <Spinner />
      </div>
    )
  }

  const hasChanges = Object.keys(edits).length > 0

  return (
    <div className={styles.container}>
      {error && (
        <div className={styles.error}>
          <p>{error}</p>
          <button onClick={() => setError(null)}>✕</button>
        </div>
      )}

      <div className={styles.header}>
        <h3 className={styles.title}>Feature Flags</h3>
        <div className={styles.headerActions}>
          {hasChanges && (
            <Button
              variant="primary"
              onClick={saveChanges}
              disabled={actionLoading}
              isLoading={actionLoading}
            >
              {t('projects:save')}
            </Button>
          )}
        </div>
      </div>

      <div className={styles.flagRow}>
        <input
          type="text"
          placeholder="Name"
          value={newFlagForm.name}
          onChange={(e) => setNewFlagForm({ ...newFlagForm, name: e.target.value })}
          disabled={actionLoading}
          className={styles.input}
        />
        <input
          type="text"
          placeholder="Description"
          value={newFlagForm.description}
          onChange={(e) => setNewFlagForm({ ...newFlagForm, description: e.target.value })}
          disabled={actionLoading}
          className={styles.input}
        />
        <select
          value={newFlagForm.type}
          onChange={(e) =>
            setNewFlagForm({
              ...newFlagForm,
              type: e.target.value as FeatureFlagType,
            })
          }
          disabled={actionLoading}
          className={styles.select}
        >
          <option value="EnvironmentVariable">Environment Variable</option>
        </select>
        <input
          type="text"
          placeholder="Key"
          value={newFlagForm.key}
          onChange={(e) => setNewFlagForm({ ...newFlagForm, key: e.target.value })}
          disabled={actionLoading || newFlagForm.type !== 'EnvironmentVariable'}
          className={styles.input}
          style={{
            opacity: newFlagForm.type !== 'EnvironmentVariable' ? 0.5 : 1,
          }}
        />
        <select
          value={newFlagForm.valueType}
          onChange={(e) =>
            setNewFlagForm({
              ...newFlagForm,
              valueType: e.target.value as FeatureFlagValueType,
            })
          }
          disabled={actionLoading}
          className={styles.select}
        >
          <option value="String">String</option>
          <option value="Bool">Boolean</option>
          <option value="Number">Number</option>
        </select>
        {newFlagForm.valueType === 'Bool' ? (
          <BoolSwitch
            value={newFlagForm.value === 'true'}
            onChange={(v) => setNewFlagForm({ ...newFlagForm, value: String(v) })}
          />
        ) : newFlagForm.valueType === 'Number' ? (
          <input
            type="number"
            value={newFlagForm.value}
            onChange={(e) => setNewFlagForm({ ...newFlagForm, value: e.target.value })}
            disabled={actionLoading}
            className={styles.input}
          />
        ) : (
          <input
            type="text"
            value={newFlagForm.value}
            onChange={(e) => setNewFlagForm({ ...newFlagForm, value: e.target.value })}
            disabled={actionLoading}
            className={styles.input}
          />
        )}
        <button
          onClick={createFlag}
          disabled={actionLoading || !newFlagForm.name || !newFlagForm.value}
          className={styles.createBtn}
          title="Create"
        >
          <Check size={16} />
        </button>
      </div>

      {flags.length > 0 && (
        <div className={styles.flagsHeader}>
          <div className={styles.headerName}>Name</div>
          <div className={styles.headerDescription}>Description</div>
          <div className={styles.headerType}>Flag Type</div>
          <div className={styles.headerKey}>Key</div>
          <div className={styles.headerValueType}>Value Type</div>
          <div className={styles.headerValue}>Value</div>
          <div />
        </div>
      )}

      <div className={styles.flagsList}>
        {flags.length === 0 ? (
          <p className={styles.empty}>{t('services:noFlags')}</p>
        ) : (
          flags.map((flag) => (
            <div key={flag.id} className={styles.flagRow}>
              <input
                type="text"
                value={getFlagValue(flag, 'name')}
                onChange={(e) => updateFlagValue(flag.id, { name: e.target.value })}
                disabled={actionLoading}
                className={styles.input}
              />
              <input
                type="text"
                value={getFlagValue(flag, 'description') || ''}
                onChange={(e) => updateFlagValue(flag.id, { description: e.target.value })}
                disabled={actionLoading}
                className={styles.input}
                placeholder="Description"
              />
              <select
                value={getFlagValue(flag, 'type')}
                onChange={(e) =>
                  updateFlagValue(flag.id, { type: e.target.value as FeatureFlagType })
                }
                disabled={actionLoading}
                className={styles.select}
              >
                <option value="EnvironmentVariable">Environment Variable</option>
              </select>
              <input
                type="text"
                value={getFlagValue(flag, 'key') || ''}
                onChange={(e) => updateFlagValue(flag.id, { key: e.target.value })}
                disabled={actionLoading || getFlagValue(flag, 'type') !== 'EnvironmentVariable'}
                className={styles.input}
                placeholder="Key"
                style={{
                  opacity: getFlagValue(flag, 'type') !== 'EnvironmentVariable' ? 0.5 : 1,
                }}
              />
              <select
                value={getFlagValue(flag, 'valueType')}
                onChange={(e) =>
                  updateFlagValue(flag.id, { valueType: e.target.value as FeatureFlagValueType })
                }
                disabled={actionLoading}
                className={styles.select}
              >
                <option value="String">String</option>
                <option value="Bool">Boolean</option>
                <option value="Number">Number</option>
              </select>
              {getFlagValue(flag, 'valueType') === 'Bool' ? (
                <BoolSwitch
                  value={getFlagValue(flag, 'value') === 'true'}
                  onChange={(v) => updateFlagValue(flag.id, { value: String(v) })}
                />
              ) : getFlagValue(flag, 'valueType') === 'Number' ? (
                <input
                  type="number"
                  value={getFlagValue(flag, 'value')}
                  onChange={(e) => updateFlagValue(flag.id, { value: e.target.value })}
                  disabled={actionLoading}
                  className={styles.input}
                />
              ) : (
                <input
                  type="text"
                  value={getFlagValue(flag, 'value')}
                  onChange={(e) => updateFlagValue(flag.id, { value: e.target.value })}
                  disabled={actionLoading}
                  className={styles.input}
                />
              )}
              <button
                onClick={() => deleteFlag(flag.id)}
                disabled={actionLoading}
                className={styles.deleteBtn}
                title="Delete"
              >
                <Trash2 size={16} />
              </button>
            </div>
          ))
        )}
      </div>
    </div>
  )
}
