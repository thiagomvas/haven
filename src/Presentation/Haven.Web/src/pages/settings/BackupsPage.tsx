import { ReactNode, useState } from 'react';
import { useTranslation } from 'react-i18next';

import { BackupOptions, RestoreBackupResult } from '@/api/backups';
import { Row, Stack } from '@/components/layout';
import { Badge } from '@/components/ui/Badge';
import { Banner } from '@/components/ui/Banner';
import { Button } from '@/components/ui/Button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/Card';
import { Checkbox } from '@/components/ui/Checkbox';
import { ErrorAlert } from '@/components/ui/ErrorAlert';
import { Form, FormGroup, FormInput, FormLabel, FormSelect } from '@/components/ui/Form';
import { Label } from '@/components/ui/Label';
import { Modal } from '@/components/ui/Modal';
import { Spinner } from '@/components/ui/Spinner';
import { Tabs } from '@/components/ui/Tabs';
import {
  useBackupOptions,
  useCreateBackup,
  useGitCommits,
  useRestoreBackup,
  useSnapshots,
  useUpdateBackupOptions,
} from '@/hooks/useBackups';
import { useForm } from '@/hooks/useForm';
import { useFormatDate } from '@/hooks/useFormatDate';
import { useGitCredentials } from '@/hooks/useGitCredentials';

import styles from '@/styles/pages/settings/RestoreBackupCard.module.css';

const CRON_PRESETS = [
  { label: 'schedule.presets.daily', value: '0 0 * * *' },
  { label: 'schedule.presets.twiceDaily', value: '0 0,12 * * *' },
  { label: 'schedule.presets.weekly', value: '0 0 * * 0' },
  { label: 'schedule.presets.monthly', value: '0 0 1 * *' },
  { label: 'schedule.presets.custom', value: 'custom' },
] as const;

function resolvePreset(cron: string): string {
  return CRON_PRESETS.find(p => p.value === cron)?.value ?? 'custom';
}

