import { useEffect, useState } from 'react';
import { Share2 } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { useNotificationRuleSummary, useNotificationRulesForEvent, useSetNotificationRules } from '@/hooks/useNotificationRules';
import { useNotificationChannels } from '@/hooks/useNotificationChannels';
import { Spinner } from '@/components/ui/Spinner';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { Checkbox } from '@/components/ui/Checkbox';
import { Badge } from '@/components/ui/Badge';
import { EventIcon } from '@/components/ui/EventIcon';
import { NotificationChannelIcon } from './NotificationChannelIcon';
import type { NotificationRuleSummaryItemDto } from '@/api/types';
import styles from './EventRoutingTab.module.css';

type FilterMode = 'all' | 'active' | 'inactive';

interface ConfigPanelProps {
  event: NotificationRuleSummaryItemDto;
}

const formatEventName = (name: string, t: (key: string) => string = (k) => k) => {
  // Try to get a translated name first
  const translated = t(`events.types.${name}.label`);
  return translated;
};

const formatEventDescription = (name: string, t: (key: string) => string = (k) => k) => {
  const translated = t(`events.types.${name}.description`);
  return translated;
}

function ConfigPanel({ event }: ConfigPanelProps) {
  const { t } = useTranslation(['notificationChannels', 'common']);
  const { t: tEvents } = useTranslation('events');
  const { data: channelsData, isLoading: channelsLoading } = useNotificationChannels({ pageNumber: 1, pageSize: 100 });
  const { data: rulesData, isLoading: rulesLoading } = useNotificationRulesForEvent(event.name);
  const { mutateAsync: setRules, isPending: isSaving } = useSetNotificationRules();

  const [selectedChannelIds, setSelectedChannelIds] = useState<string[]>([]);

  useEffect(() => {
    if (rulesData) {
      setSelectedChannelIds(rulesData.channelIds ?? []);
    }
  }, [rulesData]);

  const providers = channelsData?.items ?? [];
  const isLoading = channelsLoading || rulesLoading;

  const handleToggle = (channelId: string) => {
    setSelectedChannelIds(prev =>
      prev.includes(channelId) ? prev.filter(id => id !== channelId) : [...prev, channelId]
    );
  };

  const handleSave = async () => {
    await setRules({ eventType: event.name, data: { channelIds: selectedChannelIds } });
  };

  return (
    <div className={styles.configPanel}>
      <div className={styles.configHeader}>
        <EventIcon type={event.name} />
        <div>
          <h3 className={styles.configTitle}>{formatEventName(event.i18NKey, tEvents as (key: string) => string)}</h3>
          <p className={styles.configDescription}>{formatEventDescription(event.i18NKey, tEvents as (key: string) => string)}</p>
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
        {!isLoading && providers.map(provider => (
          <Checkbox
            key={provider.id}
            className={styles.providerItem}
            icon={
              <div className={styles.providerIconWrap}>
                <NotificationChannelIcon channel={provider.channel} size={18} />
              </div>
            }
            label={provider.name}
            description={!provider.enabled ? t('common:labels.disabled') : undefined}
            checked={selectedChannelIds.includes(provider.id)}
            onChange={() => handleToggle(provider.id)}
          />
        ))}
      </div>

      <div className={styles.configFooter}>
        <Button onClick={handleSave} disabled={isLoading} isLoading={isSaving}>
          {t('eventRouting.saveRule')}
        </Button>
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
  const { data: summary, isLoading, error } = useNotificationRuleSummary();
  const [selectedEvent, setSelectedEvent] = useState<NotificationRuleSummaryItemDto | null>(null);
  const [search, setSearch] = useState('');
  const [filter, setFilter] = useState<FilterMode>('all');

  const { t: tEvents } = useTranslation('events');

  const events = summary ?? [];

  const filteredEvents = events.filter(event => {
    if (filter === 'active' && event.ruleCount === 0) return false;
    if (filter === 'inactive' && event.ruleCount > 0) return false;

    if (!search.trim()) return true;
    const q = search.trim().toLowerCase();
    const translatedName = tEvents(`events.types.${event.i18NKey}`, { defaultValue: '' }).toLowerCase();
    const englishName = event.name.toLowerCase();
    const rawName = event.name.toLowerCase();
    return translatedName.includes(q) || englishName.includes(q) || rawName.includes(q);
  });

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

  return (
    <div className={styles.layout}>
      <div className={styles.leftColumn}>
        <div className={styles.searchToolbar}>
          <Input
            placeholder={t('eventRouting.searchPlaceholder')}
            value={search}
            onChange={e => setSearch(e.target.value)}
          />
          <div className={styles.filterGroup}>
            {(['all', 'active', 'inactive'] as FilterMode[]).map(mode => (
              <Button
                key={mode}
                size="xs"
                variant={filter === mode ? 'primary' : 'outline'}
                onClick={() => setFilter(mode)}
              >
                {t(`eventRouting.filter${mode.charAt(0).toUpperCase() + mode.slice(1)}` as any)}
              </Button>
            ))}
          </div>
        </div>

        <aside className={styles.eventList}>
          {filteredEvents.length === 0 && (
            <p className={styles.noProviders}>{t('eventRouting.noResults')}</p>
          )}
          {filteredEvents.map(event => (
            <button
              key={event.name}
              type="button"
              className={`${styles.eventItem} ${selectedEvent?.name === event.name ? styles.eventItemActive : ''}`}
              onClick={() => setSelectedEvent(event)}
            >
              <EventIcon type={event.name} />
              <span className={styles.eventItemName}>{formatEventName(event.i18NKey, tEvents as (key: string) => string)}</span>
              {event.ruleCount > 0 && (
                <Badge variant="primary">
                  {t('eventRouting.providerCount', { count: event.ruleCount })}
                </Badge>
              )}
            </button>
          ))}
        </aside>
      </div>

      <div className={styles.configArea}>
        {selectedEvent ? (
          <ConfigPanel event={selectedEvent} />
        ) : (
          <EmptySelection />
        )}
      </div>
    </div>
  );
}
