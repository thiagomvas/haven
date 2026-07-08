import { HardDrive, Plus, Trash2 } from 'lucide-react';
import { useCallback, useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';

import styles from '@/styles/components/services/VolumesEditor.module.css';

import {
  AddVolumeInput,
  ServiceVolumeDto,
  UpdateVolumeInput,
  VolumeType,
} from '../../api/types/volume.types';
import { volumesApi } from '../../api/volumes';
import { Row, Spacer, Stack } from '../layout';
import { Badge } from '../ui/Badge';
import { Button } from '../ui/Button';
import { Checkbox } from '../ui/Checkbox';
import { ErrorAlert } from '../ui/ErrorAlert';
import { Input } from '../ui/Input';
import { Label } from '../ui/Label';
import { Modal } from '../ui/Modal';
import { SelectInput } from '../ui/SelectInput';
import { Spinner } from '../ui/Spinner';
import { ManagedVolumeFilesModal } from './ManagedVolumeFilesModal';

interface VolumesEditorProps {
  projectId: string;
  environmentId: string;
  serviceId: string;
}

const EMPTY_NEW_VOLUME: AddVolumeInput = {
  type: 'Managed',
  name: '',
  target: '',
  source: '',
  readOnly: false,
  backupEnabled: false,
};

const badgeVariant = (type: VolumeType) =>
  type === 'Managed' ? 'primary' : type === 'HostPath' ? 'warning' : 'default';

export function VolumesEditor({ projectId, environmentId, serviceId }: VolumesEditorProps) {
  const { t } = useTranslation('services');

  const [volumes, setVolumes] = useState<ServiceVolumeDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [edits, setEdits] = useState<Record<string, UpdateVolumeInput>>({});
  const [isSaving, setIsSaving] = useState(false);

  const [isAddOpen, setIsAddOpen] = useState(false);
  const [newVolume, setNewVolume] = useState<AddVolumeInput>(EMPTY_NEW_VOLUME);
  const [isCreating, setIsCreating] = useState(false);
  const [createError, setCreateError] = useState<string | null>(null);

  const [deleteTarget, setDeleteTarget] = useState<ServiceVolumeDto | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);

  const [filesVolume, setFilesVolume] = useState<ServiceVolumeDto | null>(null);

  const typeOptions = [
    { value: 'Managed', label: t('volumes.types.managed') },
    { value: 'Named', label: t('volumes.types.named') },
    { value: 'HostPath', label: t('volumes.types.hostPath') },
  ];

  const loadVolumes = useCallback(async () => {
    try {
      setLoading(true);
      const result = await volumesApi.list(projectId, environmentId, serviceId);
      setVolumes(result ?? []);
    } catch (err) {
      setError(err instanceof Error ? err.message : t('error'));
    } finally {
      setLoading(false);
    }
  }, [projectId, environmentId, serviceId, t]);

  useEffect(() => {
    void loadVolumes();
  }, [loadVolumes]);

  const updateField = (id: string, updates: UpdateVolumeInput) => {
    setEdits(prev => ({ ...prev, [id]: { ...prev[id], ...updates } }));
  };

  const getField = <K extends keyof ServiceVolumeDto>(
    volume: ServiceVolumeDto,
    field: K
  ): ServiceVolumeDto[K] =>
    ((edits[volume.id] as Record<string, unknown> | undefined)?.[field as string] ??
      volume[field]) as ServiceVolumeDto[K];

  const hasChanges = Object.keys(edits).length > 0;

  const saveChanges = async () => {
    if (!hasChanges) return;
    try {
      setIsSaving(true);
      await Promise.all(
        Object.entries(edits).map(([id, data]) =>
          volumesApi.update(projectId, environmentId, serviceId, id, data)
        )
      );
      setEdits({});
      await loadVolumes();
    } catch (err) {
      setError(err instanceof Error ? err.message : t('error'));
    } finally {
      setIsSaving(false);
    }
  };

  const needsSource = newVolume.type === 'Named' || newVolume.type === 'HostPath';
  const canCreate =
    !!newVolume.name.trim() &&
    !!newVolume.target.trim() &&
    (!needsSource || !!newVolume.source?.trim());

  const handleCreate = async () => {
    if (!canCreate) return;
    try {
      setIsCreating(true);
      setCreateError(null);
      await volumesApi.add(projectId, environmentId, serviceId, {
        type: newVolume.type,
        name: newVolume.name.trim(),
        target: newVolume.target.trim(),
        source: needsSource ? newVolume.source?.trim() : undefined,
        readOnly: newVolume.readOnly,
        backupEnabled: newVolume.backupEnabled,
      });
      setNewVolume(EMPTY_NEW_VOLUME);
      setIsAddOpen(false);
      await loadVolumes();
    } catch (err) {
      setCreateError(err instanceof Error ? err.message : t('error'));
    } finally {
      setIsCreating(false);
    }
  };

  const handleDelete = async () => {
    if (!deleteTarget) return;
    try {
      setIsDeleting(true);
      await volumesApi.delete(projectId, environmentId, serviceId, deleteTarget.id);
      setDeleteTarget(null);
      await loadVolumes();
    } catch (err) {
      setError(err instanceof Error ? err.message : t('error'));
    } finally {
      setIsDeleting(false);
    }
  };

  const openAddModal = () => {
    setNewVolume(EMPTY_NEW_VOLUME);
    setCreateError(null);
    setIsAddOpen(true);
  };

  if (loading) {
    return (
      <div className={styles.spinnerWrap}>
        <Spinner />
      </div>
    );
  }

  return (
    <div className={styles.container}>
      {error && <ErrorAlert message={error} variant="block" />}

      <Row align="center" gap="2">
        <Label variant="primary" size="md" weight="semibold">
          {t('volumes.title')}
        </Label>
        {volumes.length > 0 && <Badge>{volumes.length}</Badge>}
        <Spacer expand direction="horizontal" />
        {hasChanges && (
          <>
            <Button variant="ghost" size="sm" onClick={() => setEdits({})} disabled={isSaving}>
              {t('volumes.discard')}
            </Button>
            <Button variant="primary" size="sm" onClick={saveChanges} isLoading={isSaving}>
              {t('volumes.saveChanges')}
            </Button>
          </>
        )}
        <Button variant="secondary" size="sm" icon={<Plus size={14} />} onClick={openAddModal}>
          {t('volumes.add')}
        </Button>
      </Row>

      {volumes.length === 0 ? (
        <div className={styles.emptyState}>
          <HardDrive size={28} className={styles.emptyIcon} />
          <Label variant="secondary" size="sm">
            {t('volumes.empty')}
          </Label>
          <Button variant="secondary" size="sm" icon={<Plus size={14} />} onClick={openAddModal}>
            {t('volumes.addFirst')}
          </Button>
        </div>
      ) : (
        <Stack gap="2">
          {volumes.map(volume => {
            const isDirty = !!edits[volume.id];
            const isManaged = volume.type === 'Managed';
            return (
              <div
                key={volume.id}
                className={`${styles.volumeCard} ${isDirty ? styles.volumeCardDirty : ''}`}
              >
                <div className={styles.volumeRow}>
                  <Badge variant={badgeVariant(volume.type)}>{volume.type}</Badge>
                  <Input
                    className={styles.grow}
                    value={getField(volume, 'name')}
                    onChange={e => updateField(volume.id, { name: e.target.value })}
                    placeholder={t('volumes.name')}
                    disabled={isSaving}
                  />
                  <button
                    className={styles.deleteBtn}
                    onClick={() => setDeleteTarget(volume)}
                    disabled={isSaving}
                    title={t('volumes.delete')}
                  >
                    <Trash2 size={14} />
                  </button>
                </div>

                <div className={styles.volumeRow}>
                  <Input
                    className={`${styles.grow} ${styles.mono}`}
                    value={getField(volume, 'target')}
                    onChange={e => updateField(volume.id, { target: e.target.value })}
                    placeholder={t('volumes.target')}
                    disabled={isSaving}
                  />
                  {!isManaged && (
                    <Input
                      className={`${styles.grow} ${styles.mono}`}
                      value={getField(volume, 'source') ?? ''}
                      onChange={e => updateField(volume.id, { source: e.target.value })}
                      placeholder={t('volumes.source')}
                      disabled={isSaving}
                    />
                  )}
                </div>

                <div className={styles.volumeRow}>
                  <Checkbox
                    label={t('volumes.readOnly')}
                    checked={getField(volume, 'readOnly')}
                    onChange={e => updateField(volume.id, { readOnly: e.target.checked })}
                    disabled={isSaving}
                  />
                  <Checkbox
                    label={t('volumes.backup')}
                    checked={getField(volume, 'backupEnabled')}
                    onChange={e => updateField(volume.id, { backupEnabled: e.target.checked })}
                    disabled={isSaving}
                  />
                  <Spacer expand direction="horizontal" />
                  {isManaged && (
                    <Button variant="ghost" size="sm" onClick={() => setFilesVolume(volume)}>
                      {t('volumes.manageFiles')}
                    </Button>
                  )}
                </div>
              </div>
            );
          })}
        </Stack>
      )}

      <Modal
        isOpen={isAddOpen}
        onClose={() => setIsAddOpen(false)}
        title={t('volumes.addTitle')}
        size="sm"
        error={createError ?? undefined}
        footer={
          <Row gap="2" justify="flex-end" full>
            <Button variant="ghost" onClick={() => setIsAddOpen(false)} disabled={isCreating}>
              {t('volumes.cancel')}
            </Button>
            <Button
              variant="primary"
              onClick={handleCreate}
              isLoading={isCreating}
              disabled={!canCreate}
              icon={<Plus size={14} />}
            >
              {t('volumes.create')}
            </Button>
          </Row>
        }
      >
        <Stack gap="3">
          <SelectInput
            label={t('volumes.type')}
            value={newVolume.type}
            onChange={v => setNewVolume(p => ({ ...p, type: v as VolumeType }))}
            options={typeOptions}
          />
          <Input
            label={t('volumes.name')}
            value={newVolume.name}
            onChange={e => setNewVolume(p => ({ ...p, name: e.target.value }))}
            placeholder="nginx-config"
            autoFocus
          />
          <Input
            label={t('volumes.target')}
            value={newVolume.target}
            onChange={e => setNewVolume(p => ({ ...p, target: e.target.value }))}
            placeholder="/etc/nginx"
          />
          {needsSource && (
            <Input
              label={t('volumes.source')}
              value={newVolume.source ?? ''}
              onChange={e => setNewVolume(p => ({ ...p, source: e.target.value }))}
              placeholder={newVolume.type === 'HostPath' ? '/srv/data' : 'my-volume'}
            />
          )}
          <Checkbox
            label={t('volumes.readOnly')}
            checked={newVolume.readOnly}
            onChange={e => setNewVolume(p => ({ ...p, readOnly: e.target.checked }))}
          />
          <Checkbox
            label={t('volumes.backupHint')}
            checked={newVolume.backupEnabled}
            onChange={e => setNewVolume(p => ({ ...p, backupEnabled: e.target.checked }))}
          />
        </Stack>
      </Modal>

      <Modal
        isOpen={!!deleteTarget}
        onClose={() => setDeleteTarget(null)}
        title={t('volumes.deleteTitle')}
        size="sm"
        footer={
          <Row gap="2" justify="flex-end" full>
            <Button variant="ghost" onClick={() => setDeleteTarget(null)} disabled={isDeleting}>
              {t('volumes.cancel')}
            </Button>
            <Button variant="danger" onClick={handleDelete} isLoading={isDeleting}>
              {t('volumes.delete')}
            </Button>
          </Row>
        }
      >
        <Label variant="secondary" size="sm">
          {t('volumes.deleteConfirm', { name: deleteTarget?.name })}
        </Label>
      </Modal>

      {filesVolume && (
        <ManagedVolumeFilesModal
          projectId={projectId}
          environmentId={environmentId}
          serviceId={serviceId}
          volume={filesVolume}
          isOpen={!!filesVolume}
          onClose={() => setFilesVolume(null)}
        />
      )}
    </div>
  );
}