function BackupOptionsForm({ current }: { current: BackupOptions }) {
  const { t } = useTranslation('settings');
  const { mutateAsync: updateOptions } = useUpdateBackupOptions();

  const initialPreset = resolvePreset(current.cronExpression);

  const { values, fieldErrors, submitError, isLoading, handleSubmit, updateField } = useForm({
    initialValues: {
      enabled: current.enabled,
      backupsPath: current.backupsPath,
      retentionCount: current.retentionCount,
      cronExpression: current.cronExpression,
      cronPreset: initialPreset,
      gitEnabled: current.git.enabled,
      gitRemoteUrl: current.git.remoteUrl ?? '',
      gitBranch: current.git.branch,
      gitCredentialsId: current.git.gitCredentialsId ?? '',
    },
    onSubmit: async values => {
      const options: BackupOptions = {
        enabled: values.enabled,
        backupsPath: values.backupsPath,
        retentionCount: values.retentionCount,
        cronExpression: values.cronExpression,
        git: {
          enabled: values.gitEnabled,
          remoteUrl: values.gitRemoteUrl || undefined,
          branch: values.gitBranch,
          gitCredentialsId: credentials.some(c => c.id === values.gitCredentialsId)
            ? values.gitCredentialsId
            : null,
        },
      };
      await updateOptions(options);
    },
  });

  function handlePresetChange(preset: string) {
    updateField('cronPreset', preset);
    if (preset !== 'custom') {
      updateField('cronExpression', preset);
    }
  }

  const { data: credentialsPage } = useGitCredentials({ pageNumber: 1, pageSize: 100 });
  const credentials = credentialsPage?.items ?? [];

  return (
    <Form onSubmit={handleSubmit} isLoading={isLoading}>
      <FormGroup>
        <Checkbox
          label={t('backups.fields.enabled')}
          description={t('backups.fields.enabledDescription')}
          checked={values.enabled}
          onChange={e => updateField('enabled', e.target.checked)}
        />
      </FormGroup>
      <FormGroup>
        <FormLabel htmlFor="backupsPath" required>
          {t('backups.fields.backupsPath')}
        </FormLabel>
        <FormInput
          id="backupsPath"
          type="text"
          value={values.backupsPath}
          onChange={e => updateField('backupsPath', e.target.value)}
          placeholder="/var/lib/haven/backups"
          fieldName="backupsPath"
          fieldErrors={fieldErrors}
          disabled={!values.enabled}
        />
      </FormGroup>
      <FormGroup>
        <FormLabel htmlFor="retentionCount" required>
          {t('backups.fields.retentionCount')}
        </FormLabel>
        <FormInput
          id="retentionCount"
          type="number"
          value={values.retentionCount}
          onChange={e => updateField('retentionCount', Number(e.target.value))}
          min={1}
          fieldName="retentionCount"
          fieldErrors={fieldErrors}
          disabled={!values.enabled}
        />
      </FormGroup>

      <div style={{ marginTop: 'var(--space-4)', marginBottom: 'var(--space-2)' }}>
        <Label variant="muted">{t('backups.schedule.sectionTitle')}</Label>
      </div>

      <FormGroup>
        <FormLabel htmlFor="cronPreset" required>
          {t('backups.schedule.preset')}
        </FormLabel>
        <FormSelect
          id="cronPreset"
          value={values.cronPreset}
          onChange={e => handlePresetChange(e.target.value)}
          fieldName="cronPreset"
          fieldErrors={fieldErrors}
          disabled={!values.enabled}
        >
          {CRON_PRESETS.map(p => (
            <option key={p.value} value={p.value}>
              {t(`backups.${p.label}`)}
            </option>
          ))}
        </FormSelect>
      </FormGroup>

      {values.cronPreset === 'custom' && (
        <FormGroup>
          <FormLabel htmlFor="cronExpression" required>
            {t('backups.schedule.customCron')}
          </FormLabel>
          <FormInput
            id="cronExpression"
            type="text"
            value={values.cronExpression}
            onChange={e => updateField('cronExpression', e.target.value)}
            placeholder="0 0 * * *"
            fieldName="cronExpression"
            fieldErrors={fieldErrors}
            disabled={!values.enabled}
          />
        </FormGroup>
      )}

      <div style={{ marginTop: 'var(--space-4)', marginBottom: 'var(--space-2)' }}>
        <Label variant="muted">{t('backups.git.sectionTitle')}</Label>
      </div>

      <FormGroup>
        <Checkbox
          label={t('backups.git.enabled')}
          description={t('backups.git.enabledDescription')}
          checked={values.gitEnabled}
          onChange={e => updateField('gitEnabled', e.target.checked)}
          disabled={!values.enabled}
        />
      </FormGroup>
      <FormGroup>
        <FormLabel htmlFor="gitRemoteUrl">{t('backups.git.remoteUrl')}</FormLabel>
        <FormInput
          id="gitRemoteUrl"
          type="text"
          value={values.gitRemoteUrl}
          onChange={e => updateField('gitRemoteUrl', e.target.value)}
          placeholder="https://github.com/org/repo.git"
          fieldName="gitRemoteUrl"
          fieldErrors={fieldErrors}
          disabled={!values.enabled || !values.gitEnabled}
        />
      </FormGroup>
      <FormGroup>
        <FormLabel htmlFor="gitBranch" required>
          {t('backups.git.branch')}
        </FormLabel>
        <FormInput
          id="gitBranch"
          type="text"
          value={values.gitBranch}
          onChange={e => updateField('gitBranch', e.target.value)}
          placeholder="main"
          fieldName="gitBranch"
          fieldErrors={fieldErrors}
          disabled={!values.enabled || !values.gitEnabled}
        />
      </FormGroup>
      <FormGroup>
        <FormLabel htmlFor="gitCredentialsId">{t('backups.git.credentials')}</FormLabel>
        <FormSelect
          id="gitCredentialsId"
          value={values.gitCredentialsId}
          onChange={e => updateField('gitCredentialsId', e.target.value)}
          fieldName="gitCredentialsId"
          fieldErrors={fieldErrors}
          disabled={!values.enabled || !values.gitEnabled}
        >
          <option value="">{t('backups.git.noCredentials')}</option>
          {credentials.map(cred => (
            <option key={cred.id} value={cred.id}>
              {cred.displayName}
            </option>
          ))}
        </FormSelect>
      </FormGroup>

      {values.gitEnabled && values.gitRemoteUrl && !values.gitCredentialsId && (
        <Banner variant="warning">{t('backups.git.noCredentialsWarning')}</Banner>
      )}

      {submitError && <ErrorAlert message={submitError} variant="block" />}
      <Row justify="flex-end">
        <Button type="submit" variant="primary" isLoading={isLoading}>
          {t('backups.save')}
        </Button>
      </Row>
    </Form>
  );
}

