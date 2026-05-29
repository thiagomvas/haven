import { useTranslation } from 'react-i18next'
import { FormGroup, FormLabel } from '../ui/Form'
import styles from './PortMappingsEditor.module.css'

export interface PortMapping {
  host: string
  container: string
}

interface PortMappingsEditorProps {
  portMappings: PortMapping[]
  onChange: (mappings: PortMapping[]) => void
  disabled?: boolean
}

export function PortMappingsEditor({ portMappings, onChange, disabled }: PortMappingsEditorProps) {
  const { t } = useTranslation('services')

  const updatePort = (idx: number, field: keyof PortMapping, value: string) => {
    const updated = portMappings.map((p, i) => (i === idx ? { ...p, [field]: value } : p))
    onChange(updated)
  }

  const removePort = (idx: number) => {
    onChange(portMappings.filter((_, i) => i !== idx))
  }

  const addPort = () => {
    onChange([...portMappings, { host: '', container: '' }])
  }

  return (
    <FormGroup>
      <div className={styles.labelWithHelp}>
        <FormLabel htmlFor="portMappings">{t('createPage.portMappings')}</FormLabel>
        <span className={styles.helpText}>{t('createPage.portMappingsHelp')}</span>
      </div>
      <div className={styles.portsContainer}>
        {portMappings.length === 0 ? (
          <p className={styles.emptyState}>{t('createPage.noPortMappings')}</p>
        ) : (
          portMappings.map((port, idx) => (
            <div key={idx} className={styles.portRow}>
              <input
                type="text"
                className={styles.portInput}
                placeholder={t('createPage.hostPortPlaceholder')}
                value={port.host}
                onChange={(e) => updatePort(idx, 'host', e.target.value)}
                disabled={disabled}
              />
              <span className={styles.portSeparator}>:</span>
              <input
                type="text"
                className={styles.portInput}
                placeholder={t('createPage.containerPortPlaceholder')}
                value={port.container}
                onChange={(e) => updatePort(idx, 'container', e.target.value)}
                disabled={disabled}
              />
              <button
                type="button"
                className={styles.portRemove}
                onClick={() => removePort(idx)}
                disabled={disabled}
              >
                ×
              </button>
            </div>
          ))
        )}
      </div>
      <button type="button" className={styles.addPortButton} onClick={addPort} disabled={disabled}>
        {t('createPage.addPort')}
      </button>
    </FormGroup>
  )
}
