import {
  Bell,
  Database,
  FileCode2,
  FolderOpen,
  GitBranch,
  LayoutDashboard,
  PanelRightClose,
  PanelRightOpen,
  Settings,
} from 'lucide-react';
import { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { NavLink } from 'react-router-dom';

import { usePermission } from '@/hooks/usePermission';

import { Button } from '../ui/Button';
import { Tooltip } from '../ui/Tooltip';
import styles from '@/styles/components/layout/Sidebar.module.css';

interface SidebarProps {
  collapsed?: boolean;
  onToggleCollapse?: () => void;
}

interface NavItem {
  to: string;
  icon: ReactNode;
  label: string;
  translationKey: string;
  external?: boolean;
}

export function Sidebar({ collapsed = false, onToggleCollapse }: SidebarProps) {
  const { t } = useTranslation('layout');
  const canViewProjects = usePermission('projects.read');
  const canViewCredentials = usePermission('system.read_git_credentials');
  const canViewNotifications = usePermission('system.read_notifications');

  const mainNavItems: NavItem[] = [
    {
      to: '/dashboard',
      icon: <LayoutDashboard size={20} />,
      label: 'Dashboard',
      translationKey: 'sidebar.dashboard',
    },
    ...(canViewProjects
      ? [
          {
            to: '/projects',
            icon: <FolderOpen size={20} />,
            label: 'Projects',
            translationKey: 'sidebar.projects',
          },
          {
            to: '/service-registry',
            icon: <Database size={20} />,
            label: 'Service Registry',
            translationKey: 'sidebar.serviceRegistry',
          },
        ]
      : []),
  ];

  const systemNavItems: NavItem[] = [
    ...(canViewCredentials
      ? [
          {
            to: '/git-providers',
            icon: <GitBranch size={20} />,
            label: 'Git Providers',
            translationKey: 'sidebar.gitProviders',
          },
        ]
      : []),
    ...(canViewNotifications
      ? [
          {
            to: '/notification-channels',
            icon: <Bell size={20} />,
            label: 'Notifications',
            translationKey: 'sidebar.notifications',
          },
        ]
      : []),
  ];

  const scalarUrl = `${import.meta.env.DEV ? (import.meta.env.VITE_API_URL ?? '') : ''}/scalar/`;

  const helpNavItems: NavItem[] = [
    {
      to: scalarUrl,
      icon: <FileCode2 size={20} />,
      label: 'API Reference',
      translationKey: 'sidebar.apiReference',
      external: true,
    },
  ];

  const NavLinkContent = ({ icon, label }: { icon: ReactNode; label: string }) => (
    <>
      {collapsed ? (
        <Tooltip content={label}>{icon}</Tooltip>
      ) : (
        <>
          {icon}
          <span>{label}</span>
        </>
      )}
    </>
  );

  const renderNavItem = (item: NavItem) =>
    item.external ? (
      <a
        key={item.to}
        href={item.to}
        target="_blank"
        rel="noopener noreferrer"
        className={styles.navLink}
      >
        <NavLinkContent icon={item.icon} label={t(item.translationKey as any)} />
      </a>
    ) : (
      <NavLink
        key={item.to}
        to={item.to}
        className={({ isActive }) => `${styles.navLink} ${isActive ? styles.navLinkActive : ''}`}
      >
        <NavLinkContent icon={item.icon} label={t(item.translationKey as any)} />
      </NavLink>
    );

  const renderSection = (items: NavItem[]) => items.map(renderNavItem);

  return (
    <aside className={`${styles.sidebar} ${collapsed ? styles.sidebarCollapsed : ''}`}>
      <div className={styles.sidebarHeader}>
        <Button
          variant="ghost"
          size="sm"
          onClick={onToggleCollapse}
          title={t('header.toggleSidebar')}
          className={styles.toggleButton}
        >
          {collapsed ? <PanelRightOpen size={20} /> : <PanelRightClose size={20} />}
        </Button>
      </div>
      <nav className={styles.navContainer}>
        <div className={styles.navSection}>
          <div className={styles.sectionTitle}>{collapsed ? '' : t('sidebar.main')}</div>
          <div className={styles.navItems}>{renderSection(mainNavItems)}</div>
        </div>

        {systemNavItems.length > 0 && (
          <div className={styles.navSection}>
            <div className={styles.sectionTitle}>{collapsed ? '' : t('sidebar.system')}</div>
            <div className={styles.navItems}>{renderSection(systemNavItems)}</div>
          </div>
        )}
        {helpNavItems.length > 0 && (
          <div className={styles.navSection}>
            <div className={styles.sectionTitle}>{collapsed ? '' : t('sidebar.help')}</div>
            <div className={styles.navItems}>{renderSection(helpNavItems)}</div>
          </div>
        )}
      </nav>

      <div className={styles.sidebarFooter}>
        <NavLink
          to="/settings"
          className={({ isActive }) =>
            `${styles.configButton} ${isActive ? styles.configButtonActive : ''}`
          }
          title={t('sidebar.settings')}
          aria-label={t('sidebar.settings')}
        >
          <Tooltip content={t('sidebar.settings')}>
            <Settings size={20} />
          </Tooltip>
        </NavLink>
      </div>
    </aside>
  );
}
