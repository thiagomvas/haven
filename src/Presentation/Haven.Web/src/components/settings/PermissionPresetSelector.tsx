import { useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { Stack } from '@/components/layout'
import styles from './PermissionPresetSelector.module.css'

interface Props {
  presets: Record<string, { permissions: string[] }>
  selectedPermissions: string[]
  onPresetSelect: (permissions: string[]) => void
  disabled?: boolean
}

export function PermissionPresetSelector({ presets, selectedPermissions, onPresetSelect, disabled = false }: Props) {
  const { t } = useTranslation('settings')

  const activePreset = useMemo(() => {
    for (const [key, preset] of Object.entries(presets)) {
      const presetSet = new Set(preset.permissions)
      const selectedSet = new Set(selectedPermissions)
      if (presetSet.size === selectedSet.size && [...presetSet].every(p => selectedSet.has(p))) {
        return key
      }
    }
    return null
  }, [presets, selectedPermissions])

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <div className={styles.label}>{t('users.createModal.permissionPreset')}</div>
        {selectedPermissions.length > 0 && (
          <div className={styles.count}>{selectedPermissions.length} selected</div>
        )}
      </div>
      <div className={styles.presetsGrid}>
        {Object.entries(presets).map(([key]) => (
          <button
            key={key}
            type="button"
            className={`${styles.presetButton} ${activePreset === key ? styles.active : ''}`}
            onClick={() => onPresetSelect(presets[key].permissions)}
            disabled={disabled}
          >
            <Stack gap="2">
              <div className={styles.presetTitle}>
                {t(`users.createModal.presets.${key}.title` as any)}
              </div>
              <div className={styles.presetDescription}>
                {t(`users.createModal.presets.${key}.description` as any)}
              </div>
              {activePreset === key && <div className={styles.badge}>✓ Active</div>}
            </Stack>
          </button>
        ))}
      </div>
    </div>
  )
}