function ManualBackupCard() {
  const { t } = useTranslation('settings');
  const { mutateAsync: createBackup, isPending } = useCreateBackup();
  const [lastSnapshot, setLastSnapshot] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function handleCreate() {
    setError(null);
    try {
      const result = await createBackup();
      setLastSnapshot(result.snapshotPath);
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : t('backups.manual.failed'));
    }
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>{t('backups.manual.title')}</CardTitle>
        <Label variant="muted">{t('backups.manual.description')}</Label>
      </CardHeader>
      <CardContent>
        {lastSnapshot && (
          <div style={{ marginBottom: 'var(--space-3)' }}>
            <Label variant="muted">
              {t('backups.manual.snapshotPath', { path: lastSnapshot })}
            </Label>
          </div>
        )}
        {error && <ErrorAlert message={error} variant="block" />}
        <Row justify="flex-end">
          <Button variant="secondary" onClick={handleCreate} isLoading={isPending}>
            {t('backups.manual.trigger')}
          </Button>
        </Row>
      </CardContent>
    </Card>
  );
}

type ChangeType = 'created' | 'updated' | 'deleted';

const CHANGE_COLOR: Record<ChangeType, string> = {
  created: 'var(--color-success)',
  updated: 'var(--color-warning)',
  deleted: 'var(--color-danger)',
};
const CHANGE_PREFIX: Record<ChangeType, string> = { created: '+', updated: '~', deleted: '-' };
const CHANGE_BADGE: Record<ChangeType, 'success' | 'warning' | 'danger'> = {
  created: 'success',
  updated: 'warning',
  deleted: 'danger',
};

interface TreeNode {
  id: string;
  name: string;
  change: ChangeType | null;
  children: TreeNode[];
  envVarChanges: Array<{ key: string; change: ChangeType }>;
  volumeFileChanges: Array<{ path: string; change: ChangeType }>;
}

