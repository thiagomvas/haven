import { useTranslation } from 'react-i18next'
import { NavLink } from 'react-router-dom'
import { LayoutDashboard, FolderOpen, Clock } from 'lucide-react'
import styles from './Sidebar.module.css'

export function Sidebar() {
  const { t } = useTranslation('layout')

  return (
    <aside className={styles.sidebar}>
      <nav className={styles.nav}>
        <NavLink
          to="/dashboard"
          className={({ isActive }) =>
            `${styles.navLink} ${isActive ? styles.navLinkActive : ''}`
          }
        >
          <LayoutDashboard size={20} />
          <span>{t('sidebar.dashboard')}</span>
        </NavLink>
        <NavLink
          to="/projects"
          className={({ isActive }) =>
            `${styles.navLink} ${isActive ? styles.navLinkActive : ''}`
          }
        >
          <FolderOpen size={20} />
          <span>{t('sidebar.projects')}</span>
        </NavLink>
        <NavLink
          to="/events"
          className={({ isActive }) =>
            `${styles.navLink} ${isActive ? styles.navLinkActive : ''}`
          }
        >
          <Clock size={20} />
          <span>{t('sidebar.events')}</span>
        </NavLink>
      </nav>
    </aside>
  )
}
