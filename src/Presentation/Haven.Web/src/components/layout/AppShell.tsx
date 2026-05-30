import { useEffect, useState } from 'react'
import { Outlet, useNavigate } from 'react-router-dom'
import { useTheme } from '@/hooks/useTheme'
import { useCurrentUser } from '@/hooks/useCurrentUser'
import { Moon, Sun } from 'lucide-react'
import { Button } from '../ui/Button'
import { UserAvatar } from '../ui/UserAvatar'
import { Header } from './Header'
import { Sidebar } from './Sidebar'
import { Breadcrumb } from './Breadcrumb'
import { FuzzySearchBar } from './FuzzySearchBar'
import styles from './AppShell.module.css'

export function AppShell() {
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false)
  const { theme, toggleTheme } = useTheme()
  const navigate = useNavigate()
  const user = useCurrentUser()

  useEffect(() => {
    if (user?.requirePasswordChange) {
      navigate('/set-password', { replace: true })
    }
  }, [user, navigate])

  const headerRight = (
    <div style={{ display: 'flex', alignItems: 'center', gap: 'var(--space-2)' }}>
      <Button
        variant="ghost"
        size="sm"
        onClick={toggleTheme}
        title={theme === 'light' ? 'Switch to dark' : 'Switch to light'}
      >
        {theme === 'light' ? <Moon size={18} /> : <Sun size={18} />}
      </Button>
      {user && <UserAvatar user={user} />}
    </div>
  )

  return (
    <div className={styles.container}>
      <Sidebar collapsed={sidebarCollapsed} onToggleCollapse={() => setSidebarCollapsed(!sidebarCollapsed)} />
      <div className={`${styles.main} ${sidebarCollapsed ? styles.mainCollapsed : ''}`}>
        <Header left={<Breadcrumb />} center={<FuzzySearchBar />} right={headerRight} />
        <main className={styles.content}>
          <Outlet />
        </main>
      </div>
    </div>
  )
}
