import { Folder, Mail, MonitorCog, Network, ShieldCheck, Trash, UserPlus } from 'lucide-react';
import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';

import { Row, Spacer, Stack } from '@/components/layout';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/layout/Table';
import { PermissionPresetSelector } from '@/components/settings/PermissionPresetSelector';
import { PermissionsModal } from '@/components/settings/PermissionsModal';
import { RoleSelector } from '@/components/settings/RoleSelector';
import { Badge } from '@/components/ui/Badge';
import { Button } from '@/components/ui/Button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/Card';
import { Input } from '@/components/ui/Input';
import { Label } from '@/components/ui/Label';
import { Modal } from '@/components/ui/Modal';
import { SimpleUserAvatar } from '@/components/ui/SimpleUserAvatar';
import { Spinner } from '@/components/ui/Spinner';
import { Tooltip } from '@/components/ui/Tooltip';
import { useCurrentUser } from '@/hooks/useCurrentUser';
import { usePermission } from '@/hooks/usePermission';
import {
  useAllPermissions,
  useCreateUser,
  useDeleteUser,
  useResendInvite,
  useUsers,
} from '@/hooks/useUsers';

export function UsersPage() {
  const { t } = useTranslation('settings');
  const currentUser = useCurrentUser();
  const { data: users, isLoading } = useUsers();
  const { data: allPermissions } = useAllPermissions();
  const { mutateAsync: createUser, isPending: isCreating, error: createError } = useCreateUser();
  const { mutate: deleteUser } = useDeleteUser();
  const { mutate: resendInvite, isPending: isResendingInvite } = useResendInvite();

  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [email, setEmail] = useState('');
  const [role, setRole] = useState<'user' | 'admin'>('user');
  const [selectedPermissions, setSelectedPermissions] = useState<string[]>([]);
  const [formError, setFormError] = useState<string | undefined>();
  const [permissionsUser, setPermissionsUser] = useState<{
    id: string;
    name: string;
  } | null>(null);
  const canViewUsers = usePermission('system.manage_users');
  const canCreateUser = usePermission('system.manage_users');
  const canDeleteUser = usePermission('system.manage_users');

  const presets = useMemo<Record<string, { permissions: string[] }>>(() => {
    if (!allPermissions)
      return {
        readonly: { permissions: [] },
        developer: { permissions: [] },
        maintainer: { permissions: [] },
      };
    return {
      readonly: {
        permissions: allPermissions.filter(p => p.endsWith('.read')),
      },
      developer: {
        permissions: allPermissions.filter(
          p =>
            !p.endsWith('.delete') &&
            !p.endsWith('.manage_users') &&
            !p.endsWith('.manage_git_credentials')
        ),
      },
      maintainer: {
        permissions: allPermissions,
      },
    };
  }, [allPermissions]);

  const handleCreate = async () => {
    setFormError(undefined);
    try {
      await createUser({
        email,
        isAdmin: role === 'admin',
        permissions: selectedPermissions,
      });
      setEmail('');
      setRole('user');
      setSelectedPermissions([]);
      setIsCreateOpen(false);
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : t('users.errors.createFailed');
      setFormError(message);
    }
  };

  const handleCloseModal = () => {
    setIsCreateOpen(false);
    setEmail('');
    setRole('user');
    setSelectedPermissions([]);
    setFormError(undefined);
  };

  if (!canViewUsers) return null;

  return (
    <>
      <Card>
        <CardHeader>
          <Row justify="space-between">
            <CardTitle>{t('users.title')}</CardTitle>
            {canCreateUser && (
              <Button icon={<UserPlus />} onClick={() => setIsCreateOpen(true)}>
                {t('users.createUser')}
              </Button>
            )}
          </Row>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <Row justify="center">
              <Spinner />
            </Row>
          ) : !users?.length ? (
            <Label variant="muted">{t('users.empty')}</Label>
          ) : (
            <Stack gap="2">
              {users.map((user, i) => (
                <Card key={user.id} padding="var(--space-3)">
                  <Row>
                    <SimpleUserAvatar name={user.name || user.email} />
                    <Stack gap="1">
                      <Label size="md" variant="primary" style={{ fontWeight: 'bold' }}>
                        {user.name || user.email}
                      </Label>
                      <Row gap="1">
                        <Label size="sm" variant="secondary">
                          {user.email}
                        </Label>
                        {user.isAdmin && <Badge variant="primary">{t('users.admin')}</Badge>}
                      </Row>
                    </Stack>
                    <Spacer expand />
                    <Badge variant={user.requirePasswordChange ? 'warning' : 'success'}>
                      {user.requirePasswordChange
                        ? t('users.statuses.pending')
                        : t('users.statuses.active')}
                    </Badge>
                    <Row gap="1">
                      {user.requirePasswordChange && (
                        <Tooltip content={t('users.resendInvite')} direction="left">
                          <Button
                            size="md"
                            disabled={!canCreateUser || isResendingInvite}
                            variant="text"
                            icon={<Mail />}
                            onClick={() => resendInvite(user.id)}
                          />
                        </Tooltip>
                      )}
                      <Tooltip content={t('users.permissionsModal.title')} direction="left">
                        <Button
                          size="md"
                          disabled={!canCreateUser}
                          variant="text"
                          icon={<ShieldCheck />}
                          onClick={() => setPermissionsUser(user)}
                        />
                      </Tooltip>
                      <Tooltip content={t('users.delete')} direction="left">
                        <Button
                          size="md"
                          disabled={!canDeleteUser || user.id === currentUser?.id}
                          variant="text"
                          icon={<Trash color="var(--color-danger)" />}
                          onClick={() => deleteUser(user.id)}
                        />
                      </Tooltip>
                    </Row>
                  </Row>
                </Card>
              ))}
            </Stack>
          )}
        </CardContent>
      </Card>

      <Modal
        isOpen={isCreateOpen}
        onClose={handleCloseModal}
        title={t('users.createModal.title')}
        error={formError}
        footer={
          <Row justify="flex-end">
            <Button variant="secondary" onClick={handleCloseModal}>
              {t('users.createModal.cancel')}
            </Button>
            <Button onClick={handleCreate} disabled={isCreating || !email}>
              {isCreating ? <Spinner /> : t('users.createModal.submit')}
            </Button>
          </Row>
        }
      >
        <Stack gap="4">
          <Stack gap="2">
            <Label>{t('users.createModal.basicInfo')}</Label>
            <Input
              label={t('users.createModal.emailLabel')}
              type="email"
              value={email}
              onChange={e => setEmail(e.target.value)}
              placeholder="john@example.com"
            />
            <Label variant="muted" size="sm">
              {t('users.createModal.inviteHint')}
            </Label>
          </Stack>

          <Stack gap="2">
            <Label>{t('users.createModal.role')}</Label>
            <RoleSelector value={role} onChange={setRole} disabled={isCreating} />
          </Stack>

          {role === 'user' && allPermissions && (
            <Stack gap="2">
              <PermissionPresetSelector
                presets={presets}
                selectedPermissions={selectedPermissions}
                onPresetSelect={setSelectedPermissions}
                disabled={isCreating}
              />
            </Stack>
          )}
        </Stack>
      </Modal>

      {permissionsUser && (
        <PermissionsModal
          userId={permissionsUser.id}
          userName={permissionsUser.name}
          isOpen={!!permissionsUser}
          onClose={() => setPermissionsUser(null)}
          categoryIcons={{
            projects: <Folder />,
            dns: <Network />,
            system: <MonitorCog />,
          }}
        />
      )}
    </>
  );
}
