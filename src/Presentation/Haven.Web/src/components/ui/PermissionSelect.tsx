import { ChevronDown, X } from 'lucide-react';
import { useEffect, useMemo, useRef, useState } from 'react';
import { createPortal } from 'react-dom';
import { useTranslation } from 'react-i18next';

import { Checkbox } from '@/components/ui/Checkbox';
import { useFloatingPosition } from '@/hooks/useFloatingPosition';
import { usePermissionOptions } from '@/hooks/useUsers';
import styles from '@/styles/components/ui/PermissionSelect.module.css';

interface PermissionSelectProps {
  label?: string;
  value: string[];
  onChange: (value: string[]) => void;
  /** Allow selecting more than one permission. Defaults to true. */
  multiple?: boolean;
  placeholder?: string;
  disabled?: boolean;
  required?: boolean;
  /** Only list permissions the current user holds. Defaults to false. */
  onlyAttributed?: boolean;
}

export function PermissionSelect({
  label,
  value,
  onChange,
  multiple = true,
  placeholder = 'Select permissions…',
  disabled,
  required,
  onlyAttributed = false,
}: PermissionSelectProps) {
  const { data: permissions = [], isLoading } = usePermissionOptions(onlyAttributed);
  const { t } = useTranslation('common');
  const [open, setOpen] = useState(false);
  const [search, setSearch] = useState('');
  const triggerRef = useRef<HTMLButtonElement>(null);
  const dropdownRef = useRef<HTMLDivElement>(null);
  const position = useFloatingPosition(open, triggerRef);

  useEffect(() => {
    if (!open) return;
    const handler = (e: MouseEvent) => {
      const target = e.target as Node;
      if (triggerRef.current?.contains(target) || dropdownRef.current?.contains(target)) return;
      setOpen(false);
      setSearch('');
    };
    document.addEventListener('mousedown', handler);
    return () => document.removeEventListener('mousedown', handler);
  }, [open]);

  const labelOf = (permission: string) =>
    t(`permissions.${permission}` as any, { defaultValue: permission }) as string;
  const descriptionOf = (permission: string) =>
    t(`permissions.${permission}_description` as any, { defaultValue: '' }) as string;

  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase();
    if (!q) return permissions;
    return permissions.filter(
      p =>
        p.toLowerCase().includes(q) ||
        labelOf(p).toLowerCase().includes(q) ||
        descriptionOf(p).toLowerCase().includes(q)
    );
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [permissions, search, t]);

  const groups = useMemo(() => {
    const byModule = new Map<string, string[]>();
    for (const p of filtered) {
      const module = p.split('.')[0];
      byModule.set(module, [...(byModule.get(module) ?? []), p]);
    }
    return [...byModule.entries()];
  }, [filtered]);

  const toggle = (permission: string) => {
    if (!multiple) {
      onChange([permission]);
      setOpen(false);
      setSearch('');
      return;
    }
    onChange(
      value.includes(permission) ? value.filter(v => v !== permission) : [...value, permission]
    );
  };

  const allFilteredSelected = filtered.length > 0 && filtered.every(p => value.includes(p));
  const toggleAllFiltered = () => {
    onChange(
      allFilteredSelected
        ? value.filter(v => !filtered.includes(v))
        : [...value, ...filtered.filter(p => !value.includes(p))]
    );
  };

  return (
    <div className={styles.wrapper}>
      {label && (
        <label className={styles.label}>
          {label}
          {required && <span className={styles.required}>*</span>}
        </label>
      )}
      <button
        ref={triggerRef}
        type="button"
        className={styles.trigger}
        onClick={() => setOpen(o => !o)}
        disabled={disabled}
        aria-haspopup="listbox"
        aria-expanded={open}
      >
        {value.length === 0 ? (
          <span className={styles.placeholder}>{isLoading ? 'Loading…' : placeholder}</span>
        ) : (
          <span className={styles.tags}>
            {value.map(v => (
              <span key={v} className={styles.tag}>
                {labelOf(v)}
                {!disabled && (
                  <span
                    role="button"
                    aria-label={`Remove ${v}`}
                    className={styles.tagRemove}
                    onClick={e => {
                      e.stopPropagation();
                      onChange(value.filter(x => x !== v));
                    }}
                  >
                    <X size={12} />
                  </span>
                )}
              </span>
            ))}
          </span>
        )}
        <ChevronDown size={15} className={`${styles.chevron} ${open ? styles.chevronOpen : ''}`} />
      </button>

      {open &&
        position &&
        createPortal(
          <div
            ref={dropdownRef}
            className={styles.dropdown}
            style={{
              top: position.top,
              left: position.left,
              width: position.width,
              maxHeight: position.maxHeight + 48,
            }}
          >
            <input
              autoFocus
              className={styles.search}
              placeholder="Search permissions…"
              value={search}
              onChange={e => setSearch(e.target.value)}
            />
            <div className={styles.list} role="listbox" aria-multiselectable={multiple}>
              {multiple && filtered.length > 0 && (
                <Checkbox
                  label={search ? 'Select all matching' : 'Select all'}
                  checked={allFilteredSelected}
                  onChange={toggleAllFiltered}
                />
              )}
              {groups.map(([module, perms]) => (
                <div key={module} className={styles.group}>
                  <div className={styles.groupTitle}>
                    {t(`permissionModules.${module}` as any, { defaultValue: module })}
                  </div>
                  {perms.map(p => (
                    <Checkbox
                      key={p}
                      label={labelOf(p)}
                      description={descriptionOf(p) || undefined}
                      checked={value.includes(p)}
                      onChange={() => toggle(p)}
                    />
                  ))}
                </div>
              ))}
              {filtered.length === 0 && <div className={styles.empty}>No permissions found</div>}
            </div>
          </div>,
          document.body
        )}
    </div>
  );
}
