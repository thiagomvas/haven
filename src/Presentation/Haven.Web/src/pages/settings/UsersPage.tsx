import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { UserPlus, Trash2, ShieldCheck } from 'lucide-react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/Card'
import { Badge } from '@/components/ui/Badge'
import { Button } from '@/components/ui/Button'
import { Input } from '@/components/ui/Input'
import { Label } from '@/components/ui/Label'
import { Modal } from '@/components/ui/Modal'
import { Spinner } from '@/components/ui/Spinner'
import { Row } from '@/components/layout'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/layout/Table'
import { useCurrentUser } from '@/hooks/useCurrentUser'
import { usePermission } from '@/hooks/usePermission'
import { useUsers, useCreateUser, useDeleteUser } from '@/hooks/useUsers'
import { PermissionsModal } from '@/components/settings/PermissionsModal'

export function UsersPage() {
  const { t } = useTranslation('settings')
  const currentUser = useCurrentUser()
  const { data: users, isLoading } = useUsers()
  const { mutateAsync: createUser, isPending: isCreating, error: createError } = useCreateUser()
  const { mutate: deleteUser } = useDeleteUser()

  const [isCreateOpen, setIsCreateOpen] = useState(false)
  const [name, setName] = useState('')
  const [email, setEmail] = useState('')
  const [temporaryPassword, setTemporaryPassword] = useState('')
  const [formError, setFormError] = useState<string | undefined>()
  const [permissionsUser, setPermissionsUser] = useState<{ id: string; name: string } | null>(null)
  const canViewUsers = usePermission('users.view')
  const canDeleteUser = usePermission('users.delete')

  const handleCreate = async () => {
    setFormError(undefined)
    try {
      await createUser({ name, email, temporaryPassword })
      setName('')
      setEmail('')
      setTemporaryPassword('')
      setIsCreateOpen(false)
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : t('users.errors.createFailed')
      setFormError(message)
    }
  }

  const handleCloseModal = () => {
    setIsCreateOpen(false)
    setName('')
    setEmail('')
    setTemporaryPassword('')
    setFormError(undefined)
  }

  if (!canViewUsers) return null

  return (
    <>
      <Card>
        <CardHeader>
          <Row justify="space-between">
            <CardTitle>{t('users.title')}</CardTitle>
            {currentUser?.isAdmin && (
              <Button icon={<UserPlus />} onClick={() => setIsCreateOpen(true)}>
                {t('users.createUser')}
              </Button>
            )}
          </Row>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <Row justify="center"><Spinner /></Row>
          ) : !users?.length ? (
            <Label variant="muted">{t('users.empty')}</Label>
          ) : (
            <Table>
              <TableHead>
                <TableRow isHeader>
                  <TableHeader>{t('users.table.name')}</TableHeader>
                  <TableHeader>{t('users.table.email')}</TableHeader>
                  <TableHeader>{t('users.table.role')}</TableHeader>
                  <TableHeader>{t('users.table.status')}</TableHeader>
                  {(currentUser?.isAdmin || canDeleteUser) && <TableHeader>{t('users.table.actions')}</TableHeader>}
                </TableRow>
              </TableHead>
              <TableBody>
                {users.map((user) => (
                  <TableRow key={user.id}>
                    <TableCell>{user.name}</TableCell>
                    <TableCell variant="muted">{user.email}</TableCell>
                    <TableCell>
                      <Badge variant={user.isAdmin ? 'warning' : 'default'}>
                        {user.isAdmin ? t('users.roles.admin') : t('users.roles.user')}
                      </Badge>
                    </TableCell>
                    <TableCell>
                      <Badge variant={user.requirePasswordChange ? 'warning' : 'success'}>
                        {user.requirePasswordChange ? t('users.statuses.pending') : t('users.statuses.active')}
                      </Badge>
                    </TableCell>
                    {(currentUser?.isAdmin || canDeleteUser) && (
                      <TableCell>
                        <Row gap="2">
                          {currentUser?.isAdmin && (
                            <Button
                              variant="secondary"
                              size="sm"
                              icon={<ShieldCheck />}
                              disabled={user.isAdmin}
                              onClick={() => setPermissionsUser({ id: user.id, name: user.name })}
                            >
                              {t('users.managePermissions')}
                            </Button>
                          )}
                          {canDeleteUser && (
                            <Button
                              variant="danger"
                              size="sm"
                              icon={<Trash2 />}
                              disabled={user.id === currentUser?.id}
                              onClick={() => deleteUser(user.id)}
                            >
                              {t('users.delete')}
                            </Button>
                          )}
                        </Row>
                      </TableCell>
                    )}
                  </TableRow>
                ))}
              </TableBody>
            </Table>
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
            <Button onClick={handleCreate} disabled={isCreating || !name || !email || !temporaryPassword}>
              {isCreating ? <Spinner /> : t('users.createModal.submit')}
            </Button>
          </Row>
        }
      >
        <Input
          label={t('users.createModal.nameLabel')}
          value={name}
          onChange={(e) => setName(e.target.value)}
          placeholder="John Doe"
        />
        <Input
          label={t('users.createModal.emailLabel')}
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          placeholder="john@example.com"
        />
        <Input
          label={t('users.createModal.temporaryPasswordLabel')}
          type="password"
          value={temporaryPassword}
          onChange={(e) => setTemporaryPassword(e.target.value)}
          placeholder="Min. 8 characters"
        />
      </Modal>

      {permissionsUser && (
        <PermissionsModal
          userId={permissionsUser.id}
          userName={permissionsUser.name}
          isOpen={!!permissionsUser}
          onClose={() => setPermissionsUser(null)}
        />
      )}
    </>
  )
}