function buildTree(preview: RestoreBackupResult): { projects: TreeNode[]; networks: TreeNode[] } {
  const projectChanges = new Map<string, ChangeType>();
  [
    ...preview.projects.created.map(p => [p.id, 'created'] as const),
    ...preview.projects.updated.map(p => [p.id, 'updated'] as const),
    ...preview.projects.deleted.map(p => [p.id, 'deleted'] as const),
  ].forEach(([id, ct]) => projectChanges.set(id, ct));

  const envChanges = new Map<string, ChangeType>();
  [
    ...preview.environments.created.map(e => [e.id, 'created'] as const),
    ...preview.environments.updated.map(e => [e.id, 'updated'] as const),
    ...preview.environments.deleted.map(e => [e.id, 'deleted'] as const),
  ].forEach(([id, ct]) => envChanges.set(id, ct));

  const svcChanges = new Map<string, ChangeType>();
  [
    ...preview.services.created.map(s => [s.id, 'created'] as const),
    ...preview.services.updated.map(s => [s.id, 'updated'] as const),
    ...preview.services.deleted.map(s => [s.id, 'deleted'] as const),
  ].forEach(([id, ct]) => svcChanges.set(id, ct));

  const allEnvVarChanges = [
    ...preview.environmentVariables.created.map(v => ({ ...v, change: 'created' as ChangeType })),
    ...preview.environmentVariables.updated.map(v => ({ ...v, change: 'updated' as ChangeType })),
    ...preview.environmentVariables.deleted.map(v => ({ ...v, change: 'deleted' as ChangeType })),
  ];

  const envVarsByParent = new Map<string, Array<{ key: string; change: ChangeType }>>();
  for (const v of allEnvVarChanges) {
    const list = envVarsByParent.get(v.parentId) ?? [];
    list.push({ key: v.key, change: v.change });
    envVarsByParent.set(v.parentId, list);
  }

  const allVolumeFileChanges = [
    ...(preview.volumeFiles?.created ?? []).map(v => ({ ...v, change: 'created' as ChangeType })),
    ...(preview.volumeFiles?.updated ?? []).map(v => ({ ...v, change: 'updated' as ChangeType })),
    ...(preview.volumeFiles?.deleted ?? []).map(v => ({ ...v, change: 'deleted' as ChangeType })),
  ];

  const volumeFilesByService = new Map<string, Array<{ path: string; change: ChangeType }>>();
  for (const v of allVolumeFileChanges) {
    const list = volumeFilesByService.get(v.serviceId) ?? [];
    list.push({ path: v.path, change: v.change });
    volumeFilesByService.set(v.serviceId, list);
  }

  // Collect all services, group by environmentId
  const allServices = [
    ...preview.services.created,
    ...preview.services.updated,
    ...preview.services.deleted,
  ];
  const servicesByEnv = new Map<string, typeof allServices>();
  for (const svc of allServices) {
    const list = servicesByEnv.get(svc.environmentId) ?? [];
    list.push(svc);
    servicesByEnv.set(svc.environmentId, list);
  }

  // Environment context (id -> {id, name, projectId}), seeded from the environments diff and then
  // enriched from services. This ensures a service whose environment and project are otherwise
  // unchanged (e.g. only its managed-volume files changed) still renders under its hierarchy.
  type EnvContext = { id: string; name: string; projectId: string };
  const allEnvs = [
    ...preview.environments.created,
    ...preview.environments.updated,
    ...preview.environments.deleted,
  ];

  const envContext = new Map<string, EnvContext>();
  for (const env of allEnvs) {
    envContext.set(env.id, { id: env.id, name: env.name, projectId: env.projectId });
  }

  const projectNameById = new Map<string, string>(
    [...preview.projects.created, ...preview.projects.updated, ...preview.projects.deleted].map(
      p => [p.id, p.name]
    )
  );
  for (const env of allEnvs) {
    if (env.projectName && !projectNameById.has(env.projectId))
      projectNameById.set(env.projectId, env.projectName);
  }

  for (const svc of allServices) {
    if (!envContext.has(svc.environmentId)) {
      envContext.set(svc.environmentId, {
        id: svc.environmentId,
        name: svc.environmentName ?? svc.environmentId,
        projectId: svc.projectId,
      });
    }
    if (svc.projectId && svc.projectName && !projectNameById.has(svc.projectId))
      projectNameById.set(svc.projectId, svc.projectName);
  }

  const envsByProject = new Map<string, EnvContext[]>();
  for (const env of envContext.values()) {
    const list = envsByProject.get(env.projectId) ?? [];
    list.push(env);
    envsByProject.set(env.projectId, list);
  }

  const allProjectIds = new Set<string>([
    ...projectChanges.keys(),
    ...[...envContext.values()].map(e => e.projectId),
  ]);

  const projectNodes: TreeNode[] = [];

  for (const projectId of allProjectIds) {
    const projectName = projectNameById.get(projectId) ?? projectId;
    const projectEnvs = envsByProject.get(projectId) ?? [];

    const envNodes: TreeNode[] = projectEnvs.map(env => {
      const envServices = servicesByEnv.get(env.id) ?? [];

      const serviceNodes: TreeNode[] = envServices.map(svc => ({
        id: svc.id,
        name: svc.name,
        change: svcChanges.get(svc.id) ?? null,
        children: [],
        envVarChanges: envVarsByParent.get(svc.id) ?? [],
        volumeFileChanges: volumeFilesByService.get(svc.id) ?? [],
      }));

      return {
        id: env.id,
        name: env.name,
        change: envChanges.get(env.id) ?? null,
        children: serviceNodes,
        envVarChanges: envVarsByParent.get(env.id) ?? [],
        volumeFileChanges: [],
      };
    });

    projectNodes.push({
      id: projectId,
      name: projectName,
      change: projectChanges.get(projectId) ?? null,
      children: envNodes,
      envVarChanges: envVarsByParent.get(projectId) ?? [],
      volumeFileChanges: [],
    });
  }

  const networkNodes: TreeNode[] = [
    ...preview.networks.created.map(n => ({
      id: n.id,
      name: n.name,
      change: 'created' as ChangeType,
      children: [],
      envVarChanges: [],
      volumeFileChanges: [],
    })),
    ...preview.networks.updated.map(n => ({
      id: n.id,
      name: n.name,
      change: 'updated' as ChangeType,
      children: [],
      envVarChanges: [],
      volumeFileChanges: [],
    })),
    ...preview.networks.deleted.map(n => ({
      id: n.id,
      name: n.name,
      change: 'deleted' as ChangeType,
      children: [],
      envVarChanges: [],
      volumeFileChanges: [],
    })),
  ];

  return { projects: projectNodes, networks: networkNodes };
}

