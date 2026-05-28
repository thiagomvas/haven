import { useState } from 'react'
import { Outlet } from 'react-router-dom'
import { Header } from './Header'
import { Sidebar } from './Sidebar'
import { Breadcrumb } from './Breadcrumb'
import styles from './AppShell.module.css'

export function AppShell() {
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false)

  return (
    <div className={styles.container}>
      <Sidebar collapsed={sidebarCollapsed} />
      <div className={`${styles.main} ${sidebarCollapsed ? styles.mainCollapsed : ''}`}>
        <Header onToggleSidebar={() => setSidebarCollapsed(!sidebarCollapsed)} middle={<Breadcrumb />} />
        <main className={styles.content}>
          <Outlet />
        </main>
      </div>
    </div>
  )
}
