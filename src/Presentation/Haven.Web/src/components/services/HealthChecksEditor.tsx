import { FlaskConical, History, Pencil, Play, Plus, Trash2 } from 'lucide-react';
import { Fragment, useCallback, useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';

import { BashHealthCheckConfig } from '@/api/types';
import { HealthCheckDto } from '@/api/types';
import { HealthCheckKind } from '@/api/types';
import { HealthCheckResultDto } from '@/api/types';
import { HttpHealthCheckConfig } from '@/api/types';
import { HttpHealthCheckMode } from '@/api/types';
import { TcpHealthCheckConfig } from '@/api/types';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/layout';
import styles from '@/styles/components/services/HealthChecks.module.css';

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
import { Tooltip } from '../ui/Tooltip';
import { HealthCheckHistoryModal } from './HealthCheckHistoryModal';
import { HealthCheckResultDetails } from './HealthCheckResultDetails';

interface HealthChecksEditorProps {
  projectId: string;
  environmentId: string;
  serviceId: string;
}

const KIND_OPTIONS = [
  { value: 'Container', label: 'Container (Docker healthcheck)' },
  { value: 'Http', label: 'HTTP request' },
  { value: 'Tcp', label: 'TCP port' },
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
  httpMode: HttpHealthCheckMode;
  tcpHost: string;
  tcpPort: string;
  tcpTimeoutSeconds: string;
  retries: string;
  failureThreshold: string;
  successThreshold: string;
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
  httpMode: 'Probe',
  tcpHost: '{{container}}',
  tcpPort: '',
  tcpTimeoutSeconds: '5',
  retries: '0',
  failureThreshold: '1',
  successThreshold: '1',
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
      mode: form.httpMode,
    };
    return JSON.stringify(config);
  }
  if (form.kind === 'Tcp') {
    const config: TcpHealthCheckConfig = {
      host: form.tcpHost.trim(),
      port: parseInt(form.tcpPort, 10) || 0,
      timeoutSeconds: parseInt(form.tcpTimeoutSeconds, 10) || 5,
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
    retries: String(healthCheck.retries ?? 0),
    failureThreshold: String(healthCheck.failureThreshold ?? 1),
    successThreshold: String(healthCheck.successThreshold ?? 1),
  };

  try {
    if (healthCheck.kind === 'Http') {
      const config = JSON.parse(healthCheck.config) as HttpHealthCheckConfig;
      base.httpUrl = config.url ?? '';
      base.httpMethod = config.method ?? 'GET';
      base.httpExpectedStatusCodes = (config.expectedStatusCodes ?? [200]).join(',');
      base.httpTimeoutSeconds = String(config.timeoutSeconds ?? 5);
      // Checks saved before the mode existed run from Haven itself; keep them that way.
      base.httpMode = config.mode ?? 'Direct';
    } else if (healthCheck.kind === 'Tcp') {
      const config = JSON.parse(healthCheck.config) as TcpHealthCheckConfig;
      base.tcpHost = config.host ?? '{{container}}';
      base.tcpPort = config.port ? String(config.port) : '';
      base.tcpTimeoutSeconds = String(config.timeoutSeconds ?? 5);
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
  if (form.kind === 'Tcp') {
    const port = parseInt(form.tcpPort, 10);
    if (!form.tcpHost.trim() || isNaN(port) || port < 1 || port > 65535) return false;
  }
  const inRange = (value: string, min: number, max: number) => {
    const n = parseInt(value, 10);
    return !isNaN(n) && n >= min && n <= max;
  };
  return (
    inRange(form.retries, 0, 10) &&
    inRange(form.failureThreshold, 1, 100) &&
    inRange(form.successThreshold, 1, 100)
  );
}

function formToSettings(form: FormState) {
  return {
    retries: parseInt(form.retries, 10) || 0,
    failureThreshold: parseInt(form.failureThreshold, 10) || 1,
    successThreshold: parseInt(form.successThreshold, 10) || 1,
  };
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
  const [runResult, setRunResult] = useState<{ name: string; result: HealthCheckResultDto } | null>(
    null
  );

  const [historyTarget, setHistoryTarget] = useState<HealthCheckDto | null>(null);

  const [expandedId, setExpandedId] = useState<string | null>(null);
  const [expandedResult, setExpandedResult] = useState<HealthCheckResultDto | null>(null);
  const [isExpandedLoading, setIsExpandedLoading] = useState(false);

  const [testResult, setTestResult] = useState<HealthCheckResultDto | null>(null);
  const [isTesting, setIsTesting] = useState(false);

  const load = useCallback(async () => {
    try {
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
    setTestResult(null);
    setIsFormOpen(true);
  };

  const openEditModal = (healthCheck: HealthCheckDto) => {
    setEditTarget(healthCheck);
    setForm(healthCheckToForm(healthCheck));
    setFormError(null);
    setTestResult(null);
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
          ...formToSettings(form),
        });
      } else {
        await healthChecksApi.create(projectId, environmentId, serviceId, {
          name: form.name.trim(),
          kind: form.kind,
          enabled: form.enabled,
          cronExpression: form.cronExpression.trim() || undefined,
          config,
          ...formToSettings(form),
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
      const result = await healthChecksApi.runNow(
        projectId,
        environmentId,
        serviceId,
        healthCheck.id
      );
      if (result) setRunResult({ name: healthCheck.name, result });
      setExpandedId(null);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : t('services:error'));
    } finally {
      setRunningId(null);
    }
  };

  const handleTest = async () => {
    if (!isFormValid(form)) return;
    try {
      setIsTesting(true);
      setFormError(null);
      setTestResult(null);
      const result = await healthChecksApi.test(projectId, environmentId, serviceId, {
        kind: form.kind,
        config: formToConfig(form),
      });
      setTestResult(result ?? null);
    } catch (err) {
      setFormError(err instanceof Error ? err.message : t('services:error'));
    } finally {
      setIsTesting(false);
    }
  };

  const toggleExpanded = async (healthCheck: HealthCheckDto) => {
    if (expandedId === healthCheck.id) {
      setExpandedId(null);
      return;
    }

    setExpandedId(healthCheck.id);
    setExpandedResult(null);
    try {
      setIsExpandedLoading(true);
      const results = await healthChecksApi.results(
        projectId,
        environmentId,
        serviceId,
        healthCheck.id,
        1
      );
      setExpandedResult(results?.[0] ?? null);
    } catch (err) {
      setError(err instanceof Error ? err.message : t('services:error'));
    } finally {
      setIsExpandedLoading(false);
    }
  };

  const reasonSummary = (healthCheck: HealthCheckDto) => {
    const reason =
      healthCheck.lastRunReason && healthCheck.lastRunReason !== 'None'
        ? t(`services:healthChecks.reasons.${healthCheck.lastRunReason}`)
        : null;
    return [reason, healthCheck.lastRunMessage].filter(Boolean).join(': ');
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
              <Fragment key={healthCheck.id}>
                <TableRow
                  actions={
                    <>
                      <Button
                        variant="text"
                        size="xs"
                        icon={<History size={14} />}
                        onClick={() => setHistoryTarget(healthCheck)}
                        title={t('services:healthChecks.history.button')}
                        aria-label={t('services:healthChecks.history.button')}
                      />
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
                    <div className={styles.statusCell}>
                      {reasonSummary(healthCheck) ? (
                        <Tooltip content={reasonSummary(healthCheck)} direction="below">
                          <Badge variant={STATUS_VARIANT[healthCheck.lastRunStatus]}>
                            {healthCheck.lastRunStatus}
                          </Badge>
                        </Tooltip>
                      ) : (
                        <Badge variant={STATUS_VARIANT[healthCheck.lastRunStatus]}>
                          {healthCheck.lastRunStatus}
                        </Badge>
                      )}
                      {healthCheck.lastRunAt ? (
                        <Label variant="muted" size="xs">
                          {dateFormatter.format(new Date(healthCheck.lastRunAt))}
                        </Label>
                      ) : (
                        <Label variant="muted" size="xs">
                          {t('services:healthChecks.neverRun')}
                        </Label>
                      )}
                      {healthCheck.lastRunStatus !== 'Unhealthy' &&
                        healthCheck.consecutiveFailures > 0 && (
                          <Label variant="muted" size="xs">
                            {t('services:healthChecks.pendingFailures', {
                              count: healthCheck.consecutiveFailures,
                              threshold: healthCheck.failureThreshold,
                            })}
                          </Label>
                        )}
                      {healthCheck.lastRunAt && (
                        <button
                          type="button"
                          className={styles.linkButton}
                          onClick={() => toggleExpanded(healthCheck)}
                        >
                          {expandedId === healthCheck.id
                            ? t('services:healthChecks.details.hide')
                            : t('services:healthChecks.details.show')}
                        </button>
                      )}
                    </div>
                  </TableCell>
                </TableRow>
                {expandedId === healthCheck.id && (
                  <TableRow>
                    <TableCell colSpan={6} className={styles.expandedCell}>
                      {isExpandedLoading ? (
                        <Spinner />
                      ) : expandedResult ? (
                        <HealthCheckResultDetails outcome={expandedResult} />
                      ) : (
                        <Label variant="muted" size="xs">
                          {t('services:healthChecks.details.none')}
                        </Label>
                      )}
                    </TableCell>
                  </TableRow>
                )}
              </Fragment>
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
              <SelectInput
                label={t('services:healthChecks.mode.label')}
                value={form.httpMode}
                onChange={v => setForm(p => ({ ...p, httpMode: v as HttpHealthCheckMode }))}
                options={[
                  { value: 'Probe', label: t('services:healthChecks.mode.probe') },
                  { value: 'Direct', label: t('services:healthChecks.mode.direct') },
                ]}
              />
              <Label variant="muted" size="xs">
                {form.httpMode === 'Probe'
                  ? t('services:healthChecks.mode.probeHint')
                  : t('services:healthChecks.mode.directHint')}
              </Label>
              <Input
                label={t('services:healthChecks.http.url') + ' *'}
                value={form.httpUrl}
                onChange={e => setForm(p => ({ ...p, httpUrl: e.target.value }))}
                placeholder={
                  form.httpMode === 'Probe'
                    ? 'http://{{container}}:8080/health'
                    : 'http://localhost:8080/health'
                }
              />
              <Label variant="muted" size="xs">
                {form.httpMode === 'Probe'
                  ? t('services:healthChecks.http.urlHintProbe')
                  : t('services:healthChecks.http.urlHintDirect')}
              </Label>
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

          {form.kind === 'Tcp' && (
            <>
              <Divider />
              <Input
                label={t('services:healthChecks.tcp.host') + ' *'}
                value={form.tcpHost}
                onChange={e => setForm(p => ({ ...p, tcpHost: e.target.value }))}
                placeholder="{{container}}"
              />
              <Label variant="muted" size="xs">
                {t('services:healthChecks.tcp.hostHint')}
              </Label>
              <Input
                label={t('services:healthChecks.tcp.port') + ' *'}
                type="number"
                value={form.tcpPort}
                onChange={e => setForm(p => ({ ...p, tcpPort: e.target.value }))}
                placeholder="5432"
              />
              <Input
                label={t('services:healthChecks.timeoutSeconds')}
                type="number"
                value={form.tcpTimeoutSeconds}
                onChange={e => setForm(p => ({ ...p, tcpTimeoutSeconds: e.target.value }))}
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

          <Divider />
          <Label variant="primary" size="sm" weight="semibold">
            {t('services:healthChecks.resilience.title')}
          </Label>
          <Input
            label={t('services:healthChecks.resilience.retries')}
            type="number"
            value={form.retries}
            onChange={e => setForm(p => ({ ...p, retries: e.target.value }))}
          />
          <Label variant="muted" size="xs">
            {t('services:healthChecks.resilience.retriesHint')}
          </Label>
          <Input
            label={t('services:healthChecks.resilience.failureThreshold')}
            type="number"
            value={form.failureThreshold}
            onChange={e => setForm(p => ({ ...p, failureThreshold: e.target.value }))}
          />
          <Label variant="muted" size="xs">
            {t('services:healthChecks.resilience.failureThresholdHint')}
          </Label>
          <Input
            label={t('services:healthChecks.resilience.successThreshold')}
            type="number"
            value={form.successThreshold}
            onChange={e => setForm(p => ({ ...p, successThreshold: e.target.value }))}
          />
          <Label variant="muted" size="xs">
            {t('services:healthChecks.resilience.successThresholdHint')}
          </Label>

          <Divider />
          <Row align="center" gap="2">
            <Button
              variant="secondary"
              size="sm"
              icon={<FlaskConical size={14} />}
              onClick={handleTest}
              isLoading={isTesting}
              disabled={!isFormValid(form)}
            >
              {isTesting
                ? t('services:healthChecks.test.running')
                : t('services:healthChecks.test.button')}
            </Button>
            {testResult && (
              <Badge variant={STATUS_VARIANT[testResult.status]}>{testResult.status}</Badge>
            )}
          </Row>
          <Label variant="muted" size="xs">
            {t('services:healthChecks.test.hint')}
          </Label>
          {testResult && <HealthCheckResultDetails outcome={testResult} />}
        </Stack>
      </Modal>

      <Modal
        isOpen={!!runResult}
        onClose={() => setRunResult(null)}
        title={t('services:healthChecks.runResult.title', { name: runResult?.name })}
        size="sm"
      >
        {runResult && (
          <Stack gap="3">
            <Badge variant={STATUS_VARIANT[runResult.result.status]}>
              {runResult.result.status}
            </Badge>
            <HealthCheckResultDetails outcome={runResult.result} />
          </Stack>
        )}
      </Modal>

      <HealthCheckHistoryModal
        projectId={projectId}
        environmentId={environmentId}
        serviceId={serviceId}
        healthCheck={historyTarget}
        onClose={() => setHistoryTarget(null)}
      />

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
