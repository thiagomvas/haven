import { useTranslation } from 'react-i18next';

import { Grid, Stack } from '@/components/layout';
import styles from '@/styles/components/services/PortMappingsEditor.module.css';

import { FormGroup, FormLabel } from '../ui/Form';

export interface PortMapping {
  host: string;
  container: string;
  ip?: string;
}

interface PortMappingsEditorProps {
  portMappings: PortMapping[];
  onChange: (mappings: PortMapping[]) => void;
  disabled?: boolean;
  showIpField?: boolean;
}

export function PortMappingsEditor({
  portMappings,
  onChange,
  disabled,
  showIpField,
}: PortMappingsEditorProps) {
  const { t } = useTranslation('services');

  const updatePort = (idx: number, field: keyof PortMapping, value: string) => {
    const updated = portMappings.map((p, i) => (i === idx ? { ...p, [field]: value } : p));
    onChange(updated);
  };

  const removePort = (idx: number) => {
    onChange(portMappings.filter((_, i) => i !== idx));
  };

  const addPort = () => {
    onChange([...portMappings, { host: '', container: '', ip: '' }]);
  };

  return (
    <FormGroup>
      <div className={styles.labelWithHelp}>
        <FormLabel htmlFor="portMappings">{t('createPage.portMappings')}</FormLabel>
        <span className={styles.helpText}>{t('createPage.portMappingsHelp')}</span>
      </div>
      <Stack gap="3" className={styles.portsContainer}>
        {portMappings.length === 0 ? (
          <p className={styles.emptyState}>{t('createPage.noPortMappings')}</p>
        ) : (
          portMappings.map((port, idx) => (
            <Grid
              key={idx}
              gap="2"
              columnTemplate={showIpField ? '1.5fr auto 1fr auto 1fr auto' : '1fr auto 1fr auto'}
              className={styles.portRow}
            >
              {showIpField && (
                <>
                  <input
                    type="text"
                    className={styles.portInput}
                    placeholder={t('createPage.hostIpPlaceholder')}
                    value={port.ip ?? ''}
                    onChange={e => updatePort(idx, 'ip', e.target.value)}
                    disabled={disabled}
                  />
                  <span className={styles.portSeparator}>:</span>
                </>
              )}
              <input
                type="text"
                className={styles.portInput}
                placeholder={t('createPage.hostPortPlaceholder')}
                value={port.host}
                onChange={e => updatePort(idx, 'host', e.target.value)}
                disabled={disabled}
              />
              <span className={styles.portSeparator}>:</span>
              <input
                type="text"
                className={styles.portInput}
                placeholder={t('createPage.containerPortPlaceholder')}
                value={port.container}
                onChange={e => updatePort(idx, 'container', e.target.value)}
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
            </Grid>
          ))
        )}
      </Stack>
      <button type="button" className={styles.addPortButton} onClick={addPort} disabled={disabled}>
        {t('createPage.addPort')}
      </button>
    </FormGroup>
  );
}
