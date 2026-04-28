import { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { NavLink } from 'react-router-dom'
import { LayoutDashboard, FolderOpen, Clock } from 'lucide-react'
import { Tooltip } from '../ui/Tooltip'
import styles from './Sidebar.module.css'

interface SidebarProps {
  collapsed?: boolean
}

export function Sidebar({ collapsed = false }: SidebarProps) {
  const { t } = useTranslation('layout')

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

  return (
    <aside className={`${styles.sidebar} ${collapsed ? styles.sidebarCollapsed : ''}`}>
      <nav className={styles.nav}>
        <NavLink
          to="/dashboard"
          className={({ isActive }) =>
            `${styles.navLink} ${isActive ? styles.navLinkActive : ''}`
          }
        >
          <NavLinkContent icon={<LayoutDashboard size={20} />} label={t('sidebar.dashboard')} />
        </NavLink>
        <NavLink
          to="/projects"
          className={({ isActive }) =>
            `${styles.navLink} ${isActive ? styles.navLinkActive : ''}`
          }
        >
          <NavLinkContent icon={<FolderOpen size={20} />} label={t('sidebar.projects')} />
        </NavLink>
        <NavLink
          to="/events"
          className={({ isActive }) =>
            `${styles.navLink} ${isActive ? styles.navLinkActive : ''}`
          }
        >
          <NavLinkContent icon={<Clock size={20} />} label={t('sidebar.events')} />
        </NavLink>
      </nav>
    </aside>
  )
}
