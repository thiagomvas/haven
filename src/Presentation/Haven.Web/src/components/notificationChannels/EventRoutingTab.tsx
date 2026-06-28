import { useState, useMemo, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import {
  useAllNotificationRules,
  useSetNotificationRules,
  useNotificationRuleSummary,
} from '@/hooks/useNotificationRules';
import { useNotificationChannels } from '@/hooks/useNotificationChannels';
import { Spinner } from '@/components/ui/Spinner';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { Checkbox } from '@/components/ui/Checkbox';
import { EventIcon } from '@/components/ui/EventIcon';
import { NotificationChannelIcon } from './NotificationChannelIcon';
import type { NotificationRuleContext } from "@/api/types/notification.types";
import styles from './EventRoutingTab.module.css';

type FilterMode = 'all' | 'active' | 'inactive';
type CheckState = 'all' | 'some' | 'none';

const formatEventName = (i18nKey: string, t: (key: string) => string) =>
  t(`events.types.${i18nKey}.label`);

const formatEventDescription = (i18nKey: string, t: (key: string) => string) =>
  t(`events.types.${i18nKey}.description`);

interface EventRoutingTabProps {
  ctx?: NotificationRuleContext;
}

export function EventRoutingTab({ ctx }: EventRoutingTabProps = {}) {
  const { t } = useTranslation(['notificationChannels', 'common']);
  const { t: tEvents } = useTranslation('events');

  const { data: summary, isLoading: summaryLoading, error } = useNotificationRuleSummary(ctx);
  const { data: allRulesData, isLoading: rulesLoading } = useAllNotificationRules(ctx);
  const { data: channelsData, isLoading: channelsLoading } = useNotificationChannels({
    pageNumber: 1,
    pageSize: 100,
  });
  const { mutateAsync: setRules, isPending: isSaving } = useSetNotificationRules(ctx);

  const [search, setSearch] = useState('');
  const [filter, setFilter] = useState<FilterMode>('all');
  const [selectedEvents, setSelectedEvents] = useState<Set<string>>(new Set());
  const [localRules, setLocalRules] = useState<Record<string, Set<string>> | null>(null);
  const [dirtyEvents, setDirtyEvents] = useState<Set<string>>(new Set());

  const isLoading = summaryLoading || rulesLoading || channelsLoading;
  const channels = channelsData?.items ?? [];
  const events = summary ?? [];

  const rulesMap = useMemo<Record<string, Set<string>>>(() => {
    if (!allRulesData) return {};
    const map: Record<string, Set<string>> = {};
    for (const rule of allRulesData) {
      map[rule.eventType] = new Set(rule.channelIds);
    }
    return map;
  }, [allRulesData]);

  const effectiveRules = useMemo(() => {
    if (!localRules) return rulesMap;
    // Merge: localRules has explicit overrides, rulesMap fills the rest
    const merged: Record<string, Set<string>> = { ...rulesMap };
    for (const [k, v] of Object.entries(localRules)) merged[k] = v;
    return merged;
  }, [localRules, rulesMap]);

  const filteredEvents = useMemo(() => {
    return events.filter(event => {
      const channelCount = (effectiveRules[event.name] ?? new Set()).size;
      if (filter === 'active' && channelCount === 0) return false;
      if (filter === 'inactive' && channelCount > 0) return false;
      if (!search.trim()) return true;
      const q = search.trim().toLowerCase();
      const translated = tEvents(`events.types.${event.i18NKey}.label`, {
        defaultValue: '',
      }).toLowerCase();
      return translated.includes(q) || event.name.toLowerCase().includes(q);
    });
  }, [events, filter, search, effectiveRules, tEvents]);

  const allVisibleSelected =
    filteredEvents.length > 0 && filteredEvents.every(e => selectedEvents.has(e.name));
  const someVisibleSelected = filteredEvents.some(e => selectedEvents.has(e.name));

  const handleSelectAll = () => {
    if (allVisibleSelected) {
      setSelectedEvents(prev => {
        const next = new Set(prev);
        filteredEvents.forEach(e => next.delete(e.name));
        return next;
      });
    } else {
      setSelectedEvents(prev => new Set([...prev, ...filteredEvents.map(e => e.name)]));
    }
  };

  const handleSelectEvent = (eventName: string) => {
    setSelectedEvents(prev => {
      const next = new Set(prev);
      if (next.has(eventName)) next.delete(eventName);
      else next.add(eventName);
      return next;
    });
  };

  const getChannelState = useCallback(
    (channelId: string): CheckState => {
      if (selectedEvents.size === 0) return 'none';
      let checked = 0;
      for (const name of selectedEvents) {
        if (effectiveRules[name]?.has(channelId)) checked++;
      }
      if (checked === 0) return 'none';
      if (checked === selectedEvents.size) return 'all';
      return 'some';
    },
    [selectedEvents, effectiveRules]
  );

  const handleToggleChannel = (channelId: string) => {
    if (selectedEvents.size === 0) return;
    const state = getChannelState(channelId);
    const targetChecked = state !== 'all';

    setLocalRules(prev => {
      const updated: Record<string, Set<string>> = { ...(prev ?? {}) };
      for (const eventName of selectedEvents) {
        // Fall back to rulesMap for events not yet in localRules
        const current = new Set(prev?.[eventName] ?? rulesMap[eventName] ?? []);
        if (targetChecked) current.add(channelId);
        else current.delete(channelId);
        updated[eventName] = current;
      }
      return updated;
    });
    setDirtyEvents(prev => new Set([...prev, ...selectedEvents]));
  };

  const handleSave = async () => {
    if (!localRules || dirtyEvents.size === 0) return;
    await Promise.all(
      [...dirtyEvents].map(eventType =>
        setRules({
          eventType,
          data: { channelIds: [...(localRules[eventType] ?? rulesMap[eventType] ?? [])] },
        })
      )
    );
    setDirtyEvents(new Set());
    setLocalRules(null);
  };

  const handleDiscard = () => {
    setLocalRules(null);
    setDirtyEvents(new Set());
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
        <div className={styles.errorMessage}>
          {t('notificationChannels:eventRouting.loadError')}
        </div>
      </div>
    );
  }

  const activeSelectedCount = selectedEvents.size;
  const hasChanges = dirtyEvents.size > 0;

  return (
    <div className={styles.root}>
      <div className={styles.toolbar}>
        <Input
          placeholder={t('notificationChannels:eventRouting.searchPlaceholder')}
          value={search}
          onChange={e => setSearch(e.target.value)}
          className={styles.searchInput}
        />
        <div className={styles.filterGroup}>
          {(['all', 'active', 'inactive'] as FilterMode[]).map(mode => (
            <Button
              key={mode}
              size="xs"
              variant={filter === mode ? 'primary' : 'outline'}
              onClick={() => setFilter(mode)}
            >
              {t(
                `notificationChannels:eventRouting.filter${mode.charAt(0).toUpperCase() + mode.slice(1)}` as any
              )}
            </Button>
          ))}
        </div>
        {hasChanges && (
          <div className={styles.saveBar}>
            <span className={styles.changeCount}>
              {t('notificationChannels:eventRouting.unsavedChanges', { count: dirtyEvents.size })}
            </span>
            <Button size="sm" variant="outline" onClick={handleDiscard} disabled={isSaving}>
              {t('notificationChannels:eventRouting.discard')}
            </Button>
            <Button size="sm" onClick={handleSave} isLoading={isSaving}>
              {t('notificationChannels:eventRouting.saveChanges')}
            </Button>
          </div>
        )}
      </div>

      <div className={styles.layout}>
        {/* Left: event list */}
        <div className={styles.eventPanel}>
          <div className={styles.eventListHeader}>
            <Checkbox
              label={
                activeSelectedCount > 0
                  ? t('notificationChannels:eventRouting.selectedCount', {
                      count: activeSelectedCount,
                    })
                  : t('notificationChannels:eventRouting.selectAllLabel')
              }
              checked={allVisibleSelected}
              indeterminate={!allVisibleSelected && someVisibleSelected}
              onChange={handleSelectAll}
            />
          </div>

          <div className={styles.eventList}>
            {filteredEvents.length === 0 && (
              <p className={styles.emptyMessage}>
                {t('notificationChannels:eventRouting.noResults')}
              </p>
            )}
            {filteredEvents.map(event => {
              const isSelected = selectedEvents.has(event.name);
              const isDirty = dirtyEvents.has(event.name);
              const channelCount = (effectiveRules[event.name] ?? new Set()).size;
              return (
                <div
                  key={event.name}
                  className={`${styles.eventRow} ${isSelected ? styles.eventRowSelected : ''}`}
                >
                  <Checkbox
                    label={formatEventName(event.i18NKey, tEvents as (key: string) => string)}
                    description={formatEventDescription(
                      event.i18NKey,
                      tEvents as (key: string) => string
                    )}
                    checked={isSelected}
                    onChange={() => handleSelectEvent(event.name)}
                    icon={<EventIcon type={event.name} />}
                  />
                  <div className={styles.eventMeta}>
                    {isDirty && <span className={styles.dirtyDot} />}
                    {channelCount > 0 && (
                      <span className={styles.channelBadge}>
                        {t('notificationChannels:eventRouting.providerCount', {
                          count: channelCount,
                        })}
                      </span>
                    )}
                  </div>
                </div>
              );
            })}
          </div>
        </div>

        {/* Right: channel panel */}
        <div className={styles.channelPanel}>
          {activeSelectedCount === 0 ? (
            <div className={styles.channelPanelEmpty}>
              <p className={styles.emptyMessage}>
                {t('notificationChannels:eventRouting.selectEventsPrompt')}
              </p>
            </div>
          ) : channels.length === 0 ? (
            <div className={styles.channelPanelEmpty}>
              <p className={styles.emptyMessage}>
                {t('notificationChannels:eventRouting.noProviders')}
              </p>
            </div>
          ) : (
            <>
              <div className={styles.channelPanelHeader}>
                <span className={styles.channelPanelTitle}>
                  {t('notificationChannels:eventRouting.configuringFor', {
                    count: activeSelectedCount,
                  })}
                </span>
              </div>
              <div className={styles.channelList}>
                {channels.map(channel => {
                  const state = getChannelState(channel.id);
                  return (
                    <div key={channel.id} className={styles.channelRow}>
                      <Checkbox
                        label={channel.name}
                        description={
                          !channel.enabled
                            ? t('common:labels.disabled')
                            : state === 'some'
                              ? t('notificationChannels:eventRouting.mixed')
                              : undefined
                        }
                        checked={state === 'all'}
                        indeterminate={state === 'some'}
                        onChange={() => handleToggleChannel(channel.id)}
                        disabled={!channel.enabled}
                        icon={
                          <div className={styles.channelIconWrap}>
                            <NotificationChannelIcon channel={channel.channel} size={16} />
                          </div>
                        }
                      />
                    </div>
                  );
                })}
              </div>
            </>
          )}
        </div>
      </div>
    </div>
  );
}
