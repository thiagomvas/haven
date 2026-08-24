import { AlertTriangle, Globe, Plus, ShieldCheck, Trash2 } from 'lucide-react';
import { useCallback, useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';

import styles from '@/styles/components/services/DomainsEditor.module.css';

import { getDomainCertificateStatus } from '../../api/registryDomains';
import { sidecarDomainsApi } from '../../api/sidecarDomains';
import {
  AddDomainInput,
  DomainCertificateStatusDto,
  TlsMode,
} from '../../api/types/registryDomain.types';
import { ServiceRegistryDomainDto } from '../../api/types/service.types';
import { useSidecars } from '../../hooks/useSidecars';
import { Row, Spacer, Stack } from '../layout';
import { Badge } from '../ui/Badge';
import { Button } from '../ui/Button';
import { ErrorAlert } from '../ui/ErrorAlert';
import { Input } from '../ui/Input';
import { Label } from '../ui/Label';
import { Modal } from '../ui/Modal';
import { SelectInput } from '../ui/SelectInput';
import { Spinner } from '../ui/Spinner';

interface DashboardDomainEditorProps {
  sidecarId: string;
  disabled?: boolean;
}

// Traefik's dashboard is routed to its built-in api@internal service, not to a container port -
// this is only sent because AddDomainCommand requires a port in range; it's never read for
// dashboard domains (see DockerUtils.BuildTraefikDashboardLabels).
const UNUSED_CONTAINER_PORT = 8080;

const EMPTY_NEW_DOMAIN: AddDomainInput = {
  hostname: '',
  containerPort: UNUSED_CONTAINER_PORT,
  tlsMode: 'None',
};

export function DashboardDomainEditor({ sidecarId, disabled }: DashboardDomainEditorProps) {
  const { t } = useTranslation('services');
  const { data: sidecars } = useSidecars();
  const traefikSidecar = sidecars?.find(s => s.kind === 'Traefik');
  const acmeConfigured = traefikSidecar?.isAcmeConfigured ?? true;

  const [domains, setDomains] = useState<ServiceRegistryDomainDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [isAddOpen, setIsAddOpen] = useState(false);
  const [newDomain, setNewDomain] = useState<AddDomainInput>(EMPTY_NEW_DOMAIN);
  const [isCreating, setIsCreating] = useState(false);
  const [createError, setCreateError] = useState<string | null>(null);

  const [deleteTarget, setDeleteTarget] = useState<ServiceRegistryDomainDto | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);
  const [deleteError, setDeleteError] = useState<string | null>(null);

  const [tlsSaving, setTlsSaving] = useState<string | null>(null);
  const [statusByDomain, setStatusByDomain] = useState<Record<string, DomainCertificateStatusDto>>(
    {}
  );
  const [statusLoading, setStatusLoading] = useState<string | null>(null);

  const loadDomains = useCallback(async () => {
    try {
      setLoading(true);
      const list = await sidecarDomainsApi.list(sidecarId);
      setDomains(list);
    } catch (err) {
      setError(err instanceof Error ? err.message : t('error'));
    } finally {
      setLoading(false);
    }
  }, [sidecarId, t]);

  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect -- initial data fetch on mount
    void loadDomains();
  }, [loadDomains]);

  const canCreate = !!newDomain.hostname.trim();

  const handleCreate = async () => {
    if (!canCreate) return;
    try {
      setIsCreating(true);
      setCreateError(null);
      await sidecarDomainsApi.add(sidecarId, {
        hostname: newDomain.hostname.trim(),
        containerPort: UNUSED_CONTAINER_PORT,
        tlsMode: newDomain.tlsMode,
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
      await sidecarDomainsApi.delete(sidecarId, deleteTarget.id);
      setDeleteTarget(null);
      await loadDomains();
    } catch (err) {
      setDeleteError(err instanceof Error ? err.message : t('error'));
    } finally {
      setIsDeleting(false);
    }
  };

  const handleTlsModeChange = async (domain: ServiceRegistryDomainDto, tlsMode: TlsMode) => {
    try {
      setTlsSaving(domain.id);
      setError(null);
      await sidecarDomainsApi.update(sidecarId, domain.id, { tlsMode });
      await loadDomains();
    } catch (err) {
      setError(err instanceof Error ? err.message : t('error'));
    } finally {
      setTlsSaving(null);
    }
  };

  const checkStatus = async (domain: ServiceRegistryDomainDto) => {
    try {
      setStatusLoading(domain.id);
      const status = await getDomainCertificateStatus(domain.id);
      setStatusByDomain(prev => ({ ...prev, [domain.id]: status }));
    } catch {
      // Best-effort - leave any previous status in place.
    } finally {
      setStatusLoading(null);
    }
  };

  const openAddModal = () => {
    setNewDomain(EMPTY_NEW_DOMAIN);
    setCreateError(null);
    setIsAddOpen(true);
  };

  const tlsModeOptions = [
    { value: 'None', label: t('domains.tlsModeNone') },
    { value: 'Acme', label: t('domains.tlsModeAcme') },
    { value: 'Custom', label: t('domains.tlsModeCustom') },
  ];

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
        <Button
          variant="secondary"
          size="sm"
          icon={<Plus size={14} />}
          onClick={openAddModal}
          disabled={disabled}
        >
          {t('domains.add')}
        </Button>
      </Row>

      {domains.length === 0 ? (
        <div className={styles.emptyState}>
          <Globe size={28} className={styles.emptyIcon} />
          <Label variant="secondary" size="sm">
            {t('domains.empty')}
          </Label>
          <Button
            variant="secondary"
            size="sm"
            icon={<Plus size={14} />}
            onClick={openAddModal}
            disabled={disabled}
          >
            {t('domains.addFirst')}
          </Button>
        </div>
      ) : (
        <Stack gap="2">
          {domains.map(domain => {
            const status = statusByDomain[domain.id];
            return (
              <div key={domain.id} className={styles.domainCard}>
                <div className={styles.domainRow}>
                  <Label variant="primary" size="sm" className={styles.mono}>
                    {domain.hostname}
                  </Label>
                  <Spacer expand direction="horizontal" />
                  <button
                    className={styles.deleteBtn}
                    onClick={() => setDeleteTarget(domain)}
                    disabled={disabled}
                    title={t('domains.delete')}
                  >
                    <Trash2 size={14} />
                  </button>
                </div>

                <SelectInput
                  label={t('domains.tlsMode')}
                  options={tlsModeOptions}
                  value={domain.tlsMode}
                  onChange={value => handleTlsModeChange(domain, value as TlsMode)}
                  disabled={disabled || tlsSaving === domain.id}
                />

                {domain.tlsMode === 'Acme' && !acmeConfigured && (
                  <Row align="center" gap="1" className={styles.warningRow}>
                    <AlertTriangle size={14} className={styles.warningIcon} />
                    <Label variant="warning" size="sm">
                      {t('domains.acmeNotConfiguredWarning')}
                    </Label>
                  </Row>
                )}

                {domain.tlsMode === 'Acme' && acmeConfigured && (
                  <Row align="center" gap="1">
                    <ShieldCheck size={14} />
                    {status ? (
                      <Label variant="secondary" size="sm">
                        {status.traefikReachable
                          ? (status.routerStatus ?? t('domains.statusExpiresIn', { days: '?' }))
                          : t('domains.checkStatus')}
                      </Label>
                    ) : (
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => checkStatus(domain)}
                        isLoading={statusLoading === domain.id}
                      >
                        {t('domains.checkStatus')}
                      </Button>
                    )}
                  </Row>
                )}
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
            placeholder="traefik.example.com"
            autoFocus
          />
          <SelectInput
            label={t('domains.tlsMode')}
            options={tlsModeOptions}
            value={newDomain.tlsMode ?? 'None'}
            onChange={value => setNewDomain(p => ({ ...p, tlsMode: value as TlsMode }))}
          />
          {newDomain.tlsMode === 'Acme' && !acmeConfigured && (
            <Row align="center" gap="1" className={styles.warningRow}>
              <AlertTriangle size={14} className={styles.warningIcon} />
              <Label variant="warning" size="sm">
                {t('domains.acmeNotConfiguredWarning')}
              </Label>
            </Row>
          )}
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
