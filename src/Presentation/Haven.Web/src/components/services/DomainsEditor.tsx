import { AlertTriangle, Globe, Plus, ShieldCheck, Trash2 } from 'lucide-react';
import { useCallback, useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';

import styles from '@/styles/components/services/DomainsEditor.module.css';

import { getDomainCertificateStatus, registryDomainsApi } from '../../api/registryDomains';
import { sslCertificatesApi } from '../../api/sslCertificates';
import {
  AddDomainInput,
  DomainCertificateStatusDto,
  TlsMode,
  UpdateDomainInput,
} from '../../api/types/registryDomain.types';
import { ServiceRegistryDomainDto } from '../../api/types/service.types';
import { SslCertificateDto } from '../../api/types/sslCertificate.types';
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

interface DomainsEditorProps {
  serviceId: string;
}

const EMPTY_NEW_DOMAIN: AddDomainInput = {
  hostname: '',
  containerPort: 80,
  tlsMode: 'None',
  internalBasePath: '',
};

export function DomainsEditor({ serviceId }: DomainsEditorProps) {
  const { t } = useTranslation('services');
  const { data: sidecars } = useSidecars();
  const traefikSidecar = sidecars?.find(s => s.kind === 'Traefik');
  const acmeConfigured = traefikSidecar?.isAcmeConfigured ?? true;

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

  const [certTarget, setCertTarget] = useState<ServiceRegistryDomainDto | null>(null);
  const [selectedCertificateId, setSelectedCertificateId] = useState('');
  const [certError, setCertError] = useState<string | null>(null);
  const [certWarnings, setCertWarnings] = useState<string[]>([]);
  const [isSavingCert, setIsSavingCert] = useState(false);
  const [certificateLibrary, setCertificateLibrary] = useState<SslCertificateDto[]>([]);

  const [statusByDomain, setStatusByDomain] = useState<Record<string, DomainCertificateStatusDto>>(
    {}
  );
  const [statusLoading, setStatusLoading] = useState<string | null>(null);

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
        tlsMode: newDomain.tlsMode,
        internalBasePath: newDomain.internalBasePath?.trim() || undefined,
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

  const openCertModal = async (domain: ServiceRegistryDomainDto) => {
    setCertTarget(domain);
    setSelectedCertificateId(domain.certificateId ?? '');
    setCertError(null);
    setCertWarnings([]);
    try {
      const list = await sslCertificatesApi.list();
      setCertificateLibrary(list);
    } catch (err) {
      setCertError(err instanceof Error ? err.message : t('error'));
    }
  };

  const handleAttachCertificate = async () => {
    if (!certTarget || !selectedCertificateId) return;
    try {
      setIsSavingCert(true);
      setCertError(null);
      const result = await registryDomainsApi.attachCertificate(serviceId, certTarget.id, {
        certificateId: selectedCertificateId,
      });
      setCertWarnings(result.warnings ?? []);
      await loadDomains();
      setStatusByDomain(prev => {
        const next = { ...prev };
        delete next[certTarget.id];
        return next;
      });
    } catch (err) {
      setCertError(err instanceof Error ? err.message : t('error'));
    } finally {
      setIsSavingCert(false);
    }
  };

  const handleDetachCertificate = async () => {
    if (!certTarget) return;
    try {
      setIsSavingCert(true);
      setCertError(null);
      await registryDomainsApi.detachCertificate(serviceId, certTarget.id);
      setCertTarget(null);
      await loadDomains();
    } catch (err) {
      setCertError(err instanceof Error ? err.message : t('error'));
    } finally {
      setIsSavingCert(false);
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
            const effectiveTlsMode = getField(domain, 'tlsMode');
            const status = statusByDomain[domain.id];
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

                <Input
                  label={t('domains.internalBasePath')}
                  value={getField(domain, 'internalBasePath') ?? ''}
                  onChange={e => updateField(domain.id, { internalBasePath: e.target.value })}
                  placeholder="/api/v1"
                  disabled={isSaving}
                />
                <Label variant="secondary" size="sm">
                  {t('domains.internalBasePathHelp')}
                </Label>

                <SelectInput
                  label={t('domains.tlsMode')}
                  options={tlsModeOptions}
                  value={effectiveTlsMode}
                  onChange={value => updateField(domain.id, { tlsMode: value as TlsMode })}
                  disabled={isSaving}
                />

                {effectiveTlsMode === 'Acme' && !acmeConfigured && (
                  <Row align="center" gap="1" className={styles.warningRow}>
                    <AlertTriangle size={14} className={styles.warningIcon} />
                    <Label variant="warning" size="sm">
                      {t('domains.acmeNotConfiguredWarning')}
                    </Label>
                  </Row>
                )}

                {domain.tlsMode === 'Custom' && (
                  <Row align="center" gap="2" className={styles.certRow}>
                    {!domain.hasCertificate && (
                      <Row align="center" gap="1" className={styles.warningRow}>
                        <AlertTriangle size={14} className={styles.warningIcon} />
                        <Label variant="warning" size="sm">
                          {t('domains.customModeNoCertWarning')}
                        </Label>
                      </Row>
                    )}
                    {domain.hasCertificate && (
                      <Row align="center" gap="1">
                        <ShieldCheck size={14} />
                        {domain.certificateName && (
                          <Label variant="secondary" size="sm" weight="semibold">
                            {domain.certificateName}
                          </Label>
                        )}
                        {status ? (
                          <Label variant="secondary" size="sm">
                            {status.isExpired
                              ? t('domains.statusExpired')
                              : t('domains.statusExpiresIn', { days: status.daysUntilExpiry })}
                            {status.hostnameMismatch && ` · ${t('domains.statusHostnameMismatch')}`}
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
                    <Button variant="secondary" size="sm" onClick={() => openCertModal(domain)}>
                      {t('domains.manageCertificate')}
                    </Button>
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
          <Input
            label={t('domains.internalBasePath')}
            value={newDomain.internalBasePath ?? ''}
            onChange={e => setNewDomain(p => ({ ...p, internalBasePath: e.target.value }))}
            placeholder="/api/v1"
          />
          <Label variant="secondary" size="sm">
            {t('domains.internalBasePathHelp')}
          </Label>
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

      <Modal
        isOpen={!!certTarget}
        onClose={() => setCertTarget(null)}
        title={t('domains.certificateTitle', { hostname: certTarget?.hostname })}
        size="md"
        error={certError ?? undefined}
        footer={
          <Row gap="2" justify="flex-end" full>
            {certTarget?.hasCertificate && (
              <Button variant="danger" onClick={handleDetachCertificate} isLoading={isSavingCert}>
                {t('domains.detachCertificate')}
              </Button>
            )}
            <Spacer expand direction="horizontal" />
            <Button variant="ghost" onClick={() => setCertTarget(null)} disabled={isSavingCert}>
              {t('domains.cancel')}
            </Button>
            <Button
              variant="primary"
              onClick={handleAttachCertificate}
              isLoading={isSavingCert}
              disabled={
                !selectedCertificateId || selectedCertificateId === certTarget?.certificateId
              }
            >
              {t('domains.attachCertificate')}
            </Button>
          </Row>
        }
      >
        <Stack gap="3">
          {certWarnings.map(warning => (
            <Row key={warning} align="center" gap="1" className={styles.warningRow}>
              <AlertTriangle size={14} className={styles.warningIcon} />
              <Label variant="warning" size="sm">
                {warning}
              </Label>
            </Row>
          ))}
          {certificateLibrary.length === 0 ? (
            <Label variant="secondary" size="sm">
              {t('domains.noCertificatesInLibrary')}
            </Label>
          ) : (
            <SelectInput
              label={t('domains.certificate')}
              options={certificateLibrary.map(cert => ({ value: cert.id, label: cert.name }))}
              value={selectedCertificateId}
              onChange={setSelectedCertificateId}
              placeholder={t('domains.certificatePlaceholder')}
            />
          )}
          <Label variant="secondary" size="sm">
            <Link to="/settings?tab=ssl-certificates">{t('domains.manageCertificatesLink')}</Link>
          </Label>
        </Stack>
      </Modal>
    </div>
  );
}
