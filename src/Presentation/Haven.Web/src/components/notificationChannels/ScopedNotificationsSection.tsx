import { useTranslation } from 'react-i18next';
import { Globe, Pencil } from 'lucide-react';
import {
  useNotificationRuleSummary,
  useClearNotificationRuleOverride,
} from '@/hooks/useNotificationRules';
import { Button } from '@/components/ui/Button';
import { Chip } from '@/components/ui/Chip';
import { EventRoutingTab } from './EventRoutingTab';
import type { NotificationRuleContext } from '@/api/types';
import styles from './ScopedNotificationsSection.module.css';

interface ScopedNotificationsSectionProps {
  ctx: NotificationRuleContext;
}

export function ScopedNotificationsSection({ ctx }: ScopedNotificationsSectionProps) {
  const { t } = useTranslation('notificationChannels');
  const { data: summary } = useNotificationRuleSummary(ctx);
  const clearOverride = useClearNotificationRuleOverride(ctx);

  const overriddenEvents = summary?.filter(e => e.isOverridden) ?? [];
  const hasOverrides = overriddenEvents.length > 0;

  const handleResetAll = async () => {
    await Promise.all(overriddenEvents.map(e => clearOverride.mutateAsync(e.name)));
  };

  return (
    <div className={styles.root}>
      <div className={styles.statusBar}>
        {hasOverrides ? (
          <>
            <Chip
              icon={<Pencil size={12} />}
              content={t('scopedSection.customConfig', { count: overriddenEvents.length })}
              variant="primary"
              size="sm"
            />
            <Button
              size="xs"
              variant="outline"
              onClick={handleResetAll}
              isLoading={clearOverride.isPending}
            >
              {t('scopedSection.resetToGlobal')}
            </Button>
          </>
        ) : (
          <Chip
            icon={<Globe size={12} />}
            content={t('scopedSection.usingGlobal')}
            variant="default"
            size="sm"
          />
        )}
      </div>
      <EventRoutingTab ctx={ctx} />
    </div>
  );
}
