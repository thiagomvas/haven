import { File, FolderOpen, Plus, Trash2 } from 'lucide-react';
import { useCallback, useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';

import styles from '@/styles/components/services/VolumesEditor.module.css';

import { ManagedVolumeFileEntry, ServiceVolumeDto } from '../../api/types/volume.types';
import { volumesApi } from '../../api/volumes';
import { Row, Stack } from '../layout';
import { Button } from '../ui/Button';
import { CodeEditor } from '../ui/CodeEditor';
import { ErrorAlert } from '../ui/ErrorAlert';
import { Input } from '../ui/Input';
import { Label } from '../ui/Label';
import { Modal } from '../ui/Modal';
import { Spinner } from '../ui/Spinner';

interface ManagedVolumeFilesModalProps {
  projectId: string;
  environmentId: string;
  serviceId: string;
  volume: ServiceVolumeDto;
  isOpen: boolean;
  onClose: () => void;
}

export function ManagedVolumeFilesModal({
  projectId,
  environmentId,
  serviceId,
  volume,
  isOpen,
  onClose,
}: ManagedVolumeFilesModalProps) {
  const { t } = useTranslation('services');

  const [files, setFiles] = useState<ManagedVolumeFileEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selectedPath, setSelectedPath] = useState<string | null>(null);
  const [newFilePath, setNewFilePath] = useState('');

  const loadFiles = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const result = await volumesApi.listFiles(projectId, environmentId, serviceId, volume.id);
      setFiles(result ?? []);
    } catch (err) {
      setError(err instanceof Error ? err.message : t('error'));
    } finally {
      setLoading(false);
    }
  }, [projectId, environmentId, serviceId, volume.id, t]);

  useEffect(() => {
    if (isOpen) {
      // eslint-disable-next-line react-hooks/set-state-in-effect -- reset local UI state when modal opens
      setSelectedPath(null);
      setNewFilePath('');

      void loadFiles();
    }
  }, [isOpen, loadFiles]);

  const handleLoadContent = useCallback(
    () =>
      volumesApi
        .getFileContent(projectId, environmentId, serviceId, volume.id, selectedPath ?? '')
        .then(v => v ?? ''),
    [projectId, environmentId, serviceId, volume.id, selectedPath]
  );

  const handleSaveContent = useCallback(
    async (content: string) => {
      if (!selectedPath) return;
      await volumesApi.writeFileContent(
        projectId,
        environmentId,
        serviceId,
        volume.id,
        selectedPath,
        content
      );
      await loadFiles();
    },
    [projectId, environmentId, serviceId, volume.id, selectedPath, loadFiles]
  );

  const handleCreateFile = async () => {
    const path = newFilePath.trim();
    if (!path) return;
    try {
      await volumesApi.writeFileContent(projectId, environmentId, serviceId, volume.id, path, '');
      setNewFilePath('');
      await loadFiles();
      setSelectedPath(path);
    } catch (err) {
      setError(err instanceof Error ? err.message : t('error'));
    }
  };

  const handleDeleteFile = async (path: string) => {
    try {
      await volumesApi.deleteFile(projectId, environmentId, serviceId, volume.id, path);
      if (selectedPath === path) setSelectedPath(null);
      await loadFiles();
    } catch (err) {
      setError(err instanceof Error ? err.message : t('error'));
    }
  };

  const fileEntries = files.filter(f => !f.isDirectory);

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title={t('volumes.filesTitle', { name: volume.name })}
      description={volume.target}
      size="lg"
    >
      <Stack gap="3">
        {error && <ErrorAlert message={error} variant="block" />}

        <Row gap="2" align="flex-end">
          <div className={styles.grow}>
            <Input
              label={t('volumes.newFilePath')}
              value={newFilePath}
              onChange={e => setNewFilePath(e.target.value)}
              placeholder="nginx.conf"
              onKeyDown={e => {
                if (e.key === 'Enter') {
                  e.preventDefault();
                  void handleCreateFile();
                }
              }}
            />
          </div>
          <Button
            variant="secondary"
            icon={<Plus size={14} />}
            onClick={handleCreateFile}
            disabled={!newFilePath.trim()}
          >
            {t('volumes.addFile')}
          </Button>
        </Row>

        {loading ? (
          <div className={styles.spinnerWrap}>
            <Spinner />
          </div>
        ) : (
          <div className={styles.filesLayout}>
            <div className={styles.fileList}>
              {fileEntries.length === 0 ? (
                <Label variant="muted" size="sm">
                  {t('volumes.noFiles')}
                </Label>
              ) : (
                fileEntries.map(file => (
                  <div key={file.path} style={{ display: 'flex', alignItems: 'center' }}>
                    <button
                      className={`${styles.fileItem} ${selectedPath === file.path ? styles.fileItemActive : ''}`}
                      onClick={() => setSelectedPath(file.path)}
                    >
                      <File size={14} />
                      <span className={styles.fileItemPath}>{file.path}</span>
                    </button>
                    <button
                      className={styles.deleteBtn}
                      onClick={() => handleDeleteFile(file.path)}
                      title={t('volumes.deleteFile')}
                    >
                      <Trash2 size={14} />
                    </button>
                  </div>
                ))
              )}
            </div>

            <div className={styles.fileEditorPane}>
              {selectedPath ? (
                <CodeEditor
                  key={selectedPath}
                  onLoad={handleLoadContent}
                  onSave={handleSaveContent}
                  placeholder={selectedPath}
                  saveLabel={t('volumes.saveFile')}
                />
              ) : (
                <div className={styles.fileEditorEmpty}>
                  <Row gap="2" align="center">
                    <FolderOpen size={16} />
                    <Label variant="muted" size="sm">
                      {t('volumes.selectFile')}
                    </Label>
                  </Row>
                </div>
              )}
            </div>
          </div>
        )}
      </Stack>
    </Modal>
  );
}
