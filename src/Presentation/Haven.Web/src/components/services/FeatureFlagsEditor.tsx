import { Flag, Plus, Trash2 } from 'lucide-react';
import { useCallback, useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';

import { FeatureFlagDto } from '@/api/types/featureflags.types';
import { FeatureFlagValueType } from '@/api/types/featureflags.types';
import { FeatureFlagType } from '@/api/types/featureflags.types';
import styles from '@/styles/components/services/FeatureFlagsEditor.module.css';

import { featureFlagsApi } from '../../api/featureFlags';
import { Row, Spacer, Stack } from '../layout';
import { Badge } from '../ui/Badge';
import { Button } from '../ui/Button';
import { ErrorAlert } from '../ui/ErrorAlert';
import { Input } from '../ui/Input';
import { Label } from '../ui/Label';
import { Modal } from '../ui/Modal';
import { SelectInput } from '../ui/SelectInput';
import { Spinner } from '../ui/Spinner';

interface FeatureFlagsEditorProps {
  projectId: string;
  environmentId: string;
  serviceId: string;
}

type FlagEdits = Record<string, Partial<FeatureFlagDto>>;

interface NewFlagState {
  name: string;
  description: string;
  type: FeatureFlagType;
  key: string;
  value: string;
  valueType: FeatureFlagValueType;
}

const EMPTY_NEW_FLAG: NewFlagState = {
  name: '',
  description: '',
  type: 'EnvironmentVariable',
  key: '',
  value: '',
  valueType: 'String',
};

const VALUE_TYPE_OPTIONS = [
  { value: 'String', label: 'String' },
  { value: 'Bool', label: 'Boolean' },
  { value: 'Number', label: 'Number' },
];

const BoolSwitch = ({
  value,
  onChange,
  disabled,
}: {
  value: boolean;
  onChange: (v: boolean) => void;
  disabled?: boolean;
}) => (
  <button
    type="button"
    className={`${styles.switch} ${value ? styles.switchOn : ''}`}
    onClick={() => !disabled && onChange(!value)}
    disabled={disabled}
    title={value ? 'true' : 'false'}
  >
    <span className={styles.switchThumb} />
    <span className={styles.switchLabel}>{value ? 'true' : 'false'}</span>
  </button>
);

export function FeatureFlagsEditor({
  projectId,
  environmentId,
  serviceId,
}: FeatureFlagsEditorProps) {
  const { t } = useTranslation(['services', 'projects']);

  const [flags, setFlags] = useState<FeatureFlagDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [edits, setEdits] = useState<FlagEdits>({});
  const [isSaving, setIsSaving] = useState(false);

  const [isAddOpen, setIsAddOpen] = useState(false);
  const [newFlag, setNewFlag] = useState<NewFlagState>(EMPTY_NEW_FLAG);
  const [isCreating, setIsCreating] = useState(false);
  const [createError, setCreateError] = useState<string | null>(null);

  const [deleteTarget, setDeleteTarget] = useState<FeatureFlagDto | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);

  const loadFlags = useCallback(async () => {
    try {
      setLoading(true);
      const result = await featureFlagsApi.list(projectId, environmentId, serviceId);
      setFlags(result?.items ?? []);
    } catch (err) {
      setError(err instanceof Error ? err.message : t('services:error'));
    } finally {
      setLoading(false);
    }
  }, [projectId, environmentId, serviceId, t]);

  useEffect(() => {
    (async () => {
      await loadFlags();
    })();
  }, [loadFlags]);

  const updateFlagField = (flagId: string, updates: Partial<FeatureFlagDto>) => {
    setEdits(prev => ({ ...prev, [flagId]: { ...prev[flagId], ...updates } }));
  };

  const getFlagField = <K extends keyof FeatureFlagDto>(
    flag: FeatureFlagDto,
    field: K
  ): FeatureFlagDto[K] => (edits[flag.id]?.[field] ?? flag[field]) as FeatureFlagDto[K];

  const hasChanges = Object.keys(edits).length > 0;

  const saveChanges = async () => {
    if (!hasChanges) return;
    try {
      setIsSaving(true);
      const updates = Object.entries(edits).map(([flagId, data]) => ({ flagId, ...data }));
      await featureFlagsApi.batchUpdate(
        projectId,
        environmentId,
        serviceId,
        updates as Parameters<typeof featureFlagsApi.batchUpdate>[3]
      );
      setEdits({});
      await loadFlags();
    } catch (err) {
      setError(err instanceof Error ? err.message : t('services:error'));
    } finally {
      setIsSaving(false);
    }
  };

  const handleCreate = async () => {
    if (!newFlag.name.trim()) return;
    if (newFlag.valueType !== 'Bool' && !newFlag.value.trim()) return;
    try {
      setIsCreating(true);
      setCreateError(null);
      await featureFlagsApi.create(projectId, environmentId, serviceId, {
        name: newFlag.name.trim(),
        description: newFlag.description.trim() || undefined,
        type: newFlag.type,
        key: newFlag.key.trim() || undefined,
        value: newFlag.valueType === 'Bool' ? newFlag.value || 'false' : newFlag.value.trim(),
        valueType: newFlag.valueType,
      });
      setNewFlag(EMPTY_NEW_FLAG);
      setIsAddOpen(false);
      await loadFlags();
    } catch (err) {
      setCreateError(err instanceof Error ? err.message : t('services:error'));
    } finally {
      setIsCreating(false);
    }
  };

  const handleDelete = async () => {
    if (!deleteTarget) return;
    try {
      setIsDeleting(true);
      await featureFlagsApi.delete(projectId, environmentId, serviceId, deleteTarget.id);
      setDeleteTarget(null);
      await loadFlags();
    } catch (err) {
      setError(err instanceof Error ? err.message : t('services:error'));
    } finally {
      setIsDeleting(false);
    }
  };

  const openAddModal = () => {
    setNewFlag(EMPTY_NEW_FLAG);
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
          {t('services:featureFlags')}
        </Label>
        {flags.length > 0 && <Badge>{flags.length}</Badge>}
        <Spacer expand direction="horizontal" />
        {hasChanges && (
          <>
            <Button variant="ghost" size="sm" onClick={() => setEdits({})} disabled={isSaving}>
              Discard
            </Button>
            <Button variant="primary" size="sm" onClick={saveChanges} isLoading={isSaving}>
              Save Changes
            </Button>
          </>
        )}
        <Button variant="secondary" size="sm" icon={<Plus size={14} />} onClick={openAddModal}>
          Add Flag
        </Button>
      </Row>

      {flags.length === 0 ? (
        <div className={styles.emptyState}>
          <Flag size={28} className={styles.emptyIcon} />
          <Label variant="secondary" size="sm">
            {t('services:noFlags')}
          </Label>
          <Button variant="secondary" size="sm" icon={<Plus size={14} />} onClick={openAddModal}>
            Add your first flag
          </Button>
        </div>
      ) : (
        <Stack gap="1">
          <div className={styles.tableHeader}>
            <Label variant="muted" size="xs" weight="semibold">
              Name
            </Label>
            <Label variant="muted" size="xs" weight="semibold">
              Env Key
            </Label>
            <Label variant="muted" size="xs" weight="semibold">
              Type
            </Label>
            <Label variant="muted" size="xs" weight="semibold">
              Value
            </Label>
            <span />
          </div>

          {flags.map(flag => {
            const isDirty = !!edits[flag.id];
            const name = getFlagField(flag, 'name');
            const description = getFlagField(flag, 'description');
            const key = getFlagField(flag, 'key');
            const valueType = getFlagField(flag, 'valueType');
            const value = getFlagField(flag, 'value');

            return (
              <div
                key={flag.id}
                className={`${styles.flagCard} ${isDirty ? styles.flagCardDirty : ''}`}
              >
                <div className={styles.flagGrid}>
                  <div className={styles.nameCell}>
                    <input
                      className={styles.inlineInput}
                      value={name}
                      onChange={e => updateFlagField(flag.id, { name: e.target.value })}
                      placeholder="Name"
                      disabled={isSaving}
                    />
                    <input
                      className={`${styles.inlineInput} ${styles.inlineInputMuted}`}
                      value={description ?? ''}
                      onChange={e => updateFlagField(flag.id, { description: e.target.value })}
                      placeholder="Description (optional)"
                      disabled={isSaving}
                    />
                  </div>

                  <input
                    className={`${styles.inlineInput} ${styles.inlineInputMono}`}
                    value={key ?? ''}
                    onChange={e => updateFlagField(flag.id, { key: e.target.value })}
                    placeholder="ENV_VAR_KEY"
                    disabled={isSaving}
                  />

                  <select
                    className={styles.inlineSelect}
                    value={valueType}
                    onChange={e =>
                      updateFlagField(flag.id, {
                        valueType: e.target.value as FeatureFlagValueType,
                      })
                    }
                    disabled={isSaving}
                  >
                    <option value="String">String</option>
                    <option value="Bool">Boolean</option>
                    <option value="Number">Number</option>
                  </select>

                  <div className={styles.valueCell}>
                    {valueType === 'Bool' ? (
                      <BoolSwitch
                        value={value === 'true'}
                        onChange={v => updateFlagField(flag.id, { value: String(v) })}
                        disabled={isSaving}
                      />
                    ) : (
                      <input
                        type={valueType === 'Number' ? 'number' : 'text'}
                        className={styles.inlineInput}
                        value={value}
                        onChange={e => updateFlagField(flag.id, { value: e.target.value })}
                        placeholder="Value"
                        disabled={isSaving}
                      />
                    )}
                  </div>

                  <button
                    className={styles.deleteBtn}
                    onClick={() => setDeleteTarget(flag)}
                    disabled={isSaving}
                    title="Delete flag"
                  >
                    <Trash2 size={14} />
                  </button>
                </div>
              </div>
            );
          })}
        </Stack>
      )}

      <Modal
        isOpen={isAddOpen}
        onClose={() => setIsAddOpen(false)}
        title="Add Feature Flag"
        size="sm"
        error={createError ?? undefined}
        footer={
          <Row gap="2" justify="flex-end" full>
            <Button variant="ghost" onClick={() => setIsAddOpen(false)} disabled={isCreating}>
              Cancel
            </Button>
            <Button
              variant="primary"
              onClick={handleCreate}
              isLoading={isCreating}
              disabled={
                !newFlag.name.trim() || (newFlag.valueType !== 'Bool' && !newFlag.value.trim())
              }
              icon={<Plus size={14} />}
            >
              Create Flag
            </Button>
          </Row>
        }
      >
        <Stack gap="3">
          <Input
            label="Name *"
            value={newFlag.name}
            onChange={e => setNewFlag(p => ({ ...p, name: e.target.value }))}
            placeholder="My Feature Flag"
            autoFocus
          />
          <Input
            label="Description"
            value={newFlag.description}
            onChange={e => setNewFlag(p => ({ ...p, description: e.target.value }))}
            placeholder="What does this flag control?"
          />
          <Input
            label="Environment Variable Key"
            value={newFlag.key}
            onChange={e => setNewFlag(p => ({ ...p, key: e.target.value }))}
            placeholder="MY_FEATURE_ENABLED"
          />
          <SelectInput
            label="Value Type"
            value={newFlag.valueType}
            onChange={v =>
              setNewFlag(p => ({
                ...p,
                valueType: v as FeatureFlagValueType,
                value: v === 'Bool' ? 'false' : '',
              }))
            }
            options={VALUE_TYPE_OPTIONS}
          />
          {newFlag.valueType === 'Bool' ? (
            <div className={styles.boolFieldWrap}>
              <Label variant="secondary" size="sm" as="label">
                Value
              </Label>
              <BoolSwitch
                value={newFlag.value === 'true'}
                onChange={v => setNewFlag(p => ({ ...p, value: String(v) }))}
              />
            </div>
          ) : (
            <Input
              label="Value *"
              type={newFlag.valueType === 'Number' ? 'number' : 'text'}
              value={newFlag.value}
              onChange={e => setNewFlag(p => ({ ...p, value: e.target.value }))}
              placeholder={newFlag.valueType === 'Number' ? '42' : 'feature-value'}
            />
          )}
        </Stack>
      </Modal>

      <Modal
        isOpen={!!deleteTarget}
        onClose={() => setDeleteTarget(null)}
        title="Delete Feature Flag"
        size="sm"
        footer={
          <Row gap="2" justify="flex-end" full>
            <Button variant="ghost" onClick={() => setDeleteTarget(null)} disabled={isDeleting}>
              Cancel
            </Button>
            <Button variant="danger" onClick={handleDelete} isLoading={isDeleting}>
              Delete
            </Button>
          </Row>
        }
      >
        <Label variant="secondary" size="sm">
          Are you sure you want to delete <strong>{deleteTarget?.name}</strong>? This action cannot
          be undone.
        </Label>
      </Modal>
    </div>
  );
}
