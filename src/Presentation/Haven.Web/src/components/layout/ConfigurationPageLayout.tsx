import { ReactNode, useState } from 'react'
import styles from './ConfigurationPageLayout.module.css'

export interface ConfigurationMenuItem {
  id: string
  label: string
  content: ReactNode
}

interface ConfigurationPageLayoutProps {
  mainHeader: ReactNode
  configHeader?: ReactNode
  menuItems: ConfigurationMenuItem[]
  defaultMenuItem?: string
  children: ReactNode
  isConfigOpen?: boolean
  onConfigOpenChange?: (isOpen: boolean) => void
  selectedMenuId?: string
  onSelectedMenuIdChange?: (menuId: string) => void
  configButtonLabel?: string
  closeButtonLabel?: string
  hideConfigButton?: boolean
  hideCloseButton?: boolean
}

export function ConfigurationPageLayout({
  mainHeader,
  configHeader,
  menuItems,
  defaultMenuItem,
  children,
  isConfigOpen: controlledIsConfigOpen,
  onConfigOpenChange,
  selectedMenuId: controlledSelectedMenuId,
  onSelectedMenuIdChange,
  configButtonLabel = 'Configure',
  closeButtonLabel = 'Close',
  hideConfigButton = false,
  hideCloseButton = false,
}: ConfigurationPageLayoutProps) {
  const [uncontrolledIsConfigOpen, setUncontrolledIsConfigOpen] = useState(false)
  const isConfigOpen =
    controlledIsConfigOpen !== undefined ? controlledIsConfigOpen : uncontrolledIsConfigOpen

  const handleConfigOpenChange = (newState: boolean) => {
    if (controlledIsConfigOpen === undefined) {
      setUncontrolledIsConfigOpen(newState)
    }
    onConfigOpenChange?.(newState)
  }

  const [uncontrolledSelectedMenuItem, setUncontrolledSelectedMenuItem] = useState(
    defaultMenuItem || menuItems[0]?.id || ''
  )
  const selectedMenuItem =
    controlledSelectedMenuId !== undefined ? controlledSelectedMenuId : uncontrolledSelectedMenuItem

  const handleSelectedMenuIdChange = (menuId: string) => {
    if (controlledSelectedMenuId === undefined) {
      setUncontrolledSelectedMenuItem(menuId)
    }
    onSelectedMenuIdChange?.(menuId)
  }

  const selectedContent = menuItems.find((item) => item.id === selectedMenuItem)?.content

  if (isConfigOpen) {
    return (
      <div className={styles.container}>
        <div className={styles.header}>
          {configHeader}
          {!hideCloseButton && (
            <button
              className={styles.configButton}
              onClick={() => handleConfigOpenChange(false)}
            >
              {closeButtonLabel}
            </button>
          )}
        </div>

        <div className={styles.layoutContainer}>
          <aside className={styles.sidebar}>
            <nav className={styles.menu}>
              {menuItems.map((item) => (
                <button
                  key={item.id}
                  className={`${styles.menuItem} ${
                    selectedMenuItem === item.id ? styles.active : ''
                  }`}
                  onClick={() => {
                    handleSelectedMenuIdChange(item.id)
                  }}
                >
                  {item.label}
                </button>
              ))}
            </nav>
          </aside>
          <main className={styles.content}>{selectedContent}</main>
        </div>
      </div>
    )
  }

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        {mainHeader}
        {!hideConfigButton && (
          <button
            className={styles.configButton}
            onClick={() => handleConfigOpenChange(true)}
          >
            {configButtonLabel}
          </button>
        )}
      </div>

      <div className={styles.mainContent}>{children}</div>
    </div>
  )
}