function TreeRow({
  node,
  depth = 0,
  isLast = false,
}: {
  node: TreeNode;
  depth?: number;
  isLast?: boolean;
}) {
  const prefix = CHANGE_PREFIX[node.change ?? 'updated'];
  const color = node.change ? CHANGE_COLOR[node.change] : 'var(--color-text-secondary)';
  const indent = depth * 20;
  const connector = depth > 0 ? (isLast ? '└─ ' : '├─ ') : '';

  const leaves = [
    ...node.envVarChanges.map(v => ({ kind: 'env' as const, label: v.key, change: v.change })),
    ...node.volumeFileChanges.map(v => ({ kind: 'file' as const, label: v.path, change: v.change })),
  ];

  return (
    <>
      <div
        style={{
          display: 'flex',
          alignItems: 'baseline',
          gap: 'var(--space-2)',
          fontFamily: 'monospace',
          fontSize: 'var(--text-sm)',
          padding: '2px 0',
        }}
      >
        <span
          style={{
            color: 'var(--color-text-secondary)',
            userSelect: 'none',
            width: indent + (depth > 0 ? 24 : 12),
            flexShrink: 0,
            textAlign: 'right',
          }}
        >
          {depth > 0 ? connector : <span style={{ color, fontWeight: 700 }}>{prefix}</span>}
        </span>
        {depth > 0 && <span style={{ color, fontWeight: 700, marginRight: 4 }}>{prefix}</span>}
        <span style={{ color: 'var(--color-text-primary)' }}>{node.name}</span>
        {!node.change && <Badge variant="default">no change</Badge>}
      </div>
      {leaves.map((leaf, i) => {
        const isLastChild = i === leaves.length - 1 && node.children.length === 0;
        return (
          <div
            key={`${leaf.kind}-${leaf.label}`}
            style={{
              display: 'flex',
              alignItems: 'baseline',
              gap: 'var(--space-2)',
              fontFamily: 'monospace',
              fontSize: 'var(--text-sm)',
              padding: '2px 0',
            }}
          >
            <span
              style={{
                color: 'var(--color-text-secondary)',
                userSelect: 'none',
                width: (depth + 1) * 20 + 24,
                flexShrink: 0,
                textAlign: 'right',
              }}
            >
              {isLastChild ? '└─ ' : '├─ '}
            </span>
            <span style={{ color: CHANGE_COLOR[leaf.change], fontWeight: 700, marginRight: 4 }}>
              {CHANGE_PREFIX[leaf.change]}
            </span>
            <span style={{ color: 'var(--color-text-secondary)' }}>{leaf.label}</span>
          </div>
        );
      })}
      {node.children.map((child, i) => (
        <TreeRow
          key={child.id}
          node={child}
          depth={depth + 1}
          isLast={i === node.children.length - 1}
        />
      ))}
    </>
  );
}

