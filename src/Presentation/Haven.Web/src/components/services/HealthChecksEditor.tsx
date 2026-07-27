import { Pencil, Play, Plus, Trash2 } from 'lucide-react';
import { useCallback, useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';

import { BashHealthCheckConfig } from '@/api/types';
import { HealthCheckDto } from '@/api/types';
import { HealthCheckKind } from '@/api/types';
import { HttpHealthCheckConfig } from '@/api/types';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/layout';

import { healthChecksApi } from '../../api/healthChecks';
import { Row, Spacer, Stack } from '../layout';
import { Badge } from '../ui/Badge';
import { Button } from '../ui/Button';
import { Checkbox } from '../ui/Checkbox';
import { Divider } from '../ui/Divider';
import { ErrorAlert } from '../ui/ErrorAlert';
import { Input } from '../ui/Input';
import { Label } from '../ui/Label';
import { Modal } from '../ui/Modal';
import { SelectInput } from '../ui/SelectInput';
import { Spinner } from '../ui/Spinner';
import { ToggleChip } from '../ui/ToggleChip';

interface HealthChecksEditorProps {
  projectId: string;
  environmentId: string;
  serviceId: string;
}

const KIND_OPTIONS = [
  { value: 'Container', label: 'Container (Docker healthcheck)' },
  { value: 'Http', label: 'HTTP request' },
  { value: 'Bash', label: 'Bash command (docker exec)' },
];

const HTTP_METHOD_OPTIONS = [
  { value: 'GET', label: 'GET' },
  { value: 'HEAD', label: 'HEAD' },
  { value: 'POST', label: 'POST' },
  { value: 'PUT', label: 'PUT' },
  { value: 'PATCH', label: 'PATCH' },
  { value: 'DELETE', label: 'DELETE' },
  { value: 'OPTIONS', label: 'OPTIONS' },
];

const STATUS_VARIANT: Record<HealthCheckDto['lastRunStatus'], 'success' | 'danger' | 'default'> = {
  Healthy: 'success',
  Unhealthy: 'danger',
  Unknown: 'default',
};

const dateFormatter = new Intl.DateTimeFormat(undefined, {
  dateStyle: 'medium',
  timeStyle: 'short',
});

interface FormState {
  name: string;
  kind: HealthCheckKind;
  enabled: boolean;
  cronExpression: string;
  httpUrl: string;
  httpMethod: string;
  httpExpectedStatusCodes: string;
  httpTimeoutSeconds: string;
  bashCommand: string;
  bashExpectedExitCode: string;
  bashTimeoutSeconds: string;
}

const EMPTY_FORM: FormState = {
  name: '',
  kind: 'Container',
  enabled: true,
  cronExpression: '*/5 * * * *',
  httpUrl: '',
  httpMethod: 'GET',
  httpExpectedStatusCodes: '200',
  httpTimeoutSeconds: '5',
  bashCommand: '',
  bashExpectedExitCode: '0',
  bashTimeoutSeconds: '5',
};

function formToConfig(form: FormState): string {
  if (form.kind === 'Http') {
    const config: HttpHealthCheckConfig = {
      url: form.httpUrl.trim(),
      method: form.httpMethod,
      expectedStatusCodes: form.httpExpectedStatusCodes
        .split(',')
        .map(s => parseInt(s.trim(), 10))
        .filter(n => !isNaN(n)),
      timeoutSeconds: parseInt(form.httpTimeoutSeconds, 10) || 5,
    };
    return JSON.stringify(config);
  }
  if (form.kind === 'Bash') {
    const config: BashHealthCheckConfig = {
      command: form.bashCommand.trim(),
      expectedExitCode: parseInt(form.bashExpectedExitCode, 10) || 0,
      timeoutSeconds: parseInt(form.bashTimeoutSeconds, 10) || 5,
    };
    return JSON.stringify(config);
  }
  return '{}';
}

function healthCheckToForm(healthCheck: HealthCheckDto): FormState {
  const base: FormState = {
    ...EMPTY_FORM,
    name: healthCheck.name,
    kind: healthCheck.kind,
    enabled: healthCheck.enabled,
    cronExpression: healthCheck.cronExpression ?? '',
  };

  try {
    if (healthCheck.kind === 'Http') {
      const config = JSON.parse(healthCheck.config) as HttpHealthCheckConfig;
      base.httpUrl = config.url ?? '';
      base.httpMethod = config.method ?? 'GET';
      base.httpExpectedStatusCodes = (config.expectedStatusCodes ?? [200]).join(',');
      base.httpTimeoutSeconds = String(config.timeoutSeconds ?? 5);
    } else if (healthCheck.kind === 'Bash') {
      const config = JSON.parse(healthCheck.config) as BashHealthCheckConfig;
      base.bashCommand = config.command ?? '';
      base.bashExpectedExitCode = String(config.expectedExitCode ?? 0);
      base.bashTimeoutSeconds = String(config.timeoutSeconds ?? 5);
    }
  } catch {
    // config failed to parse — fall back to defaults for the kind-specific fields
  }

  return base;
}

function isFormValid(form: FormState): boolean {
  if (!form.name.trim()) return false;
  if (form.kind === 'Http' && !form.httpUrl.trim()) return false;
  if (form.kind === 'Bash' && !form.bashCommand.trim()) return false;
  return true;
}

export function HealthChecksEditor({
  projectId,
  environmentId,
  serviceId,
}: HealthChecksEditorProps) {
  const { t } = useTranslation(['services']);

  const [healthChecks, setHealthChecks] = useState<HealthCheckDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editTarget, setEditTarget] = useState<HealthCheckDto | null>(null);
  const [form, setForm] = useState<FormState>(EMPTY_FORM);
  const [isSaving, setIsSaving] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  const [deleteTarget, setDeleteTarget] = useState<HealthCheckDto | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);
  const [runningId, setRunningId] = useState<string | null>(null);

  const load = useCallback(async () => {
    try {
      setLoading(true);
      const result = await healthChecksApi.list(projectId, environmentId, serviceId);
      setHealthChecks(result ?? []);
    } catch (err) {
      setError(err instanceof Error ? err.message : t('services:error'));
    } finally {
      setLoading(false);
    }
  }, [projectId, environmentId, serviceId, t]);

  useEffect(() => {
    (async () => {
      await load();
    })();
  }, [load]);

  const openAddModal = () => {
    setEditTarget(null);
    setForm(EMPTY_FORM);
    setFormError(null);
    setIsFormOpen(true);
  };

  const openEditModal = (healthCheck: HealthCheckDto) => {
    setEditTarget(healthCheck);
    setForm(healthCheckToForm(healthCheck));
    setFormError(null);
    setIsFormOpen(true);
  };

  const handleSubmit = async () => {
    if (!isFormValid(form)) return;
    try {
      setIsSaving(true);
      setFormError(null);
      const config = formToConfig(form);

      if (editTarget) {
        await healthChecksApi.update(projectId, environmentId, serviceId, editTarget.id, {
          name: form.name.trim(),
          enabled: form.enabled,
          cronExpression: form.cronExpression.trim() || undefined,
          clearCronExpression: !form.cronExpression.trim(),
          config,
        });
      } else {
        await healthChecksApi.create(projectId, environmentId, serviceId, {
          name: form.name.trim(),
          kind: form.kind,
          enabled: form.enabled,
          cronExpression: form.cronExpression.trim() || undefined,
          config,
        });
      }

      setIsFormOpen(false);
      await load();
    } catch (err) {
      setFormError(err instanceof Error ? err.message : t('services:error'));
    } finally {
      setIsSaving(false);
    }
  };

  const handleDelete = async () => {
    if (!deleteTarget) return;
    try {
      setIsDeleting(true);
      await healthChecksApi.delete(projectId, environmentId, serviceId, deleteTarget.id);
      setDeleteTarget(null);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : t('services:error'));
    } finally {
      setIsDeleting(false);
    }
  };

  const handleRunNow = async (healthCheck: HealthCheckDto) => {
    try {
      setRunningId(healthCheck.id);
      await healthChecksApi.runNow(projectId, environmentId, serviceId, healthCheck.id);
    } catch (err) {
      setError(err instanceof Error ? err.message : t('services:error'));
    } finally {
      setRunningId(null);
    }
  };

  if (loading) {
    return (
      <Row justify="center" align="center">
        <Spinner />
      </Row>
    );
  }

  return (
    <Stack gap="4">
      {error && <ErrorAlert message={error} variant="block" />}

      <Row align="center" gap="2">
        <Label variant="primary" size="md" weight="semibold">
          {t('services:healthChecks.title')}
        </Label>
        {healthChecks.length > 0 && <Badge>{healthChecks.length}</Badge>}
        <Spacer expand direction="horizontal" />
        <Button variant="secondary" size="sm" icon={<Plus size={14} />} onClick={openAddModal}>
          {t('services:healthChecks.add')}
        </Button>
      </Row>

      {healthChecks.length === 0 ? (
        <Stack gap="3" align="center" style={{ padding: 'var(--space-10) var(--space-4)' }}>
          <Label variant="secondary" size="sm">
            {t('services:healthChecks.empty')}
          </Label>
          <Button variant="secondary" size="sm" icon={<Plus size={14} />} onClick={openAddModal}>
            {t('services:healthChecks.addFirst')}
          </Button>
        </Stack>
      ) : (
        <Table hoverable striped>
          <TableHead>
            <TableRow isHeader hasActionsColumn>
              <TableHeader>{t('services:healthChecks.name')}</TableHeader>
              <TableHeader>{t('services:healthChecks.kind')}</TableHeader>
              <TableHeader>{t('services:healthChecks.schedule')}</TableHeader>
              <TableHeader>{t('services:healthChecks.enabled')}</TableHeader>
              <TableHeader>{t('services:healthChecks.lastRun')}</TableHeader>
            </TableRow>
          </TableHead>
          <TableBody>
            {healthChecks.map(healthCheck => (
              <TableRow
                key={healthCheck.id}
                actions={
                  <>
                    <Button
                      variant="text"
                      size="xs"
                      icon={<Play size={14} />}
                      onClick={() => handleRunNow(healthCheck)}
                      isLoading={runningId === healthCheck.id}
                      title={t('services:healthChecks.runNow')}
                      aria-label={t('services:healthChecks.runNow')}
                    />
                    <Button
                      variant="text"
                      size="xs"
                      icon={<Pencil size={14} />}
                      onClick={() => openEditModal(healthCheck)}
                      title={t('services:healthChecks.edit')}
                      aria-label={t('services:healthChecks.edit')}
                    />
                    <Button
                      variant="text"
                      size="xs"
                      icon={<Trash2 size={14} />}
                      onClick={() => setDeleteTarget(healthCheck)}
                      title={t('services:healthChecks.delete')}
                      aria-label={t('services:healthChecks.delete')}
                    />
                  </>
                }
              >
                <TableCell>{healthCheck.name}</TableCell>
                <TableCell>
                  <Badge>{healthCheck.kind}</Badge>
                </TableCell>
                <TableCell variant="mono">
                  {healthCheck.cronExpression || t('services:healthChecks.manualOnly')}
                </TableCell>
                <TableCell>
                  <ToggleChip
                    checked={healthCheck.enabled}
                    onLabel={t('services:healthChecks.enabled')}
                    offLabel={t('services:healthChecks.disabled')}
                  />
                </TableCell>
                <TableCell>
                  <Stack gap="1">
                    <Badge variant={STATUS_VARIANT[healthCheck.lastRunStatus]}>
                      {healthCheck.lastRunStatus}
                    </Badge>
                    {healthCheck.lastRunAt && (
                      <Label variant="muted" size="xs">
                        {dateFormatter.format(new Date(healthCheck.lastRunAt))}
                      </Label>
                    )}
                  </Stack>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}

      <Modal
        isOpen={isFormOpen}
        onClose={() => setIsFormOpen(false)}
        title={
          editTarget ? t('services:healthChecks.editTitle') : t('services:healthChecks.addTitle')
        }
        size="sm"
        error={formError ?? undefined}
        footer={
          <Row gap="2" justify="flex-end" full>
            <Button variant="ghost" onClick={() => setIsFormOpen(false)} disabled={isSaving}>
              {t('services:healthChecks.cancel')}
            </Button>
            <Button
              variant="primary"
              onClick={handleSubmit}
              isLoading={isSaving}
              disabled={!isFormValid(form)}
              icon={<Plus size={14} />}
            >
              {editTarget ? t('services:healthChecks.save') : t('services:healthChecks.create')}
            </Button>
          </Row>
        }
      >
        <Stack gap="3">
          <Input
            label={t('services:healthChecks.name') + ' *'}
            value={form.name}
            onChange={e => setForm(p => ({ ...p, name: e.target.value }))}
            placeholder="Web server health"
            autoFocus
          />
          <SelectInput
            label={t('services:healthChecks.kind')}
            value={form.kind}
            onChange={v => setForm(p => ({ ...p, kind: v as HealthCheckKind }))}
            options={KIND_OPTIONS}
            disabled={!!editTarget}
          />
          <Checkbox
            label={t('services:healthChecks.enabled')}
            checked={form.enabled}
            onChange={e => setForm(p => ({ ...p, enabled: e.target.checked }))}
          />
          <Input
            label={t('services:healthChecks.cronExpression')}
            value={form.cronExpression}
            onChange={e => setForm(p => ({ ...p, cronExpression: e.target.value }))}
            placeholder="*/5 * * * *"
          />
          <Label variant="muted" size="xs">
            {t('services:healthChecks.cronHint')}
          </Label>

          {form.kind === 'Http' && (
            <>
              <Divider />
              <Input
                label={t('services:healthChecks.http.url') + ' *'}
                value={form.httpUrl}
                onChange={e => setForm(p => ({ ...p, httpUrl: e.target.value }))}
                placeholder="http://localhost:8080/health"
              />
              <SelectInput
                label={t('services:healthChecks.http.method')}
                value={form.httpMethod}
                onChange={v => setForm(p => ({ ...p, httpMethod: v }))}
                options={HTTP_METHOD_OPTIONS}
              />
              <Input
                label={t('services:healthChecks.http.expectedStatusCodes')}
                value={form.httpExpectedStatusCodes}
                onChange={e => setForm(p => ({ ...p, httpExpectedStatusCodes: e.target.value }))}
                placeholder="200,204"
              />
              <Input
                label={t('services:healthChecks.timeoutSeconds')}
                type="number"
                value={form.httpTimeoutSeconds}
                onChange={e => setForm(p => ({ ...p, httpTimeoutSeconds: e.target.value }))}
              />
            </>
          )}

          {form.kind === 'Bash' && (
            <>
              <Divider />
              <Input
                label={t('services:healthChecks.bash.command') + ' *'}
                value={form.bashCommand}
                onChange={e => setForm(p => ({ ...p, bashCommand: e.target.value }))}
                placeholder="curl -f http://localhost:8080/health"
              />
              <Input
                label={t('services:healthChecks.bash.expectedExitCode')}
                type="number"
                value={form.bashExpectedExitCode}
                onChange={e => setForm(p => ({ ...p, bashExpectedExitCode: e.target.value }))}
              />
              <Input
                label={t('services:healthChecks.timeoutSeconds')}
                type="number"
                value={form.bashTimeoutSeconds}
                onChange={e => setForm(p => ({ ...p, bashTimeoutSeconds: e.target.value }))}
              />
            </>
          )}
        </Stack>
      </Modal>

      <Modal
        isOpen={!!deleteTarget}
        onClose={() => setDeleteTarget(null)}
        title={t('services:healthChecks.deleteTitle')}
        size="sm"
        footer={
          <Row gap="2" justify="flex-end" full>
            <Button variant="ghost" onClick={() => setDeleteTarget(null)} disabled={isDeleting}>
              {t('services:healthChecks.cancel')}
            </Button>
            <Button variant="danger" onClick={handleDelete} isLoading={isDeleting}>
              {t('services:healthChecks.delete')}
            </Button>
          </Row>
        }
      >
        <Label variant="secondary" size="sm">
          {t('services:healthChecks.deleteConfirm', { name: deleteTarget?.name })}
        </Label>
      </Modal>
    </Stack>
  );
}
