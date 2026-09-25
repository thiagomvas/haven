import { KeyRound, Pencil, Plus, Trash2 } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import { SecretsScope, SecretVariableDto } from '@/api/types';
import { Row, Spacer, Stack } from '@/components/layout';
import { Badge } from '@/components/ui/Badge';
import { Button } from '@/components/ui/Button';
import { ErrorAlert } from '@/components/ui/ErrorAlert';
import { Input } from '@/components/ui/Input';
import { Label } from '@/components/ui/Label';
import { Modal } from '@/components/ui/Modal';
import { Spinner } from '@/components/ui/Spinner';
import { usePermission } from '@/hooks/usePermission';
import { useCreateSecret, useDeleteSecret, useSecrets, useUpdateSecret } from '@/hooks/useSecrets';
import styles from '@/styles/components/secrets/SecretsSection.module.css';

interface SecretsSectionProps {
  parentType: SecretsScope['parentType'];
  projectId: string;
  environmentId?: string;
  serviceId?: string;
}

export function SecretsSection({
  parentType,
  projectId,
  environmentId,
  serviceId,
}: SecretsSectionProps) {
  const { t } = useTranslation(['secrets', 'common']);
  const canManage = usePermission('projects.manage_secrets');

  const scope: SecretsScope = { parentType, projectId, environmentId, serviceId };

  const { data, isLoading, error } = useSecrets(scope, { pageNumber: 1, pageSize: 100 });
  const createMutation = useCreateSecret(scope);
  const updateMutation = useUpdateSecret(scope);
  const deleteMutation = useDeleteSecret(scope);

  const [isAddOpen, setIsAddOpen] = useState(false);
  const [newKey, setNewKey] = useState('');
  const [newValue, setNewValue] = useState('');
  const [createError, setCreateError] = useState<string | null>(null);

  const [editTarget, setEditTarget] = useState<SecretVariableDto | null>(null);
  const [editKey, setEditKey] = useState('');
  const [editValue, setEditValue] = useState('');
  const [editError, setEditError] = useState<string | null>(null);

  const [deleteTarget, setDeleteTarget] = useState<SecretVariableDto | null>(null);
  const [deleteError, setDeleteError] = useState<string | null>(null);

  if (!canManage) return null;

  const secrets = data?.items ?? [];

  const openAdd = () => {
    setNewKey('');
    setNewValue('');
    setCreateError(null);
    setIsAddOpen(true);
  };

  const handleCreate = async () => {
    if (!newKey.trim() || !newValue.trim()) return;
    try {
      await createMutation.mutateAsync({ key: newKey.trim(), value: newValue });
      setIsAddOpen(false);
    } catch (err) {
      setCreateError(err instanceof Error ? err.message : t('errors.createFailed'));
    }
  };

  const openEdit = (secret: SecretVariableDto) => {
    setEditTarget(secret);
    setEditKey(secret.key);
    setEditValue('');
    setEditError(null);
  };

  const handleEditSave = async () => {
    if (!editTarget) return;
    if (!editKey.trim()) {
      setEditError(t('errors.keyRequired'));
      return;
    }
    try {
      await updateMutation.mutateAsync({
        id: editTarget.id,
        data: {
          key: editKey.trim() !== editTarget.key ? editKey.trim() : undefined,
          value: editValue.trim() ? editValue : undefined,
        },
      });
      setEditTarget(null);
    } catch (err) {
      setEditError(err instanceof Error ? err.message : t('errors.updateFailed'));
    }
  };

  const handleDelete = async () => {
    if (!deleteTarget) return;
    try {
      setDeleteError(null);
      await deleteMutation.mutateAsync(deleteTarget.id);
      setDeleteTarget(null);
    } catch (err) {
      setDeleteError(err instanceof Error ? err.message : t('errors.deleteFailed'));
    }
  };

  if (isLoading) {
    return (
      <div className={styles.spinnerWrap}>
        <Spinner />
      </div>
    );
  }

  return (
    <div className={styles.container}>
      {error && (
        <ErrorAlert
          message={error instanceof Error ? error.message : t('errors.loadFailed')}
          variant="block"
        />
      )}

      <Row align="center" gap="2">
        <Label variant="primary" size="md" weight="semibold">
          {t('title')}
        </Label>
        {secrets.length > 0 && <Badge>{secrets.length}</Badge>}
        <Spacer expand direction="horizontal" />
        <Button variant="secondary" size="sm" icon={<Plus size={14} />} onClick={openAdd}>
          {t('add')}
        </Button>
      </Row>

      {secrets.length === 0 ? (
        <div className={styles.emptyState}>
          <KeyRound size={28} className={styles.emptyIcon} />
          <Label variant="secondary" size="sm">
            {t('empty')}
          </Label>
          <Button variant="secondary" size="sm" icon={<Plus size={14} />} onClick={openAdd}>
            {t('addFirst')}
          </Button>
        </div>
      ) : (
        <Stack gap="1">
          <div className={styles.tableHeader}>
            <Label variant="muted" size="xs" weight="semibold">
              {t('table.key')}
            </Label>
            <Label variant="muted" size="xs" weight="semibold">
              {t('table.value')}
            </Label>
            <span />
          </div>

          {secrets.map(secret => (
            <div key={secret.id} className={styles.row}>
              <Label size="sm" weight="medium" className={styles.keyCell}>
                {secret.key}
              </Label>
              <Badge variant={secret.hasValue ? 'success' : 'default'}>
                {secret.hasValue ? '••••••••' : t('table.notSet')}
              </Badge>
              <div className={styles.rowActions}>
                <button
                  type="button"
                  className={styles.iconBtn}
                  onClick={() => openEdit(secret)}
                  title={t('common:actions.edit')}
                  aria-label={t('common:actions.edit')}
                >
                  <Pencil size={14} />
                </button>
                <button
                  type="button"
                  className={`${styles.iconBtn} ${styles.danger}`}
                  onClick={() => setDeleteTarget(secret)}
                  title={t('common:actions.delete')}
                  aria-label={t('common:actions.delete')}
                >
                  <Trash2 size={14} />
                </button>
              </div>
            </div>
          ))}
        </Stack>
      )}

      <Modal
        isOpen={isAddOpen}
        onClose={() => setIsAddOpen(false)}
        title={t('add')}
        size="sm"
        error={createError ?? undefined}
        closeOnEscape={!createMutation.isPending}
        closeOnBackdropClick={!createMutation.isPending}
        footer={
          <Row gap="2" justify="flex-end" full>
            <Button
              variant="ghost"
              onClick={() => setIsAddOpen(false)}
              disabled={createMutation.isPending}
            >
              {t('common:actions.cancel')}
            </Button>
            <Button
              variant="primary"
              onClick={handleCreate}
              isLoading={createMutation.isPending}
              disabled={!newKey.trim() || !newValue.trim()}
            >
              {t('common:actions.create')}
            </Button>
          </Row>
        }
      >
        <Stack gap="3">
          <Input
            label={t('form.key')}
            value={newKey}
            onChange={e => setNewKey(e.target.value)}
            placeholder="API_KEY"
            autoFocus
            disabled={createMutation.isPending}
          />
          <Input
            label={t('form.value')}
            type="password"
            value={newValue}
            onChange={e => setNewValue(e.target.value)}
            placeholder={t('form.valuePlaceholder')}
            disabled={createMutation.isPending}
          />
        </Stack>
      </Modal>

      <Modal
        isOpen={!!editTarget}
        onClose={() => setEditTarget(null)}
        title={t('edit.title')}
        description={t('edit.description')}
        size="sm"
        error={editError ?? undefined}
        closeOnEscape={!updateMutation.isPending}
        closeOnBackdropClick={!updateMutation.isPending}
        footer={
          <Row gap="2" justify="flex-end" full>
            <Button
              variant="ghost"
              onClick={() => setEditTarget(null)}
              disabled={updateMutation.isPending}
            >
              {t('common:actions.cancel')}
            </Button>
            <Button
              variant="primary"
              onClick={handleEditSave}
              isLoading={updateMutation.isPending}
              disabled={!editKey.trim()}
            >
              {t('common:actions.save')}
            </Button>
          </Row>
        }
      >
        <Stack gap="3">
          <Input
            label={t('form.key')}
            value={editKey}
            onChange={e => setEditKey(e.target.value)}
            disabled={updateMutation.isPending}
          />
          <Input
            label={t('form.value')}
            type="password"
            value={editValue}
            onChange={e => setEditValue(e.target.value)}
            placeholder={t('edit.valuePlaceholder')}
            disabled={updateMutation.isPending}
          />
        </Stack>
      </Modal>

      <Modal
        isOpen={!!deleteTarget}
        onClose={() => setDeleteTarget(null)}
        title={t('delete.title')}
        size="sm"
        error={deleteError ?? undefined}
        footer={
          <Row gap="2" justify="flex-end" full>
            <Button
              variant="ghost"
              onClick={() => setDeleteTarget(null)}
              disabled={deleteMutation.isPending}
            >
              {t('common:actions.cancel')}
            </Button>
            <Button variant="danger" onClick={handleDelete} isLoading={deleteMutation.isPending}>
              {t('common:actions.delete')}
            </Button>
          </Row>
        }
      >
        <Label variant="secondary" size="sm">
          {t('delete.message', { key: deleteTarget?.key })}
        </Label>
      </Modal>
    </div>
  );
}