function DiffTree({ preview }: { preview: RestoreBackupResult }) {
  const { t } = useTranslation('settings');
  const { projects, networks } = buildTree(preview);

  const totalCounts = {
    created:
      preview.projects.created.length +
      preview.environments.created.length +
      preview.services.created.length +
      preview.networks.created.length,
    updated:
      preview.projects.updated.length +
      preview.environments.updated.length +
      preview.services.updated.length +
      preview.networks.updated.length,
    deleted:
      preview.projects.deleted.length +
      preview.environments.deleted.length +
      preview.services.deleted.length +
      preview.networks.deleted.length,
  };

  return (
    <Stack gap="4">
      <Row gap="2">
        {totalCounts.created > 0 && (
          <Badge variant="success">
            +{totalCounts.created} {t('backups.restore.preview.created')}
          </Badge>
        )}
        {totalCounts.updated > 0 && (
          <Badge variant="warning">
            ~{totalCounts.updated} {t('backups.restore.preview.updated')}
          </Badge>
        )}
        {totalCounts.deleted > 0 && (
          <Badge variant="danger">
            -{totalCounts.deleted} {t('backups.restore.preview.deleted')}
          </Badge>
        )}
      </Row>
      <div
        style={{
          background: 'var(--color-surface)',
          border: '1px solid var(--color-border)',
          borderRadius: 'var(--radius-md)',
          padding: 'var(--space-3)',
          maxHeight: 360,
          overflowY: 'auto',
        }}
      >
        {projects.map(p => (
          <TreeRow key={p.id} node={p} depth={0} />
        ))}
        {networks.length > 0 && (
          <>
            <div
              style={{ margin: 'var(--space-2) 0', borderTop: '1px solid var(--color-border)' }}
            />
            <Label variant="muted" style={{ fontFamily: 'monospace', fontSize: 'var(--text-sm)' }}>
              {t('backups.restore.preview.sections.networks')}
            </Label>
            {networks.map(n => (
              <TreeRow key={n.id} node={n} depth={0} />
            ))}
          </>
        )}
      </div>
    </Stack>
  );
}

function RestorePreviewModal({
  isOpen,
  onClose,
  preview,
  onConfirm,
  isConfirming,
  confirmError,
  success,
  restoreWarnings,
}: {
  isOpen: boolean;
  onClose: () => void;
  preview: RestoreBackupResult | null;
  onConfirm: () => void;
  isConfirming: boolean;
  confirmError: string | null;
  success: boolean;
  restoreWarnings: string[];
}) {
  const { t } = useTranslation('settings');

  const hasAnyChanges = preview
    ? [
        preview.projects,
        preview.environments,
        preview.networks,
        preview.services,
        preview.environmentVariables,
        preview.volumeFiles,
      ].some(s => (s?.created.length ?? 0) + (s?.updated.length ?? 0) + (s?.deleted.length ?? 0) > 0)
    : false;

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title={t('backups.restore.preview.title')}
      description={t('backups.restore.preview.description')}
      size="lg"
      closeOnBackdropClick={!isConfirming}
      error={confirmError ?? undefined}
      footer={
        success ? undefined : (
          <Row justify="flex-end" gap="2">
            <Button variant="ghost" onClick={onClose} disabled={isConfirming}>
              {t('backups.restore.preview.cancel')}
            </Button>
            <Button
              variant="danger"
              onClick={onConfirm}
              isLoading={isConfirming}
              disabled={!hasAnyChanges}
            >
              {t('backups.restore.preview.confirm')}
            </Button>
          </Row>
        )
      }
    >
      {success ? (
        restoreWarnings.length > 0 ? (
          <Stack gap="2">
            <Banner
              variant="warning"
              description={t('backups.restore.preview.successWithWarnings')}
            />
            <ul>
              {restoreWarnings.map(warning => (
                <li key={warning}>{warning}</li>
              ))}
            </ul>
          </Stack>
        ) : (
          <Banner variant="success" description={t('backups.restore.preview.success')} />
        )
      ) : !preview ? (
        <Row justify="center">
          <Spinner />
        </Row>
      ) : !hasAnyChanges ? (
        <Label variant="muted">{t('backups.restore.preview.noChanges')}</Label>
      ) : (
        <DiffTree preview={preview} />
      )}
    </Modal>
  );
}

