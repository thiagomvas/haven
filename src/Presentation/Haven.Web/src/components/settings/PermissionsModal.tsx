import { useState, useEffect, useMemo, ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { Modal } from '@/components/ui/Modal';
import { Button } from '@/components/ui/Button';
import { Spinner } from '@/components/ui/Spinner';
import { Row, Stack } from '@/components/layout';
import { Badge } from '@/components/ui/Badge';
import { useUserPermissions, useSetUserPermissions, useAllPermissions } from '@/hooks/useUsers';
import styles from './PermissionsModal.module.css';
import { Divider } from '../ui/Divider';

interface Props {
  userId: string;
  userName: string;
  isOpen: boolean;
  onClose: () => void;
  categoryIcons?: Record<string, ReactNode>;
}

export function PermissionsModal({ userId, userName, isOpen, onClose, categoryIcons = {} }: Props) {
  const { t, i18n } = useTranslation('settings');
  const { data: currentPermissions, isLoading: isLoadingPermissions } = useUserPermissions(
    isOpen ? userId : null
  );
  const { data: allPermissions, isLoading: isLoadingAll } = useAllPermissions();
  const { mutateAsync: setPermissions, isPending } = useSetUserPermissions();

  const [selected, setSelected] = useState<Set<string>>(new Set());
  const [error, setError] = useState<string | undefined>();

  const isLoading = isLoadingPermissions || isLoadingAll;

  const permissionModules = useMemo(() => {
    if (!allPermissions) return {};
    const modules: Record<string, string[]> = {};
    for (const perm of allPermissions) {
      const dot = perm.indexOf('.');
      if (dot === -1) continue;
      const module = perm.slice(0, dot);
      const action = perm.slice(dot + 1);
      if (!modules[module]) modules[module] = [];
      modules[module].push(action);
    }
    return modules;
  }, [allPermissions]);

  const isDestructivePermission = (permission: string): boolean => {
    const destructiveActions = ['delete', 'manage_users', 'manage_git_credentials'];
    return destructiveActions.some(action => permission.endsWith(`.${action}`));
  };

  const presets = useMemo(() => {
    if (!allPermissions) return {};
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

  const applyPreset = (presetKey: string) => {
    const preset = presets[presetKey as keyof typeof presets];
    if (preset) {
      setSelected(new Set(preset.permissions));
    }
  };

  useEffect(() => {
    if (currentPermissions) {
      setSelected(new Set(currentPermissions));
    }
  }, [currentPermissions]);

  const toggle = (permission: string) => {
    setSelected(prev => {
      const next = new Set(prev);
      if (next.has(permission)) {
        next.delete(permission);
      } else {
        next.add(permission);
      }
      return next;
    });
  };

  const handleSave = async () => {
    setError(undefined);
    try {
      await setPermissions({ userId, permissions: Array.from(selected) });
      onClose();
    } catch (err) {
      setError(err instanceof Error ? err.message : t('users.permissionsModal.saveFailed'));
    }
  };

  const handleClose = () => {
    setError(undefined);
    onClose();
  };

  const allPermissionKeys = useMemo(() => allPermissions ?? [], [allPermissions]);

  const allSelected = allPermissionKeys.length > 0 && allPermissionKeys.every(p => selected.has(p));
  const someSelected = !allSelected && allPermissionKeys.some(p => selected.has(p));

  const toggleAll = () => {
    if (allSelected) {
      setSelected(new Set());
    } else {
      setSelected(new Set(allPermissionKeys));
    }
  };

  const toggleModule = (module: string, actions: string[]) => {
    const keys = actions.map(a => `${module}.${a}`);
    const allOn = keys.every(k => selected.has(k));
    setSelected(prev => {
      const next = new Set(prev);
      if (allOn) {
        keys.forEach(k => next.delete(k));
      } else {
        keys.forEach(k => next.add(k));
      }
      return next;
    });
  };

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
            <div className={styles.toggleAllLabel}>{t('users.permissionsModal.toggleAll')}</div>
            <div className={styles.toggle}>
              <input
                id="perm-toggle-all"
                type="checkbox"
                className={styles.toggleInput}
                checked={allSelected}
                ref={el => {
                  if (el) el.indeterminate = someSelected;
                }}
                onChange={toggleAll}
              />
              <div className={styles.toggleTrack}>
                <div className={styles.toggleThumb} />
              </div>
            </div>
          </label>

          <Stack gap="2" className={styles.presetsSection}>
            <div className={styles.presetsGrid}>
              {Object.entries(presets).map(([key]) => (
                <button
                  key={key}
                  type="button"
                  className={styles.presetButton}
                  onClick={() => applyPreset(key)}
                >
                  <Stack gap="2">
                    <div className={styles.presetTitle}>
                      {t(`users.createModal.presets.${key}.title` as any)}
                    </div>
                    <div className={styles.presetDescription}>
                      {t(`users.createModal.presets.${key}.description` as any)}
                    </div>
                  </Stack>
                </button>
              ))}
            </div>
          </Stack>

          <div className={styles.permissionsContainer}>
            {Object.entries(permissionModules).map(([module, actions]) => {
              const moduleKeys = actions.map(a => `${module}.${a}`);
              const moduleAllOn = moduleKeys.every(k => selected.has(k));
              const moduleSomeOn = !moduleAllOn && moduleKeys.some(k => selected.has(k));
              const moduleToggleId = `perm-module-${module}`;
              return (
                <div key={module} className={styles.moduleCard}>
                  <Row justify="space-between" className={styles.moduleHeader}>
                    <Row gap="2" className={styles.moduleHeaderContent}>
                      {categoryIcons[module] && (
                        <div className={styles.moduleIcon}>{categoryIcons[module]}</div>
                      )}
                      <div>{t(`users.permissionModules.${module}` as any)}</div>
                    </Row>
                    {!moduleAllOn && (
                      <button
                        type="button"
                        className={styles.actionButton}
                        onClick={() => {
                          setSelected(prev => {
                            const next = new Set(prev);
                            actions.forEach(a => next.add(`${module}.${a}`));
                            return next;
                          });
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
                          setSelected(prev => {
                            const next = new Set(prev);
                            actions.forEach(a => next.delete(`${module}.${a}`));
                            return next;
                          });
                        }}
                      >
                        {t('users.permissionsModal.clear')}
                      </button>
                    )}
                  </Row>
                  <Stack gap="1" className={styles.permissionList}>
                    {actions.map((action, index) => {
                      const key = `${module}.${action}`;
                      const id = `perm-${key}`;
                      const descriptionKey = `users.permissions.${module}.${action}_description`;
                      const description = t(descriptionKey, { defaultValue: '' });
                      return (
                        <Stack key={key} gap="1">
                          <label htmlFor={id} className={styles.permissionRow}>
                            <div className={styles.permissionContent}>
                              <Row gap="2" align="center">
                                <div className={styles.permissionLabel}>
                                  {t(`users.permissions.${module}.${action}` as any)}
                                </div>
                                {isDestructivePermission(key) && (
                                  <Badge variant="danger">
                                    {t('users.permissionsModal.destructive')}
                                  </Badge>
                                )}
                              </Row>
                              {description && (
                                <div className={styles.permissionDescription}>{description}</div>
                              )}
                            </div>
                            <div className={styles.toggle}>
                              <input
                                id={id}
                                type="checkbox"
                                className={styles.toggleInput}
                                checked={selected.has(key)}
                                onChange={() => toggle(key)}
                              />
                              <div className={styles.toggleTrack}>
                                <div className={styles.toggleThumb} />
                              </div>
                            </div>
                          </label>
                          {index < actions.length - 1 && <Divider variant="dashed" />}
                        </Stack>
                      );
                    })}
                  </Stack>
                </div>
              );
            })}
          </div>
        </>
      )}
    </Modal>
  );
}
