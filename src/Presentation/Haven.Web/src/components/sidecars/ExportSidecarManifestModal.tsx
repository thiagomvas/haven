import { Download } from 'lucide-react';
import { useTranslation } from 'react-i18next';

import { Button } from '@/components/ui/Button';
import { CodeBlock } from '@/components/ui/CodeBlock';
import { Modal } from '@/components/ui/Modal';
import styles from '@/styles/components/sidecars/ImportSidecarManifestModal.module.css';

interface ExportSidecarManifestModalProps {
  sidecarName: string;
  isOpen: boolean;
  onClose: () => void;
  manifestYaml: string;
}

export function ExportSidecarManifestModal({
  sidecarName,
  isOpen,
  onClose,
  manifestYaml,
}: ExportSidecarManifestModalProps) {
  const { t } = useTranslation(['sidecars', 'common']);

  const handleDownload = () => {
    const blob = new Blob([manifestYaml], { type: 'text/yaml' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `${sidecarName}.yaml`;
    link.click();
    URL.revokeObjectURL(url);
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title={t('exportModal.title', { name: sidecarName })}
      description={t('exportModal.description')}
      size="lg"
      footer={
        <div className={styles.footer}>
          <Button variant="ghost" onClick={onClose}>
            {t('common:actions.cancel')}
          </Button>
          <Button variant="primary" icon={<Download size={16} />} onClick={handleDownload}>
            {t('exportModal.download')}
          </Button>
        </div>
      }
    >
      <CodeBlock code={manifestYaml} copyable header={`${sidecarName}.yaml`} />
    </Modal>
  );
}