function SourceList({ children }: { children: ReactNode }) {
  return <div className={styles.sourceList}>{children}</div>;
}

function RestoreBackupCard() {
  const { t } = useTranslation('settings');
  const formatDate = useFormatDate();

  const { data: snapshots, isLoading: snapshotsLoading } = useSnapshots();
  const { data: commits, isLoading: commitsLoading } = useGitCommits();
  const { mutateAsync: restore } = useRestoreBackup();

  const [selectedSnapshot, setSelectedSnapshot] = useState<string | null>(null);
  const [selectedCommit, setSelectedCommit] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState('snapshot');

  const [modalOpen, setModalOpen] = useState(false);
  const [preview, setPreview] = useState<RestoreBackupResult | null>(null);
  const [previewLoading, setPreviewLoading] = useState(false);
  const [previewError, setPreviewError] = useState<string | null>(null);
  const [isConfirming, setIsConfirming] = useState(false);
  const [confirmError, setConfirmError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);
  const [restoreWarnings, setRestoreWarnings] = useState<string[]>([]);

  const selectedSource =
    activeTab === 'snapshot' ? selectedSnapshot : activeTab === 'git' ? selectedCommit : 'manifest';

  async function handlePreview() {
    if (!selectedSource) return;
    setPreviewLoading(true);
    setPreviewError(null);
    setPreview(null);
    setSuccess(false);
    setConfirmError(null);
    setModalOpen(true);

    try {
      const result = await restore({
        source: activeTab === 'snapshot' ? 'FileSystem' : activeTab === 'git' ? 'Git' : 'Manifest',
        snapshotName: activeTab === 'snapshot' ? selectedSource : undefined,
        commitSha: activeTab === 'git' ? selectedSource : undefined,
        dryRun: true,
      });
      setPreview(result);
    } catch (e: unknown) {
      setPreviewError(e instanceof Error ? e.message : 'Failed to preview restore.');
      setModalOpen(false);
    } finally {
      setPreviewLoading(false);
    }
  }

  async function handleConfirm() {
    if (!selectedSource) return;
    setIsConfirming(true);
    setConfirmError(null);

    try {
      const result = await restore({
        source: activeTab === 'snapshot' ? 'FileSystem' : activeTab === 'git' ? 'Git' : 'Manifest',
        snapshotName: activeTab === 'snapshot' ? selectedSource : undefined,
        commitSha: activeTab === 'git' ? selectedSource : undefined,
        dryRun: false,
      });
      setRestoreWarnings(result.volumeFileRestoreWarnings ?? []);
      setSuccess(true);
    } catch (e: unknown) {
      setConfirmError(e instanceof Error ? e.message : 'Restore failed.');
    } finally {
      setIsConfirming(false);
    }
  }

  function handleModalClose() {
    if (isConfirming) return;
    setModalOpen(false);
    if (success) {
      setSuccess(false);
      setRestoreWarnings([]);
      setPreview(null);
      setSelectedSnapshot(null);
      setSelectedCommit(null);
    }
  }

  const snapshotTab = snapshotsLoading ? (
    <Row justify="center">
      <Spinner />
    </Row>
  ) : !snapshots?.length ? (
    <Label variant="muted">{t('backups.restore.snapshots.empty')}</Label>
  ) : (
    <SourceList>
      {snapshots.map(s => (
        <button
          key={s.name}
          className={`${styles.sourceItem} ${selectedSnapshot === s.name ? styles.selected : ''}`}
          onClick={() => setSelectedSnapshot(s.name)}
        >
          <div
            style={{
              width: 16,
              height: 16,
              borderRadius: '50%',
              border: `2px solid ${selectedSnapshot === s.name ? 'var(--color-primary)' : 'var(--color-border)'}`,
              flexShrink: 0,
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
            }}
          >
            {selectedSnapshot === s.name && (
              <div
                style={{
                  width: 8,
                  height: 8,
                  borderRadius: '50%',
                  background: 'var(--color-primary)',
                }}
              />
            )}
          </div>
          <Stack gap="1" align="flex-start">
            <Label>{s.name}</Label>
            {s.createdAt && <Label variant="muted">{formatDate(s.createdAt)}</Label>}
          </Stack>
        </button>
      ))}
    </SourceList>
  );

  const gitTab = commitsLoading ? (
    <Row justify="center">
      <Spinner />
    </Row>
  ) : !commits?.length ? (
    <Label variant="muted">{t('backups.restore.commits.empty')}</Label>
  ) : (
    <SourceList>
      {commits.map(c => (
        <button
          key={c.sha}
          className={`${styles.sourceItem} ${selectedCommit === c.sha ? styles.selected : ''}`}
          onClick={() => setSelectedCommit(c.sha)}
        >
          <div
            style={{
              width: 16,
              height: 16,
              borderRadius: '50%',
              border: `2px solid ${selectedCommit === c.sha ? 'var(--color-primary)' : 'var(--color-border)'}`,
              flexShrink: 0,
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
            }}
          >
            {selectedCommit === c.sha && (
              <div
                style={{
                  width: 8,
                  height: 8,
                  borderRadius: '50%',
                  background: 'var(--color-primary)',
                }}
              />
            )}
          </div>
          <Stack gap="1" align="flex-start">
            <Label>{c.message}</Label>
            <Label variant="muted">
              {c.sha.slice(0, 7)} · {c.author} · {formatDate(c.timestamp)}
            </Label>
          </Stack>
        </button>
      ))}
    </SourceList>
  );

  return (
    <>
      <Card>
        <CardHeader>
          <CardTitle>{t('backups.restore.title')}</CardTitle>
          <Label variant="muted">{t('backups.restore.description')}</Label>
        </CardHeader>
        <CardContent>
          <Tabs
            activeTab={activeTab}
            onChange={tab => {
              setActiveTab(tab);
            }}
            items={[
              { id: 'snapshot', label: t('backups.restore.tabs.snapshot'), content: snapshotTab },
              { id: 'git', label: t('backups.restore.tabs.git'), content: gitTab },
              {
                id: 'manifest',
                label: t('backups.restore.tabs.manifest'),
                content: <Label variant="muted">{t('backups.restore.manifest.description')}</Label>,
              },
            ]}
          />
          {previewError && <ErrorAlert message={previewError} variant="block" />}
          <div style={{ marginTop: 'var(--space-4)', display: 'flex', justifyContent: 'flex-end' }}>
            <Button
              variant="secondary"
              onClick={handlePreview}
              isLoading={previewLoading}
              disabled={!selectedSource}
            >
              {t('backups.restore.previewButton')}
            </Button>
          </div>
        </CardContent>
      </Card>

      <RestorePreviewModal
        isOpen={modalOpen}
        onClose={handleModalClose}
        preview={preview}
        onConfirm={handleConfirm}
        isConfirming={isConfirming}
        confirmError={confirmError}
        success={success}
        restoreWarnings={restoreWarnings}
      />
    </>
  );
}

export function BackupsPage() {
  const { t } = useTranslation('settings');
  const { data: options, isLoading } = useBackupOptions();

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-4)' }}>
      <Card>
        <CardHeader>
          <CardTitle>{t('backups.title')}</CardTitle>
          <Label variant="muted">{t('backups.description')}</Label>
        </CardHeader>
        <CardContent>
          {isLoading || !options ? (
            <Row justify="center">
              <Spinner />
            </Row>
          ) : (
            <BackupOptionsForm key={JSON.stringify(options)} current={options} />
          )}
        </CardContent>
      </Card>
      <ManualBackupCard />
      <RestoreBackupCard />
    </div>
  );
}
