import { Pencil, Plus, Trash2 } from 'lucide-react';
import { useCallback, useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';

import { ActionKind, ActionRisk, ActionShell, CustomActionDto } from '@/api/types';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/layout';

import { customActionsApi } from '../../api/customActions';
import { Row, Spacer, Stack } from '../layout';
import { Badge } from '../ui/Badge';
import { Button } from '../ui/Button';
import { Divider } from '../ui/Divider';
import { ErrorAlert } from '../ui/ErrorAlert';
import { IconPicker } from '../ui/IconPicker';
import { Input } from '../ui/Input';
import { Label } from '../ui/Label';
import { LucideIcon } from '../ui/LucideIcon';
import { Modal } from '../ui/Modal';
import { SelectInput } from '../ui/SelectInput';
import { Spinner } from '../ui/Spinner';
import { Textarea } from '../ui/Textarea';
import { CommandArgsEditor } from './CommandArgsEditor';
import {
  actionToForm,
  CustomActionFormState,
  EMPTY_FORM,
  formToInput,
  isFormValid,
} from './customActionForm';

interface CustomActionsEditorProps {
  projectId: string;
  environmentId: string;
  serviceId: string;
}

const HTTP_METHODS = ['GET', 'HEAD', 'POST', 'PUT', 'PATCH', 'DELETE'].map(m => ({
  value: m,
  label: m,
}));

export function CustomActionsEditor({
  projectId,
  environmentId,
  serviceId,
}: CustomActionsEditorProps) {
  const { t } = useTranslation(['services']);

  const [actions, setActions] = useState<CustomActionDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editTarget, setEditTarget] = useState<CustomActionDto | null>(null);
  const [form, setForm] = useState<CustomActionFormState>(EMPTY_FORM);
  const [isSaving, setIsSaving] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  const [deleteTarget, setDeleteTarget] = useState<CustomActionDto | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);

  const load = useCallback(async () => {
    try {
      setLoading(true);
      const result = await customActionsApi.list(projectId, environmentId, serviceId);
      setActions(result ?? []);
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

  const openEditModal = (action: CustomActionDto) => {
    setEditTarget(action);
    setForm(actionToForm(action));
    setFormError(null);
    setIsFormOpen(true);
  };

  const patch = (changes: Partial<CustomActionFormState>) => setForm(p => ({ ...p, ...changes }));

  const handleSubmit = async () => {
    if (!isFormValid(form)) return;
    try {
      setIsSaving(true);
      setFormError(null);
      const input = formToInput(form);
      if (editTarget) {
        await customActionsApi.update(projectId, environmentId, serviceId, editTarget.id, input);
      } else {
        await customActionsApi.create(projectId, environmentId, serviceId, input);
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
      await customActionsApi.delete(projectId, environmentId, serviceId, deleteTarget.id);
      setDeleteTarget(null);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : t('services:error'));
    } finally {
      setIsDeleting(false);
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
          {t('services:customActions.title')}
        </Label>
        {actions.length > 0 && <Badge>{actions.length}</Badge>}
        <Spacer expand direction="horizontal" />
        <Button variant="secondary" size="sm" icon={<Plus size={14} />} onClick={openAddModal}>
          {t('services:customActions.add')}
        </Button>
      </Row>

      {actions.length === 0 ? (
        <Stack gap="3" align="center" style={{ padding: 'var(--space-10) var(--space-4)' }}>
          <Label variant="secondary" size="sm">
            {t('services:customActions.empty')}
          </Label>
          <Button variant="secondary" size="sm" icon={<Plus size={14} />} onClick={openAddModal}>
            {t('services:customActions.addFirst')}
          </Button>
        </Stack>
      ) : (
        <Table hoverable striped>
          <TableHead>
            <TableRow isHeader hasActionsColumn>
              <TableHeader>{t('services:customActions.name')}</TableHeader>
              <TableHeader>{t('services:customActions.alias')}</TableHeader>
              <TableHeader>{t('services:customActions.kind')}</TableHeader>
              <TableHeader>{t('services:customActions.risk')}</TableHeader>
            </TableRow>
          </TableHead>
          <TableBody>
            {actions.map(action => (
              <TableRow
                key={action.id}
                actions={
                  <>
                    <Button
                      variant="text"
                      size="xs"
                      icon={<Pencil size={14} />}
                      onClick={() => openEditModal(action)}
                      title={t('services:customActions.edit')}
                      aria-label={t('services:customActions.edit')}
                    />
                    <Button
                      variant="text"
                      size="xs"
                      icon={<Trash2 size={14} />}
                      onClick={() => setDeleteTarget(action)}
                      title={t('services:customActions.delete')}
                      aria-label={t('services:customActions.delete')}
                    />
                  </>
                }
              >
                <TableCell>
                  <Row gap="2" align="center">
                    <LucideIcon name={action.icon} size={16} />
                    {action.actionName}
                  </Row>
                </TableCell>
                <TableCell variant="mono">{action.alias}</TableCell>
                <TableCell>
                  <Badge>{action.config.$type === 'exec' ? 'Exec' : 'HTTP'}</Badge>
                </TableCell>
                <TableCell>
                  <Badge variant={action.risk === 'Safe' ? 'success' : 'warning'}>
                    {t(`services:customActions.risks.${action.risk}`)}
                  </Badge>
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
          editTarget ? t('services:customActions.editTitle') : t('services:customActions.addTitle')
        }
        size="sm"
        error={formError ?? undefined}
        footer={
          <Row gap="2" justify="flex-end" full>
            <Button variant="ghost" onClick={() => setIsFormOpen(false)} disabled={isSaving}>
              {t('services:customActions.cancel')}
            </Button>
            <Button
              variant="primary"
              onClick={handleSubmit}
              isLoading={isSaving}
              disabled={!isFormValid(form)}
              icon={<Plus size={14} />}
            >
              {editTarget ? t('services:customActions.save') : t('services:customActions.create')}
            </Button>
          </Row>
        }
      >
        <Stack gap="3">
          <Input
            label={t('services:customActions.name') + ' *'}
            value={form.actionName}
            onChange={e => patch({ actionName: e.target.value })}
            placeholder="Clear cache"
            autoFocus
          />
          <Input
            label={t('services:customActions.alias') + ' *'}
            value={form.alias}
            onChange={e => patch({ alias: e.target.value })}
            placeholder="clear-cache"
          />
          <Input
            label={t('services:customActions.description')}
            value={form.actionDescription}
            onChange={e => patch({ actionDescription: e.target.value })}
          />
          <IconPicker
            label={t('services:customActions.icon') + ' *'}
            hint={t('services:customActions.iconHint')}
            value={form.icon}
            onChange={icon => patch({ icon })}
          />
          <SelectInput
            label={t('services:customActions.risk')}
            value={form.risk}
            onChange={v => patch({ risk: v as ActionRisk })}
            options={[
              { value: 'Safe', label: t('services:customActions.risks.Safe') },
              {
                value: 'RequireConfirmation',
                label: t('services:customActions.risks.RequireConfirmation'),
              },
            ]}
          />
          <Input
            label={t('services:customActions.timeoutSeconds')}
            type="number"
            value={form.timeoutSeconds}
            onChange={e => patch({ timeoutSeconds: e.target.value })}
          />
          <Input
            label={t('services:customActions.requiredPermissions')}
            value={form.requiredPermissions}
            onChange={e => patch({ requiredPermissions: e.target.value })}
            placeholder="projects.manage_deploys"
          />

          <Divider />

          <SelectInput
            label={t('services:customActions.kind')}
            value={form.kind}
            onChange={v => patch({ kind: v as ActionKind })}
            options={[
              { value: 'exec', label: t('services:customActions.kinds.exec') },
              { value: 'http', label: t('services:customActions.kinds.http') },
            ]}
          />

          {form.kind === 'exec' ? (
            <>
              <CommandArgsEditor
                commandArgs={form.execCommand}
                onChange={execCommand => patch({ execCommand })}
              />
              <Input
                label={t('services:customActions.exec.workingDir')}
                value={form.execWorkingDir}
                onChange={e => patch({ execWorkingDir: e.target.value })}
                placeholder="/app"
              />
              <Input
                label={t('services:customActions.exec.user')}
                value={form.execUser}
                onChange={e => patch({ execUser: e.target.value })}
              />
              <SelectInput
                label={t('services:customActions.exec.shell')}
                value={form.execShell}
                onChange={v => patch({ execShell: v as ActionShell | '' })}
                options={[
                  { value: '', label: t('services:customActions.exec.noShell') },
                  { value: 'Bash', label: 'Bash' },
                  { value: 'Sh', label: 'Sh' },
                ]}
              />
            </>
          ) : (
            <>
              <SelectInput
                label={t('services:customActions.http.method')}
                value={form.httpMethod}
                onChange={v => patch({ httpMethod: v })}
                options={HTTP_METHODS}
              />
              <Input
                label={t('services:customActions.http.url') + ' *'}
                value={form.httpUrl}
                onChange={e => patch({ httpUrl: e.target.value })}
                placeholder="http://localhost:8080/cache/clear"
              />
              <Textarea
                label={t('services:customActions.http.headers')}
                value={form.httpHeaders}
                onChange={e => patch({ httpHeaders: e.target.value })}
                placeholder="Authorization: Bearer ..."
                rows={3}
              />
              <Textarea
                label={t('services:customActions.http.body')}
                value={form.httpBody}
                onChange={e => patch({ httpBody: e.target.value })}
                rows={3}
              />
              <Input
                label={t('services:customActions.http.successStatusCodes')}
                value={form.httpSuccessCodes}
                onChange={e => patch({ httpSuccessCodes: e.target.value })}
                placeholder="200, 204"
              />
            </>
          )}
        </Stack>
      </Modal>

      <Modal
        isOpen={!!deleteTarget}
        onClose={() => setDeleteTarget(null)}
        title={t('services:customActions.deleteTitle')}
        size="sm"
        footer={
          <Row gap="2" justify="flex-end" full>
            <Button variant="ghost" onClick={() => setDeleteTarget(null)} disabled={isDeleting}>
              {t('services:customActions.cancel')}
            </Button>
            <Button variant="danger" onClick={handleDelete} isLoading={isDeleting}>
              {t('services:customActions.delete')}
            </Button>
          </Row>
        }
      >
        <Label variant="secondary" size="sm">
          {t('services:customActions.deleteConfirm', { name: deleteTarget?.actionName })}
        </Label>
      </Modal>
    </Stack>
  );
}
