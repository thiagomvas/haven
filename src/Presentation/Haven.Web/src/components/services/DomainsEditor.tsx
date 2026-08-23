import { Globe, Plus, Trash2 } from 'lucide-react';
import { useCallback, useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';

import styles from '@/styles/components/services/DomainsEditor.module.css';

import { registryDomainsApi } from '../../api/registryDomains';
import { AddDomainInput, UpdateDomainInput } from '../../api/types/registryDomain.types';
import { ServiceRegistryDomainDto } from '../../api/types/service.types';
import { Row, Spacer, Stack } from '../layout';
import { Badge } from '../ui/Badge';
import { Button } from '../ui/Button';
import { Checkbox } from '../ui/Checkbox';
import { ErrorAlert } from '../ui/ErrorAlert';
import { Input } from '../ui/Input';
import { Label } from '../ui/Label';
import { Modal } from '../ui/Modal';
import { Spinner } from '../ui/Spinner';

interface DomainsEditorProps {
  serviceId: string;
}

const EMPTY_NEW_DOMAIN: AddDomainInput = {
  hostname: '',
  containerPort: 80,
  enableTls: false,
};

export function DomainsEditor({ serviceId }: DomainsEditorProps) {
  const { t } = useTranslation('services');

  const [domains, setDomains] = useState<ServiceRegistryDomainDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [edits, setEdits] = useState<Record<string, UpdateDomainInput>>({});
  const [isSaving, setIsSaving] = useState(false);

  const [isAddOpen, setIsAddOpen] = useState(false);
  const [newDomain, setNewDomain] = useState<AddDomainInput>(EMPTY_NEW_DOMAIN);
  const [isCreating, setIsCreating] = useState(false);
  const [createError, setCreateError] = useState<string | null>(null);

  const [deleteTarget, setDeleteTarget] = useState<ServiceRegistryDomainDto | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);
  const [deleteError, setDeleteError] = useState<string | null>(null);

  const loadDomains = useCallback(async () => {
    try {
      setLoading(true);
      const entry = await registryDomainsApi.getEntry(serviceId);
      setDomains(entry?.domains ?? []);
    } catch (err) {
      setError(err instanceof Error ? err.message : t('error'));
    } finally {
      setLoading(false);
    }
  }, [serviceId, t]);

  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect -- initial data fetch on mount
    void loadDomains();
  }, [loadDomains]);

  const updateField = (id: string, updates: UpdateDomainInput) => {
    setEdits(prev => ({ ...prev, [id]: { ...prev[id], ...updates } }));
  };

  const getField = <K extends keyof ServiceRegistryDomainDto>(
    domain: ServiceRegistryDomainDto,
    field: K
  ): ServiceRegistryDomainDto[K] =>
    ((edits[domain.id] as Record<string, unknown> | undefined)?.[field as string] ??
      domain[field]) as ServiceRegistryDomainDto[K];

  const hasChanges = Object.keys(edits).length > 0;

  const saveChanges = async () => {
    if (!hasChanges) return;
    try {
      setIsSaving(true);
      setError(null);
      await Promise.all(
        Object.entries(edits).map(([id, data]) => registryDomainsApi.update(serviceId, id, data))
      );
      setEdits({});
      await loadDomains();
    } catch (err) {
      setError(err instanceof Error ? err.message : t('error'));
    } finally {
      setIsSaving(false);
    }
  };

  const canCreate = !!newDomain.hostname.trim() && newDomain.containerPort > 0;

  const handleCreate = async () => {
    if (!canCreate) return;
    try {
      setIsCreating(true);
      setCreateError(null);
      await registryDomainsApi.add(serviceId, {
        hostname: newDomain.hostname.trim(),
        containerPort: newDomain.containerPort,
        enableTls: newDomain.enableTls,
      });
      setNewDomain(EMPTY_NEW_DOMAIN);
      setIsAddOpen(false);
      await loadDomains();
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
      setDeleteError(null);
      await registryDomainsApi.delete(serviceId, deleteTarget.id);
      setDeleteTarget(null);
      await loadDomains();
    } catch (err) {
      setDeleteError(err instanceof Error ? err.message : t('error'));
    } finally {
      setIsDeleting(false);
    }
  };

  const openAddModal = () => {
    setNewDomain(EMPTY_NEW_DOMAIN);
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
          {t('domains.title')}
        </Label>
        {domains.length > 0 && <Badge>{domains.length}</Badge>}
        <Spacer expand direction="horizontal" />
        {hasChanges && (
          <>
            <Button variant="ghost" size="sm" onClick={() => setEdits({})} disabled={isSaving}>
              {t('domains.discard')}
            </Button>
            <Button variant="primary" size="sm" onClick={saveChanges} isLoading={isSaving}>
              {t('domains.saveChanges')}
            </Button>
          </>
        )}
        <Button variant="secondary" size="sm" icon={<Plus size={14} />} onClick={openAddModal}>
          {t('domains.add')}
        </Button>
      </Row>

      <Label variant="secondary" size="sm">
        {t('domains.description')}
      </Label>

      {domains.length === 0 ? (
        <div className={styles.emptyState}>
          <Globe size={28} className={styles.emptyIcon} />
          <Label variant="secondary" size="sm">
            {t('domains.empty')}
          </Label>
          <Button variant="secondary" size="sm" icon={<Plus size={14} />} onClick={openAddModal}>
            {t('domains.addFirst')}
          </Button>
        </div>
      ) : (
        <Stack gap="2">
          {domains.map(domain => {
            const isDirty = !!edits[domain.id];
            return (
              <div
                key={domain.id}
                className={`${styles.domainCard} ${isDirty ? styles.domainCardDirty : ''}`}
              >
                <div className={styles.domainRow}>
                  <Input
                    className={`${styles.grow} ${styles.mono}`}
                    value={getField(domain, 'hostname')}
                    onChange={e => updateField(domain.id, { hostname: e.target.value })}
                    placeholder={t('domains.hostname')}
                    disabled={isSaving}
                  />
                  <Input
                    className={styles.portInput}
                    type="number"
                    min={1}
                    max={65535}
                    value={getField(domain, 'containerPort')}
                    onChange={e =>
                      updateField(domain.id, { containerPort: Number(e.target.value) })
                    }
                    placeholder={t('domains.containerPort')}
                    disabled={isSaving}
                  />
                  <button
                    className={styles.deleteBtn}
                    onClick={() => setDeleteTarget(domain)}
                    disabled={isSaving}
                    title={t('domains.delete')}
                  >
                    <Trash2 size={14} />
                  </button>
                </div>
                <Checkbox
                  label={t('domains.enableTls')}
                  description={t('domains.enableTlsHelp')}
                  checked={getField(domain, 'enableTls')}
                  onChange={e => updateField(domain.id, { enableTls: e.target.checked })}
                  disabled={isSaving}
                />
              </div>
            );
          })}
        </Stack>
      )}

      <Modal
        isOpen={isAddOpen}
        onClose={() => setIsAddOpen(false)}
        title={t('domains.addTitle')}
        size="sm"
        error={createError ?? undefined}
        footer={
          <Row gap="2" justify="flex-end" full>
            <Button variant="ghost" onClick={() => setIsAddOpen(false)} disabled={isCreating}>
              {t('domains.cancel')}
            </Button>
            <Button
              variant="primary"
              onClick={handleCreate}
              isLoading={isCreating}
              disabled={!canCreate}
              icon={<Plus size={14} />}
            >
              {t('domains.create')}
            </Button>
          </Row>
        }
      >
        <Stack gap="3">
          <Input
            label={t('domains.hostname')}
            value={newDomain.hostname}
            onChange={e => setNewDomain(p => ({ ...p, hostname: e.target.value }))}
            placeholder="app.example.com"
            autoFocus
          />
          <Input
            label={t('domains.containerPort')}
            type="number"
            min={1}
            max={65535}
            value={newDomain.containerPort}
            onChange={e => setNewDomain(p => ({ ...p, containerPort: Number(e.target.value) }))}
            placeholder="8080"
          />
          <Checkbox
            label={t('domains.enableTls')}
            description={t('domains.enableTlsHelp')}
            checked={!!newDomain.enableTls}
            onChange={e => setNewDomain(p => ({ ...p, enableTls: e.target.checked }))}
          />
        </Stack>
      </Modal>

      <Modal
        isOpen={!!deleteTarget}
        onClose={() => setDeleteTarget(null)}
        title={t('domains.deleteTitle')}
        size="sm"
        error={deleteError ?? undefined}
        footer={
          <Row gap="2" justify="flex-end" full>
            <Button variant="ghost" onClick={() => setDeleteTarget(null)} disabled={isDeleting}>
              {t('domains.cancel')}
            </Button>
            <Button variant="danger" onClick={handleDelete} isLoading={isDeleting}>
              {t('domains.delete')}
            </Button>
          </Row>
        }
      >
        <Label variant="secondary" size="sm">
          {t('domains.deleteConfirm', { hostname: deleteTarget?.hostname })}
        </Label>
      </Modal>
    </div>
  );
}
