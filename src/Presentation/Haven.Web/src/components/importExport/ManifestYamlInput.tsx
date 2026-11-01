import { Upload } from 'lucide-react';
import { useRef } from 'react';
import { useTranslation } from 'react-i18next';

import { Button } from '../ui/Button';
import { FormGroup, FormLabel, FormTextarea } from '../ui/Form';

interface ManifestYamlInputProps {
  value: string;
  onChange: (yaml: string) => void;
  disabled?: boolean;
}

/** Lets the user either pick a manifest file or paste its YAML directly. */
export function ManifestYamlInput({ value, onChange, disabled }: ManifestYamlInputProps) {
  const { t } = useTranslation('common');
  const fileInputRef = useRef<HTMLInputElement>(null);

  const handleFile = async (file: File | undefined) => {
    if (!file) return;
    onChange(await file.text());
    // Reset so picking the same file again still fires onChange.
    if (fileInputRef.current) fileInputRef.current.value = '';
  };

  return (
    <FormGroup>
      <FormLabel htmlFor="manifest-yaml" required>
        {t('manifestImport.yaml')}
      </FormLabel>
      <input
        ref={fileInputRef}
        type="file"
        accept=".yaml,.yml,text/yaml,text/plain"
        hidden
        onChange={e => handleFile(e.target.files?.[0])}
      />
      <div style={{ marginBottom: 'var(--space-2)' }}>
        <Button
          variant="secondary"
          size="sm"
          icon={<Upload size={16} />}
          onClick={() => fileInputRef.current?.click()}
          disabled={disabled}
        >
          {t('manifestImport.chooseFile')}
        </Button>
      </div>
      <FormTextarea
        id="manifest-yaml"
        placeholder={t('manifestImport.yamlPlaceholder')}
        value={value}
        onChange={e => onChange(e.target.value)}
        disabled={disabled}
        rows={10}
        spellCheck={false}
        style={{ fontFamily: 'var(--font-mono, monospace)' }}
      />
    </FormGroup>
  );
}
