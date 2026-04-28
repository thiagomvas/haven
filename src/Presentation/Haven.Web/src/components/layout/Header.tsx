import { useTheme } from '@/hooks/useTheme'
import { Moon, Sun } from 'lucide-react'
import { Button } from '../ui/Button'
import styles from './Header.module.css'

export function Header() {
  const { theme, toggleTheme } = useTheme()

  return (
    <header className={styles.header}>
      <div className={styles.container}>
        <h1 className={styles.title}>Haven</h1>
        <Button
          variant="ghost"
          size="sm"
          onClick={toggleTheme}
          title={`Switch to ${theme === 'light' ? 'dark' : 'light'} mode`}
        >
          {theme === 'light' ? (
            <Moon size={18} />
          ) : (
            <Sun size={18} />
          )}
        </Button>
      </div>
    </header>
  )
}
