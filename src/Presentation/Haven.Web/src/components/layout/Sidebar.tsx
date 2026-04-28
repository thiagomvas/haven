import { NavLink } from 'react-router-dom'
import { LayoutDashboard, FolderOpen, Clock } from 'lucide-react'
import styles from './Sidebar.module.css'

export function Sidebar() {
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
          <span>Dashboard</span>
        </NavLink>
        <NavLink
          to="/projects"
          className={({ isActive }) =>
            `${styles.navLink} ${isActive ? styles.navLinkActive : ''}`
          }
        >
          <FolderOpen size={20} />
          <span>Projects</span>
        </NavLink>
        <NavLink
          to="/events"
          className={({ isActive }) =>
            `${styles.navLink} ${isActive ? styles.navLinkActive : ''}`
          }
        >
          <Clock size={20} />
          <span>Events</span>
        </NavLink>
      </nav>
    </aside>
  )
}
