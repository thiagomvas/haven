import { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { useTheme } from '@/hooks/useTheme'
import { Moon, Sun } from 'lucide-react'
import { Button } from '../ui/Button'
import styles from './Header.module.css'

interface HeaderProps {
  left?: ReactNode
  middle?: ReactNode
  right?: ReactNode
}

export function Header({ left, middle, right }: HeaderProps) {
  const { t } = useTranslation('layout')
  const { theme, toggleTheme } = useTheme()

  return (
    <header className={styles.header}>
      <div className={styles.container}>
        <div className={styles.section}>
          {left || (
            <h1 className={styles.title}>
              {t('header.brand')}
            </h1>
          )}
        </div>
        <div className={styles.section}>
          {middle}
        </div>
        <div className={styles.section}>
          {right || (
            <Button
              variant="ghost"
              size="sm"
              onClick={toggleTheme}
              title={theme === 'light' ? t('header.switchToDark') : t('header.switchToLight')}
            >
              {theme === 'light' ? (
                <Moon size={18} />
              ) : (
                <Sun size={18} />
              )}
            </Button>
          )}
        </div>
      </div>
    </header>
  )
}
