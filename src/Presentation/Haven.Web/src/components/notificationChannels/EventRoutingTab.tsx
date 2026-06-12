import { useState } from 'react';
import { Share2 } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { useDomainEventTypes } from '@/hooks/useEvents';
import { useNotificationChannels } from '@/hooks/useNotificationChannels';
import { Spinner } from '@/components/ui/Spinner';
import { Button } from '@/components/ui/Button';
import { EventIcon } from '@/components/ui/EventIcon';
import { NotificationChannelIcon } from './NotificationChannelIcon';
import type { DomainEventTypeDto } from '@/api/types';
import styles from './EventRoutingTab.module.css';

function formatEventName(name: string): string {
  return name.replace(/Event$/, '').replace(/([A-Z])/g, ' $1').trim();
}

interface ConfigPanelProps {
  event: DomainEventTypeDto;
  enabledProviders: string[];
  onToggleProvider: (providerId: string) => void;
}

function ConfigPanel({ event, enabledProviders, onToggleProvider }: ConfigPanelProps) {
  const { t } = useTranslation(['notificationChannels', 'common']);
  const { data, isLoading } = useNotificationChannels({ pageNumber: 1, pageSize: 100 });

  const providers = data?.items ?? [];

  return (
    <div className={styles.configPanel}>
      <div className={styles.configHeader}>
        <EventIcon type={event.name} />
        <div>
          <h3 className={styles.configTitle}>{formatEventName(event.name)}</h3>
          <p className={styles.configDescription}>
            {t('eventRouting.notifyVia')}
          </p>
        </div>
      </div>

      <div className={styles.providerList}>
        {isLoading && (
          <div className={styles.providerLoading}>
            <Spinner />
          </div>
        )}
        {!isLoading && providers.length === 0 && (
          <p className={styles.noProviders}>{t('eventRouting.noProviders')}</p>
        )}
        {providers.map(provider => (
          <label key={provider.id} className={styles.providerItem}>
            <div className={styles.providerInfo}>
              <div className={styles.providerIconWrap}>
                <NotificationChannelIcon channel={provider.channel} size={18} />
              </div>
              <div className={styles.providerMeta}>
                <span className={styles.providerName}>{provider.name}</span>
                {!provider.enabled && (
                  <span className={styles.providerDisabledHint}>
                    {t('common:labels.disabled')}
                  </span>
                )}
              </div>
            </div>
            <input
              type="checkbox"
              className={styles.providerCheckbox}
              checked={enabledProviders.includes(provider.id)}
              onChange={() => onToggleProvider(provider.id)}
              aria-label={provider.name}
            />
          </label>
        ))}
      </div>

      <div className={styles.configFooter}>
        <p className={styles.comingSoonHint}>{t('eventRouting.comingSoon')}</p>
        <Button disabled>{t('eventRouting.saveRule')}</Button>
      </div>
    </div>
  );
}

function EmptySelection() {
  const { t } = useTranslation('notificationChannels');
  return (
    <div className={styles.emptySelection}>
      <Share2 size={48} className={styles.emptySelectionIcon} />
      <h3 className={styles.emptySelectionTitle}>{t('page.eventRouting.empty.title')}</h3>
      <p className={styles.emptySelectionDescription}>{t('page.eventRouting.empty.description')}</p>
    </div>
  );
}

export function EventRoutingTab() {
  const { t } = useTranslation('notificationChannels');
  const { data: eventTypes, isLoading, error } = useDomainEventTypes();
  const [selectedEvent, setSelectedEvent] = useState<DomainEventTypeDto | null>(null);
  const [mockRules, setMockRules] = useState<Record<string, string[]>>({});

  const handleToggleProvider = (providerId: string) => {
    if (!selectedEvent) return;
    setMockRules(prev => {
      const current = prev[selectedEvent.name] ?? [];
      const next = current.includes(providerId)
        ? current.filter(id => id !== providerId)
        : [...current, providerId];
      return { ...prev, [selectedEvent.name]: next };
    });
  };

  if (isLoading) {
    return (
      <div className={styles.loadingContainer}>
        <Spinner />
      </div>
    );
  }

  if (error) {
    return (
      <div className={styles.errorContainer}>
        <div className={styles.errorMessage}>{t('eventRouting.loadError')}</div>
      </div>
    );
  }

  const events = eventTypes ?? [];

  return (
    <div className={styles.layout}>
      <aside className={styles.eventList}>
        {events.map(event => (
          <button
            key={event.name}
            type="button"
            className={`${styles.eventItem} ${selectedEvent?.name === event.name ? styles.eventItemActive : ''}`}
            onClick={() => setSelectedEvent(event)}
          >
            <EventIcon type={event.name} />
            <span className={styles.eventItemName}>{formatEventName(event.name)}</span>
            {(mockRules[event.name]?.length ?? 0) > 0 && (
              <span className={styles.eventRuleCount}>
                {t('eventRouting.providerCount', { count: mockRules[event.name].length })}
              </span>
            )}
          </button>
        ))}
      </aside>

      <div className={styles.configArea}>
        {selectedEvent ? (
          <ConfigPanel
            event={selectedEvent}
            enabledProviders={mockRules[selectedEvent.name] ?? []}
            onToggleProvider={handleToggleProvider}
          />
        ) : (
          <EmptySelection />
        )}
      </div>
    </div>
  );
}
