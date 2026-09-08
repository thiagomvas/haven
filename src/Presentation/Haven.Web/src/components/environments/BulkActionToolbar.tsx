import { Play, RotateCw, Square, X } from 'lucide-react';
import { useTranslation } from 'react-i18next';

import { Row, Spacer } from '@/components/layout';
import { Button } from '@/components/ui/Button';
import { Checkbox } from '@/components/ui/Checkbox';

export type BulkAction = 'deploy' | 'restart' | 'stop';

interface BulkActionToolbarProps {
  selectedCount: number;
  totalCount: number;
  allSelected: boolean;
  someSelected: boolean;
  onToggleSelectAll: () => void;
  onRequestAction: (action: BulkAction) => void;
  onExitSelectionMode: () => void;
}

export function BulkActionToolbar({
  selectedCount,
  totalCount,
  allSelected,
  someSelected,
  onToggleSelectAll,
  onRequestAction,
  onExitSelectionMode,
}: BulkActionToolbarProps) {
  const { t } = useTranslation(['environments', 'common']);
  const hasSelection = selectedCount > 0;

  return (
    <Row align="center" gap="3" full>
      <Checkbox
        label={t('bulkActions.selectAll')}
        checked={allSelected}
        indeterminate={someSelected && !allSelected}
        onChange={onToggleSelectAll}
        disabled={totalCount === 0}
      />
      <span>{t('bulkActions.selectedCount', { count: selectedCount })}</span>
      <Spacer expand direction="horizontal" />
      <Button
        variant="secondary"
        size="sm"
        icon={<Play size={16} />}
        disabled={!hasSelection}
        onClick={() => onRequestAction('deploy')}
      >
        {t('common:actions.deploySelected')}
      </Button>
      <Button
        variant="secondary"
        size="sm"
        icon={<RotateCw size={16} />}
        disabled={!hasSelection}
        onClick={() => onRequestAction('restart')}
      >
        {t('common:actions.restartSelected')}
      </Button>
      <Button
        variant="danger"
        size="sm"
        icon={<Square size={16} />}
        disabled={!hasSelection}
        onClick={() => onRequestAction('stop')}
      >
        {t('common:actions.stopSelected')}
      </Button>
      <Button variant="ghost" size="sm" icon={<X size={16} />} onClick={onExitSelectionMode}>
        {t('common:actions.exitSelection')}
      </Button>
    </Row>
  );
}
