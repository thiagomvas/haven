import { FileUp, HardDriveDownload } from 'lucide-react';
import { ChangeEvent, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';

import { sidecarsApi } from '@/api/sidecars';
import { SidecarDto } from '@/api/types';
import { Button } from '@/components/ui/Button';
import { Modal } from '@/components/ui/Modal';
import { Textarea } from '@/components/ui/Textarea';
import styles from '@/styles/components/sidecars/ImportSidecarManifestModal.module.css';

interface ImportSidecarManifestModalProps {
  sidecar: SidecarDto;
  isOpen: boolean;
  onClose: () => void;
  onImport: (manifestYaml: string) => Promise<void>;
  isImporting: boolean;
}

export function ImportSidecarManifestModal({
  sidecar,
  isOpen,
  onClose,
  onImport,
  isImporting,
}: ImportSidecarManifestModalProps) {
  const { t } = useTranslation(['sidecars', 'common']);
  const [manifestYaml, setManifestYaml] = useState('');
  const [error, setError] = useState<string | undefined>(undefined);
  const [isLoadingFromDisk, setIsLoadingFromDisk] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const handleClose = () => {
    setManifestYaml('');
    setError(undefined);
    onClose();
  };

  const handleFileChange = async (e: ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    e.target.value = '';
    if (!file) return;

    try {
      setManifestYaml(await file.text());
      setError(undefined);
    } catch {
      setError(t('importModal.fileReadError'));
    }
  };

  const handleLoadFromDisk = async () => {
    setError(undefined);
    setIsLoadingFromDisk(true);
    try {
      const content = await sidecarsApi.getManifest(sidecar.id);
      setManifestYaml(content ?? '');
    } catch (err) {
      setError(err instanceof Error ? err.message : t('importModal.loadFromDiskError'));
    } finally {
      setIsLoadingFromDisk(false);
    }
  };

  const handleImport = async () => {
    setError(undefined);
    try {
      await onImport(manifestYaml);
      handleClose();
    } catch (err) {
      setError(err instanceof Error ? err.message : t('importError'));
    }
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={handleClose}
      title={t('importModal.title', { name: sidecar.name })}
      description={t('importModal.description')}
      size="lg"
      error={error}
      footer={
        <div className={styles.footer}>
          <Button variant="ghost" onClick={handleClose} disabled={isImporting}>
            {t('common:actions.cancel')}
          </Button>
          <Button
            variant="primary"
            onClick={handleImport}
            isLoading={isImporting}
            disabled={!manifestYaml.trim() || isLoadingFromDisk}
          >
            {t('importModal.apply')}
          </Button>
        </div>
      }
    >
      <div className={styles.actionsRow}>
        <input
          ref={fileInputRef}
          type="file"
          accept=".yaml,.yml,.txt"
          className={styles.hiddenFileInput}
          onChange={handleFileChange}
        />
        <Button
          type="button"
          variant="outline"
          size="sm"
          icon={<FileUp size={14} />}
          onClick={() => fileInputRef.current?.click()}
          disabled={isImporting}
        >
          {t('importModal.uploadFile')}
        </Button>
        <Button
          type="button"
          variant="outline"
          size="sm"
          icon={<HardDriveDownload size={14} />}
          onClick={handleLoadFromDisk}
          isLoading={isLoadingFromDisk}
          disabled={isImporting}
        >
          {t('importModal.loadFromDisk')}
        </Button>
      </div>

      <Textarea
        value={manifestYaml}
        onChange={e => setManifestYaml(e.target.value)}
        placeholder={t('importModal.placeholder')}
        className={styles.textarea}
        rows={16}
        disabled={isImporting}
      />
    </Modal>
  );
}
