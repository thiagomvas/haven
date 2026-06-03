import { useState, useEffect, useMemo, ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { Modal } from '@/components/ui/Modal'
import { Button } from '@/components/ui/Button'
import { Spinner } from '@/components/ui/Spinner'
import { Row } from '@/components/layout'
import { useUserPermissions, useSetUserPermissions, useAllPermissions } from '@/hooks/useUsers'
import styles from './PermissionsModal.module.css'

interface Props {
  userId: string
  userName: string
  isOpen: boolean
  onClose: () => void
  categoryIcons?: Record<string, ReactNode>
}

export function PermissionsModal({ userId, userName, isOpen, onClose, categoryIcons = {} }: Props) {
  const { t, i18n } = useTranslation('settings')
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

  const allPermissionKeys = useMemo(() => allPermissions ?? [], [allPermissions])

  const allSelected = allPermissionKeys.length > 0 && allPermissionKeys.every((p) => selected.has(p))
  const someSelected = !allSelected && allPermissionKeys.some((p) => selected.has(p))

  const toggleAll = () => {
    if (allSelected) {
      setSelected(new Set())
    } else {
      setSelected(new Set(allPermissionKeys))
    }
  }

  const toggleModule = (module: string, actions: string[]) => {
    const keys = actions.map((a) => `${module}.${a}`)
    const allOn = keys.every((k) => selected.has(k))
    setSelected((prev) => {
      const next = new Set(prev)
      if (allOn) {
        keys.forEach((k) => next.delete(k))
      } else {
        keys.forEach((k) => next.add(k))
      }
      return next
    })
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
        <>
          <label className={styles.toggleAllRow}>
            <span className={styles.toggleAllLabel}>{t('users.permissionsModal.toggleAll')}</span>
            <span className={styles.toggle}>
              <input
                id="perm-toggle-all"
                type="checkbox"
                className={styles.toggleInput}
                checked={allSelected}
                ref={(el) => { if (el) el.indeterminate = someSelected }}
                onChange={toggleAll}
              />
              <span className={styles.toggleTrack}>
                <span className={styles.toggleThumb} />
              </span>
            </span>
          </label>
          <div className={styles.permissionsContainer}>
            {Object.entries(permissionModules).map(([module, actions]) => {
              const moduleKeys = actions.map((a) => `${module}.${a}`)
              const moduleAllOn = moduleKeys.every((k) => selected.has(k))
              const moduleSomeOn = !moduleAllOn && moduleKeys.some((k) => selected.has(k))
              const moduleToggleId = `perm-module-${module}`
              return (
                <div key={module} className={styles.moduleCard}>
                  <div className={styles.moduleHeader}>
                    <span className={styles.moduleHeaderContent}>
                      {categoryIcons[module] && <span className={styles.moduleIcon}>{categoryIcons[module]}</span>}
                      <span>{t(`users.permissionModules.${module}`)}</span>
                    </span>
                    {!moduleAllOn && (
                      <button
                        type="button"
                        className={styles.actionButton}
                        onClick={() => {
                          setSelected((prev) => {
                            const next = new Set(prev)
                            actions.forEach((a) => next.add(`${module}.${a}`))
                            return next
                          })
                        }}
                      >
                        {t('users.permissionsModal.selectAll')}
                      </button>
                    )}
                    {moduleAllOn && (
                      <button
                        type="button"
                        className={styles.actionButton}
                        onClick={() => {
                          setSelected((prev) => {
                            const next = new Set(prev)
                            actions.forEach((a) => next.delete(`${module}.${a}`))
                            return next
                          })
                        }}
                      >
                        {t('users.permissionsModal.clear')}
                      </button>
                    )}
                  </div>
                  <div className={styles.permissionList}>
                    {actions.map((action) => {
                      const key = `${module}.${action}`
                      const id = `perm-${key}`
                      const descriptionKey = `users.permissions.${module}.${action}_description`
                      const description = t(descriptionKey, { defaultValue: '' })
                      return (
                        <label key={key} htmlFor={id} className={styles.permissionRow}>
                          <span className={styles.permissionContent}>
                            <span className={styles.permissionLabel}>
                              {t(`users.permissions.${module}.${action}`)}
                            </span>
                            {description && (
                              <span className={styles.permissionDescription}>
                                {description}
                              </span>
                            )}
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
              )
            })}
          </div>
        </>
      )}
    </Modal>
  )
}
