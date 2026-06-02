import { useState, useEffect, useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { Modal } from '@/components/ui/Modal'
import { Button } from '@/components/ui/Button'
import { Spinner } from '@/components/ui/Spinner'
import { Row, Grid } from '@/components/layout'
import { useUserPermissions, useSetUserPermissions, useAllPermissions } from '@/hooks/useUsers'
import styles from './PermissionsModal.module.css'

interface Props {
  userId: string
  userName: string
  isOpen: boolean
  onClose: () => void
}

export function PermissionsModal({ userId, userName, isOpen, onClose }: Props) {
  const { t } = useTranslation('settings')
  const { data: currentPermissions, isLoading: isLoadingPermissions } = useUserPermissions(isOpen ? userId : null)
  const { data: allPermissions, isLoading: isLoadingAll } = useAllPermissions()
  const { mutateAsync: setPermissions, isPending } = useSetUserPermissions()

  const [selected, setSelected] = useState<Set<string>>(new Set())
  const [error, setError] = useState<string | undefined>()

  const isLoading = isLoadingPermissions || isLoadingAll

  const permissionModules = useMemo(() => {
    if (!allPermissions) return {}
    const modules: Record<string, string[]> = {}
    for (const perm of allPermissions) {
      const dot = perm.indexOf('.')
      if (dot === -1) continue
      const module = perm.slice(0, dot)
      const action = perm.slice(dot + 1)
      if (!modules[module]) modules[module] = []
      modules[module].push(action)
    }
    return modules
  }, [allPermissions])

  useEffect(() => {
    if (currentPermissions) {
      setSelected(new Set(currentPermissions))
    }
  }, [currentPermissions])

  const toggle = (permission: string) => {
    setSelected((prev) => {
      const next = new Set(prev)
      if (next.has(permission)) {
        next.delete(permission)
      } else {
        next.add(permission)
      }
      return next
    })
  }

  const handleSave = async () => {
    setError(undefined)
    try {
      await setPermissions({ userId, permissions: Array.from(selected) })
      onClose()
    } catch (err) {
      setError(err instanceof Error ? err.message : t('users.permissionsModal.saveFailed'))
    }
  }

  const handleClose = () => {
    setError(undefined)
    onClose()
  }

  return (
    <Modal
      isOpen={isOpen}
      onClose={handleClose}
      title={t('users.permissionsModal.title')}
      description={t('users.permissionsModal.description', { name: userName })}
      size="lg"
      error={error}
      footer={
        <Row justify="flex-end">
          <Button variant="secondary" onClick={handleClose}>
            {t('users.permissionsModal.cancel')}
          </Button>
          <Button onClick={handleSave} disabled={isPending || isLoading}>
            {isPending ? <Spinner /> : t('users.permissionsModal.save')}
          </Button>
        </Row>
      }
    >
      {isLoading ? (
        <Row justify="center">
          <Spinner />
        </Row>
      ) : (
        <Grid columns={2} gap="4">
          {Object.entries(permissionModules).map(([module, actions]) => (
            <div key={module} className={styles.moduleCard}>
              <div className={styles.moduleHeader}>
                {t(`users.permissionModules.${module}`)}
              </div>
              <div className={styles.permissionList}>
                {actions.map((action) => {
                  const key = `${module}.${action}`
                  const id = `perm-${key}`
                  return (
                    <label key={key} htmlFor={id} className={styles.permissionRow}>
                      <span className={styles.permissionLabel}>
                        {t(`users.permissions.${module}.${action}`)}
                      </span>
                      <span className={styles.toggle}>
                        <input
                          id={id}
                          type="checkbox"
                          className={styles.toggleInput}
                          checked={selected.has(key)}
                          onChange={() => toggle(key)}
                        />
                        <span className={styles.toggleTrack}>
                          <span className={styles.toggleThumb} />
                        </span>
                      </span>
                    </label>
                  )
                })}
              </div>
            </div>
          ))}
        </Grid>
      )}
    </Modal>
  )
}
