import { clsx } from 'clsx';
import { ReactNode, useState } from 'react';

import styles from '@/styles/components/ui/Tabs.module.css';

export interface TabItem {
  id: string;
  label: string;
  icon?: ReactNode;
  content: ReactNode;
  disabled?: boolean;
}

interface TabsProps {
  items: TabItem[];
  defaultTab?: string;
  activeTab?: string;
  onChange?: (tabId: string) => void;
}

export function Tabs({ items, defaultTab, activeTab: controlledActiveTab, onChange }: TabsProps) {
  const [uncontrolledActiveTab, setUncontrolledActiveTab] = useState(
    defaultTab || items[0]?.id || ''
  );
  const activeTab = controlledActiveTab !== undefined ? controlledActiveTab : uncontrolledActiveTab;

  const handleTabChange = (tabId: string) => {
    if (controlledActiveTab === undefined) {
      setUncontrolledActiveTab(tabId);
    }
    onChange?.(tabId);
  };

  const activeItem = items.find(item => item.id === activeTab);

  return (
    <div className={styles.container}>
      <div className={styles.tabList} role="tablist">
        {items.map(item => (
          <button
            key={item.id}
            role="tab"
            aria-selected={activeTab === item.id}
            aria-controls={`tab-panel-${item.id}`}
            className={clsx(styles.tab, {
              [styles.active]: activeTab === item.id,
              [styles.disabled]: item.disabled,
            })}
            onClick={() => handleTabChange(item.id)}
            disabled={item.disabled}
          >
            {item.icon && <span className={styles.icon}>{item.icon}</span>}
            <span>{item.label}</span>
          </button>
        ))}
      </div>
      {activeItem && (
        <div id={`tab-panel-${activeTab}`} role="tabpanel" className={styles.content}>
          {activeItem.content}
        </div>
      )}
    </div>
  );
}
