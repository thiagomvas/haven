import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import { Button } from '../ui/Button';
import { FormGroup, FormLabel, FormTextarea } from '../ui/Form';
import { Modal } from '../ui/Modal';

// Only raw manifests are supported for now; other sources (e.g. repository) will be added here.
type ManifestSource = 'raw';

interface ImportManifestModalProps {
  isOpen: boolean;
  onClose: () => void;
  onImport: (source: ManifestSource, content: string) => Promise<void>;
  error?: string;
}

export function ImportManifestModal({ isOpen, onClose, onImport, error }: ImportManifestModalProps) {
  const { t } = useTranslation('services');
  const [rawManifest, setRawManifest] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleImport = async () => {
    setIsSubmitting(true);
    try {
      await onImport('raw', rawManifest);
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title={t('createPage.importFromManifest')}
      description={t('createPage.importManifestDescription')}
      size="lg"
      error={error}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={isSubmitting}>
            {t('createPage.cancel')}
          </Button>
          <Button
            variant="primary"
            onClick={handleImport}
            isLoading={isSubmitting}
            disabled={!rawManifest.trim()}
          >
            {t('createPage.importButton')}
          </Button>
        </>
      }
    >
      <FormGroup>
        <FormLabel htmlFor="rawManifest" required>
          {t('createPage.rawManifest')}
        </FormLabel>
        <FormTextarea
          id="rawManifest"
          value={rawManifest}
          onChange={e => setRawManifest(e.target.value)}
          placeholder={t('createPage.rawManifestPlaceholder')}
          disabled={isSubmitting}
          rows={16}
          spellCheck={false}
          style={{ backgroundColor: 'var(--color-surface-2)', fontFamily: 'var(--font-mono)' }}
        />
      </FormGroup>
    </Modal>
  );
}
