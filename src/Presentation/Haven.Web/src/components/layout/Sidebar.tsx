import { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { NavLink } from 'react-router-dom'
import {
  LayoutDashboard,
  FolderOpen,
  GitBranch,
  Clock,
  BarChart3,
  AlertCircle,
  Settings,
  HelpCircle,
  BookOpen,
  ChevronLeft,
  ChevronRight,
  PanelRightOpen,
  PanelRightClose,
} from 'lucide-react'
import { Button } from '../ui/Button'
import { Tooltip } from '../ui/Tooltip'
import styles from './Sidebar.module.css'

interface SidebarProps {
  collapsed?: boolean
  onToggleCollapse?: () => void
}

interface NavItem {
  to: string
  icon: ReactNode
  label: string
  translationKey: string
}

export function Sidebar({ collapsed = false, onToggleCollapse }: SidebarProps) {
  const { t } = useTranslation('layout')

  const mainNavItems: NavItem[] = [
    { to: '/dashboard', icon: <LayoutDashboard size={20} />, label: 'Dashboard', translationKey: 'sidebar.dashboard' },
    { to: '/projects', icon: <FolderOpen size={20} />, label: 'Projects', translationKey: 'sidebar.projects' },
    { to: '/events', icon: <Clock size={20} />, label: 'Events', translationKey: 'sidebar.events' },
  ]

  const systemNavItems: NavItem[] = [
    { to: '/monitoring', icon: <BarChart3 size={20} />, label: 'Monitoring', translationKey: 'sidebar.monitoring' },
    { to: '/alerts', icon: <AlertCircle size={20} />, label: 'Alerts', translationKey: 'sidebar.alerts' },
    { to: '/git-providers', icon: <GitBranch size={20} />, label: 'Git Providers', translationKey: 'sidebar.gitProviders' },

  ]

  const helpNavItems: NavItem[] = [
    { to: '/docs', icon: <BookOpen size={20} />, label: 'Documentation', translationKey: 'sidebar.documentation' },
    { to: '/help', icon: <HelpCircle size={20} />, label: 'Help', translationKey: 'sidebar.helpItem' },
  ]

  const NavLinkContent = ({ icon, label }: { icon: ReactNode; label: string }) => (
    <>
      {collapsed ? (
        <Tooltip content={label}>
          {icon}
        </Tooltip>
      ) : (
        <>
          {icon}
          <span>{label}</span>
        </>
      )}
    </>
  )

  const renderNavItem = (item: NavItem) => (
    <NavLink
      key={item.to}
      to={item.to}
      className={({ isActive }) =>
        `${styles.navLink} ${isActive ? styles.navLinkActive : ''}`
      }
    >
      <NavLinkContent icon={item.icon} label={t(item.translationKey)} />
    </NavLink>
  )

  const renderSection = (items: NavItem[]) =>
    items.map(renderNavItem)

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
          <div className={styles.navItems}>
            {renderSection(mainNavItems)}
          </div>
        </div>

        <div className={styles.navSection}>
          <div className={styles.sectionTitle}>{collapsed ? '' : t('sidebar.system')}</div>
          <div className={styles.navItems}>
            {renderSection(systemNavItems)}
          </div>
        </div>

        <div className={styles.navSection}>
          <div className={styles.sectionTitle}>{collapsed ? '' : t('sidebar.help')}</div>
          <div className={styles.navItems}>
            {renderSection(helpNavItems)}
          </div>
        </div>
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
  )
}
