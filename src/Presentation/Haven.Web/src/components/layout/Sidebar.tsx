import {
  Bell,
  Boxes,
  Clock,
  Database,
  FileCode2,
  FolderOpen,
  GitBranch,
  LayoutDashboard,
  Network,
  PanelRightClose,
  PanelRightOpen,
  Settings,
} from 'lucide-react';
import { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { NavLink } from 'react-router-dom';

import { usePermission } from '@/hooks/usePermission';
import styles from '@/styles/components/layout/Sidebar.module.css';

import { Tooltip } from '../ui/Tooltip';
import { UpdateAvailableButton } from './UpdateAvailableButton';

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
  const canViewNetworks = usePermission('dns.read');
  const canViewSidecars = usePermission('sidecars.read');
  const canViewJobs = usePermission('jobs.read');

  const mainNavItems: NavItem[] = [
    {
      to: '/dashboard',
      icon: <LayoutDashboard size={18} />,
      label: 'Dashboard',
      translationKey: 'sidebar.dashboard',
    },
    ...(canViewProjects
      ? [
          {
            to: '/projects',
            icon: <FolderOpen size={18} />,
            label: 'Projects',
            translationKey: 'sidebar.projects',
          },
          {
            to: '/service-registry',
            icon: <Database size={18} />,
            label: 'Service Registry',
            translationKey: 'sidebar.serviceRegistry',
          },
        ]
      : []),
    ...(canViewNetworks
      ? [
          {
            to: '/networks',
            icon: <Network size={18} />,
            label: 'Networks',
            translationKey: 'sidebar.networks',
          },
        ]
      : []),
    ...(canViewSidecars
      ? [
          {
            to: '/sidecars',
            icon: <Boxes size={18} />,
            label: 'Sidecars',
            translationKey: 'sidebar.sidecars',
          },
        ]
      : []),
  ];

  const systemNavItems: NavItem[] = [
    ...(canViewCredentials
      ? [
          {
            to: '/git-providers',
            icon: <GitBranch size={18} />,
            label: 'Git Providers',
            translationKey: 'sidebar.gitProviders',
          },
        ]
      : []),
    ...(canViewNotifications
      ? [
          {
            to: '/notification-channels',
            icon: <Bell size={18} />,
            label: 'Notifications',
            translationKey: 'sidebar.notifications',
          },
        ]
      : []),
    ...(canViewJobs
      ? [
          {
            to: '/jobs',
            icon: <Clock size={18} />,
            label: 'Jobs',
            translationKey: 'sidebar.jobs',
          },
        ]
      : []),
  ];

  const scalarUrl = `${import.meta.env.DEV ? (import.meta.env.VITE_API_URL ?? '') : ''}/scalar/`;

  const helpNavItems: NavItem[] = [
    {
      to: scalarUrl,
      icon: <FileCode2 size={18} />,
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
        {!collapsed && (
          <NavLink to="/dashboard" className={styles.brand} aria-label={t('header.brand')}>
            <svg className={styles.brandMark} viewBox="0 0 100 100" aria-hidden="true">
              <path fill="currentColor" d="m50 0 43.3 25v50L50 100 6.7 75V25z" />
              <g className={styles.brandGlyph} fill="none" strokeWidth="2" strokeLinecap="round">
                <path d="M30 33v12.5m0 9v13M70 33v12.5m0 9v13M34.5 50H43m14 0h8.5" />
                <circle cx="30" cy="28" r="4.5" />
                <circle cx="70" cy="28" r="4.5" />
                <circle cx="30" cy="72" r="4.5" />
                <circle cx="70" cy="72" r="4.5" />
                <circle cx="30" cy="50" r="4.5" />
                <circle cx="70" cy="50" r="4.5" />
                <circle cx="50" cy="50" r="7" className={styles.brandCore} />
              </g>
            </svg>
            <span className={styles.brandName}>{t('header.brand')}</span>
          </NavLink>
        )}
        <button
          type="button"
          onClick={onToggleCollapse}
          title={t('header.toggleSidebar')}
          aria-label={t('header.toggleSidebar')}
          aria-expanded={!collapsed}
          className={styles.toggleButton}
        >
          {collapsed ? <PanelRightOpen size={18} /> : <PanelRightClose size={18} />}
        </button>
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
        <UpdateAvailableButton collapsed={collapsed} />
        <NavLink
          to="/settings"
          className={({ isActive }) =>
            `${styles.configButton} ${isActive ? styles.configButtonActive : ''}`
          }
          title={t('sidebar.settings')}
          aria-label={t('sidebar.settings')}
        >
          <Tooltip content={t('sidebar.settings')}>
            <Settings size={18} />
          </Tooltip>
        </NavLink>
      </div>
    </aside>
  );
}
