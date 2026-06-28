import { LogOut } from 'lucide-react';
import { useEffect, useRef, useState } from 'react';
import { createPortal } from 'react-dom';
import { useNavigate } from 'react-router-dom';

import { MeResponse } from '@/api/auth';
import { authApi } from '@/api/auth';
import { tokenStorage } from '@/lib/tokenStorage';

import styles from './UserAvatar.module.css';

interface UserAvatarProps {
  user: MeResponse;
}

function getColorFromName(name: string): string {
  let hash = 0;
  for (let i = 0; i < name.length; i++) {
    hash = name.charCodeAt(i) + ((hash << 5) - hash);
  }
  return `hsl(${Math.abs(hash) % 360}, 60%, 45%)`;
}

export function UserAvatar({ user }: UserAvatarProps) {
  const [open, setOpen] = useState(false);
  const [position, setPosition] = useState({ top: 0, right: 0 });
  const buttonRef = useRef<HTMLButtonElement>(null);
  const dropdownRef = useRef<HTMLDivElement>(null);
  const navigate = useNavigate();

  const handleToggle = () => {
    if (!open && buttonRef.current) {
      const rect = buttonRef.current.getBoundingClientRect();
      setPosition({
        top: rect.bottom + 8,
        right: window.innerWidth - rect.right,
      });
    }
    setOpen(v => !v);
  };

  useEffect(() => {
    if (!open) return;
    const close = (e: MouseEvent) => {
      const target = e.target as Node;
      const outsideButton = !buttonRef.current?.contains(target);
      const outsideDropdown = !dropdownRef.current?.contains(target);
      if (outsideButton && outsideDropdown) setOpen(false);
    };
    document.addEventListener('mousedown', close);
    return () => document.removeEventListener('mousedown', close);
  }, [open]);

  const handleLogout = async () => {
    try {
      await authApi.logout();
    } finally {
      tokenStorage.clear();
      navigate('/login', { replace: true });
    }
  };

  const initials = user.name
    .split(' ')
    .map(w => w[0])
    .slice(0, 2)
    .join('')
    .toUpperCase();

  return (
    <>
      <button
        ref={buttonRef}
        className={styles.avatar}
        style={{ backgroundColor: getColorFromName(user.name) }}
        onClick={handleToggle}
        title={user.name}
      >
        {initials}
      </button>

      {open &&
        createPortal(
          <div
            ref={dropdownRef}
            className={styles.dropdown}
            style={{ top: position.top, right: position.right }}
          >
            <div className={styles.dropdownHeader}>
              <div className={styles.dropdownName}>{user.name}</div>
              <div className={styles.dropdownEmail}>{user.email}</div>
            </div>
            <button className={`${styles.dropdownItem} ${styles.danger}`} onClick={handleLogout}>
              <LogOut size={14} />
              Sign out
            </button>
          </div>,
          document.body
        )}
    </>
  );
}
