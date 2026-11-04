import { clsx } from 'clsx';
import { ReactNode, useState } from 'react';

import { Button } from '@/components/ui/Button';
import styles from '@/styles/components/layout/ConfigurationPageLayout.module.css';

export interface ConfigurationMenuItem {
  id: string;
  label: string;
  content: ReactNode;
}

export interface ConfigurationMenuSection {
  id: string;
  label: string;
  items: ConfigurationMenuItem[];
}

interface ConfigurationPageLayoutProps {
  mainHeader: ReactNode;
  configHeader?: ReactNode;
  menuItems?: ConfigurationMenuItem[];
  sections?: ConfigurationMenuSection[];
  defaultMenuItem?: string;
  children: ReactNode;
  isConfigOpen?: boolean;
  onConfigOpenChange?: (isOpen: boolean) => void;
  selectedMenuId?: string;
  onSelectedMenuIdChange?: (menuId: string) => void;
  configButtonLabel?: string;
  closeButtonLabel?: string;
  hideConfigButton?: boolean;
  hideCloseButton?: boolean;
}

export function ConfigurationPageLayout({
  mainHeader,
  configHeader,
  menuItems = [],
  sections,
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
  const [uncontrolledIsConfigOpen, setUncontrolledIsConfigOpen] = useState(false);
  const isConfigOpen =
    controlledIsConfigOpen !== undefined ? controlledIsConfigOpen : uncontrolledIsConfigOpen;

  const handleConfigOpenChange = (newState: boolean) => {
    if (controlledIsConfigOpen === undefined) {
      setUncontrolledIsConfigOpen(newState);
    }
    onConfigOpenChange?.(newState);
  };

  const allItems: ConfigurationMenuItem[] = sections ? sections.flatMap(s => s.items) : menuItems;

  const [uncontrolledSelectedMenuItem, setUncontrolledSelectedMenuItem] = useState(
    defaultMenuItem || allItems[0]?.id || ''
  );
  const selectedMenuItem =
    controlledSelectedMenuId !== undefined
      ? controlledSelectedMenuId
      : uncontrolledSelectedMenuItem;

  const handleSelectedMenuIdChange = (menuId: string) => {
    if (controlledSelectedMenuId === undefined) {
      setUncontrolledSelectedMenuItem(menuId);
    }
    onSelectedMenuIdChange?.(menuId);
  };

  const selectedContent = allItems.find(item => item.id === selectedMenuItem)?.content;

  const renderMenuItem = (item: ConfigurationMenuItem) => {
    const isActive = selectedMenuItem === item.id;
    return (
      <button
        key={item.id}
        type="button"
        className={clsx(styles.menuItem, isActive && styles.active)}
        aria-current={isActive ? 'page' : undefined}
        onClick={() => handleSelectedMenuIdChange(item.id)}
      >
        {item.label}
      </button>
    );
  };

  if (isConfigOpen) {
    return (
      <div className={styles.container}>
        <div className={styles.header}>
          {configHeader}
          {!hideCloseButton && (
            <Button variant="ghost" onClick={() => handleConfigOpenChange(false)}>
              {closeButtonLabel}
            </Button>
          )}
        </div>

        <div className={styles.layoutContainer}>
          <aside className={styles.sidebar}>
            <nav className={styles.menu}>
              {sections
                ? sections.map(section => (
                    <div key={section.id} className={styles.menuSection}>
                      <span className={styles.menuSectionLabel}>{section.label}</span>
                      {section.items.map(renderMenuItem)}
                    </div>
                  ))
                : allItems.map(renderMenuItem)}
            </nav>
          </aside>
          <main className={styles.content}>{selectedContent}</main>
        </div>
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        {mainHeader}
        {!hideConfigButton && (
          <Button variant="primary" onClick={() => handleConfigOpenChange(true)}>
            {configButtonLabel}
          </Button>
        )}
      </div>

      <div className={styles.mainContent}>{children}</div>
    </div>
  );
}
