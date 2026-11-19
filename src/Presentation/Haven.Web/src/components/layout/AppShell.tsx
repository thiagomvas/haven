import { Moon, Sun } from 'lucide-react';
import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Outlet, useNavigate } from 'react-router-dom';

import { useCurrentUser } from '@/hooks/useCurrentUser';
import { useTheme } from '@/hooks/useTheme';
import styles from '@/styles/components/layout/AppShell.module.css';

import { Button } from '../ui/Button';
import { UserAvatar } from '../ui/UserAvatar';
import { Breadcrumb } from './Breadcrumb';
import { FuzzySearchBar } from './FuzzySearchBar';
import { Header } from './Header';
import { Sidebar } from './Sidebar';

export function AppShell() {
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false);
  const { t } = useTranslation('layout');
  const { theme, toggleTheme } = useTheme();
  const navigate = useNavigate();
  const user = useCurrentUser();

  useEffect(() => {
    if (user?.requirePasswordChange) {
      navigate('/set-password', { replace: true });
    }
  }, [user, navigate]);

  const headerRight = (
    <div className={styles.headerActions}>
      <Button
        variant="ghost"
        size="sm"
        onClick={toggleTheme}
        title={theme === 'light' ? t('header.switchToDark') : t('header.switchToLight')}
        aria-label={theme === 'light' ? t('header.switchToDark') : t('header.switchToLight')}
      >
        {theme === 'light' ? <Moon size={16} /> : <Sun size={16} />}
      </Button>
      {user && <UserAvatar user={user} />}
    </div>
  );

  return (
    <div className={styles.container}>
      <Sidebar
        collapsed={sidebarCollapsed}
        onToggleCollapse={() => setSidebarCollapsed(!sidebarCollapsed)}
      />
      <div className={`${styles.main} ${sidebarCollapsed ? styles.mainCollapsed : ''}`}>
        <Header left={<Breadcrumb />} center={<FuzzySearchBar />} right={headerRight} />
        <main className={styles.content}>
          <Outlet />
        </main>
      </div>
    </div>
  );
}
