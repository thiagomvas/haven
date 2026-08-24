import { AlertTriangle, Plus, ShieldCheck, Trash2 } from 'lucide-react';
import { useCallback, useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';

import { sslCertificatesApi } from '@/api/sslCertificates';
import { SslCertificateDto } from '@/api/types';
import { Row, Spacer, Stack } from '@/components/layout';
import { Badge } from '@/components/ui/Badge';
import { Button } from '@/components/ui/Button';
import { ErrorAlert } from '@/components/ui/ErrorAlert';
import { Input } from '@/components/ui/Input';
import { Label } from '@/components/ui/Label';
import { Modal } from '@/components/ui/Modal';
import { Spinner } from '@/components/ui/Spinner';
import { Textarea } from '@/components/ui/Textarea';
import styles from '@/styles/pages/settings/SslCertificatesPage.module.css';

function readFileAsText(file: File): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => resolve(String(reader.result ?? ''));
    reader.onerror = () => reject(reader.error);
    reader.readAsText(file);
  });
}

export function SslCertificatesPage() {
  const { t } = useTranslation('settings');

  const [certificates, setCertificates] = useState<SslCertificateDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [isAddOpen, setIsAddOpen] = useState(false);
  const [name, setName] = useState('');
  const [certPem, setCertPem] = useState('');
  const [keyPem, setKeyPem] = useState('');
  const [isSaving, setIsSaving] = useState(false);
  const [saveError, setSaveError] = useState<string | null>(null);
  const [warnings, setWarnings] = useState<string[]>([]);

  const [deleteTarget, setDeleteTarget] = useState<SslCertificateDto | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);
  const [deleteError, setDeleteError] = useState<string | null>(null);

  const load = useCallback(async () => {
    try {
      setLoading(true);
      const list = await sslCertificatesApi.list();
      setCertificates(list);
    } catch (err) {
      setError(err instanceof Error ? err.message : t('sslCertificates.error'));
    } finally {
      setLoading(false);
    }
  }, [t]);

  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect -- initial data fetch on mount
    void load();
  }, [load]);

  const openAddModal = () => {
    setName('');
    setCertPem('');
    setKeyPem('');
    setSaveError(null);
    setWarnings([]);
    setIsAddOpen(true);
  };

  const canSave = !!name.trim() && !!certPem.trim() && !!keyPem.trim();

  const handleUpload = async () => {
    if (!canSave) return;
    try {
      setIsSaving(true);
      setSaveError(null);
      const result = await sslCertificatesApi.upload({
        name: name.trim(),
        certificatePem: certPem,
        privateKeyPem: keyPem,
      });
      if (result.warnings.length > 0) {
        setWarnings(result.warnings);
        return;
      }
      setIsAddOpen(false);
      await load();
    } catch (err) {
      setSaveError(err instanceof Error ? err.message : t('sslCertificates.error'));
    } finally {
      setIsSaving(false);
    }
  };

  const handleDelete = async () => {
    if (!deleteTarget) return;
    try {
      setIsDeleting(true);
      setDeleteError(null);
      await sslCertificatesApi.delete(deleteTarget.id);
      setDeleteTarget(null);
      await load();
    } catch (err) {
      setDeleteError(err instanceof Error ? err.message : t('sslCertificates.error'));
    } finally {
      setIsDeleting(false);
    }
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
          {t('sslCertificates.title')}
        </Label>
        {certificates.length > 0 && <Badge>{certificates.length}</Badge>}
        <Spacer expand direction="horizontal" />
        <Button variant="secondary" size="sm" icon={<Plus size={14} />} onClick={openAddModal}>
          {t('sslCertificates.add')}
        </Button>
      </Row>

      <Label variant="secondary" size="sm">
        {t('sslCertificates.description')}
      </Label>

      {certificates.length === 0 ? (
        <div className={styles.emptyState}>
          <ShieldCheck size={28} className={styles.emptyIcon} />
          <Label variant="secondary" size="sm">
            {t('sslCertificates.empty')}
          </Label>
          <Button variant="secondary" size="sm" icon={<Plus size={14} />} onClick={openAddModal}>
            {t('sslCertificates.addFirst')}
          </Button>
        </div>
      ) : (
        <Stack gap="2">
          {certificates.map(cert => (
            <div key={cert.id} className={styles.certCard}>
              <div className={styles.certRow}>
                <ShieldCheck size={14} />
                <Label variant="primary" size="sm" weight="semibold" className={styles.grow}>
                  {cert.name}
                </Label>
                {cert.attachedDomainCount > 0 && (
                  <Badge>
                    {t('sslCertificates.attachedCount', { count: cert.attachedDomainCount })}
                  </Badge>
                )}
                <button
                  className={styles.deleteBtn}
                  onClick={() => setDeleteTarget(cert)}
                  title={t('sslCertificates.delete')}
                >
                  <Trash2 size={14} />
                </button>
              </div>
              <Label variant="secondary" size="sm" className={styles.mono}>
                {cert.subjectCommonName ?? t('sslCertificates.unknownSubject')}
              </Label>
              <Label variant={cert.isExpired ? 'warning' : 'secondary'} size="sm">
                {cert.isExpired
                  ? t('sslCertificates.statusExpired')
                  : t('sslCertificates.statusExpiresOn', {
                      date: new Date(cert.notAfter).toLocaleDateString(),
                    })}
              </Label>
            </div>
          ))}
        </Stack>
      )}

      <Modal
        isOpen={isAddOpen}
        onClose={() => setIsAddOpen(false)}
        title={t('sslCertificates.addTitle')}
        size="md"
        error={saveError ?? undefined}
        footer={
          <Row gap="2" justify="flex-end" full>
            <Button variant="ghost" onClick={() => setIsAddOpen(false)} disabled={isSaving}>
              {t('sslCertificates.cancel')}
            </Button>
            <Button
              variant="primary"
              onClick={handleUpload}
              isLoading={isSaving}
              disabled={!canSave}
              icon={<Plus size={14} />}
            >
              {t('sslCertificates.upload')}
            </Button>
          </Row>
        }
      >
        <Stack gap="3">
          {warnings.map(warning => (
            <Row key={warning} align="center" gap="1" className={styles.warningRow}>
              <AlertTriangle size={14} className={styles.warningIcon} />
              <Label variant="warning" size="sm">
                {warning}
              </Label>
            </Row>
          ))}
          {warnings.length > 0 && (
            <Button variant="primary" size="sm" onClick={handleUpload} isLoading={isSaving}>
              {t('sslCertificates.uploadAnyway')}
            </Button>
          )}
          <Input
            label={t('sslCertificates.name')}
            value={name}
            onChange={e => setName(e.target.value)}
            placeholder={t('sslCertificates.namePlaceholder')}
            autoFocus
          />
          <Stack gap="1">
            <Textarea
              label={t('sslCertificates.certificatePem')}
              value={certPem}
              onChange={e => setCertPem(e.target.value)}
              placeholder={t('sslCertificates.certificatePemPlaceholder')}
              rows={6}
            />
            <label className={styles.fileUploadLabel}>
              {t('sslCertificates.certificateUpload')}
              <input
                type="file"
                accept=".pem,.crt,.cer,.txt"
                className={styles.fileInput}
                onChange={async e => {
                  const file = e.target.files?.[0];
                  if (file) setCertPem(await readFileAsText(file));
                  e.target.value = '';
                }}
              />
            </label>
          </Stack>
          <Stack gap="1">
            <Textarea
              label={t('sslCertificates.privateKeyPem')}
              value={keyPem}
              onChange={e => setKeyPem(e.target.value)}
              placeholder={t('sslCertificates.privateKeyPemPlaceholder')}
              rows={6}
            />
            <label className={styles.fileUploadLabel}>
              {t('sslCertificates.privateKeyUpload')}
              <input
                type="file"
                accept=".pem,.key,.txt"
                className={styles.fileInput}
                onChange={async e => {
                  const file = e.target.files?.[0];
                  if (file) setKeyPem(await readFileAsText(file));
                  e.target.value = '';
                }}
              />
            </label>
          </Stack>
        </Stack>
      </Modal>

      <Modal
        isOpen={!!deleteTarget}
        onClose={() => setDeleteTarget(null)}
        title={t('sslCertificates.deleteTitle')}
        size="sm"
        error={deleteError ?? undefined}
        footer={
          <Row gap="2" justify="flex-end" full>
            <Button variant="ghost" onClick={() => setDeleteTarget(null)} disabled={isDeleting}>
              {t('sslCertificates.cancel')}
            </Button>
            <Button variant="danger" onClick={handleDelete} isLoading={isDeleting}>
              {t('sslCertificates.delete')}
            </Button>
          </Row>
        }
      >
        <Label variant="secondary" size="sm">
          {(deleteTarget?.attachedDomainCount ?? 0) > 0
            ? t('sslCertificates.deleteConfirmAttached', {
                name: deleteTarget?.name,
                count: deleteTarget?.attachedDomainCount,
              })
            : t('sslCertificates.deleteConfirm', { name: deleteTarget?.name })}
        </Label>
      </Modal>
    </div>
  );
}
