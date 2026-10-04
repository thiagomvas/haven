import { useCallback, useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';

import { CustomActionDto } from '@/api/types';

import { customActionsApi } from '../../api/customActions';
import { Row, Stack } from '../layout';
import { Banner } from '../ui/Banner';
import { Button } from '../ui/Button';
import { Card } from '../ui/Card';
import { Label } from '../ui/Label';
import { LucideIcon } from '../ui/LucideIcon';
import { Modal } from '../ui/Modal';
import { Spinner } from '../ui/Spinner';

interface CustomActionsTabProps {
  projectId: string;
  environmentId: string;
  serviceId: string;
}

type Outcome = { variant: 'success' | 'error'; message: string };

export function CustomActionsTab({ projectId, environmentId, serviceId }: CustomActionsTabProps) {
  const { t } = useTranslation(['services']);

  const [actions, setActions] = useState<CustomActionDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [runningId, setRunningId] = useState<string | null>(null);
  const [confirmTarget, setConfirmTarget] = useState<CustomActionDto | null>(null);
  const [outcome, setOutcome] = useState<Outcome | null>(null);

  const load = useCallback(async () => {
    try {
      setLoading(true);
      setActions((await customActionsApi.list(projectId, environmentId, serviceId)) ?? []);
    } catch (err) {
      setLoadError(err instanceof Error ? err.message : t('services:error'));
    } finally {
      setLoading(false);
    }
  }, [projectId, environmentId, serviceId, t]);

  useEffect(() => {
    (async () => {
      await load();
    })();
  }, [load]);

  const run = async (action: CustomActionDto) => {
    try {
      setRunningId(action.id);
      setOutcome(null);
      await customActionsApi.execute(projectId, environmentId, serviceId, action.id);
      setOutcome({
        variant: 'success',
        message: t('services:customActions.runSucceeded', { name: action.actionName }),
      });
    } catch (err) {
      setOutcome({
        variant: 'error',
        message: err instanceof Error ? err.message : t('services:error'),
      });
    } finally {
      setRunningId(null);
      setConfirmTarget(null);
    }
  };

  const onClick = (action: CustomActionDto) => {
    if (action.risk === 'RequireConfirmation') setConfirmTarget(action);
    else void run(action);
  };

  if (loading) {
    return (
      <Row justify="center" align="center">
        <Spinner />
      </Row>
    );
  }

  return (
    <Stack gap="4">
      {loadError && <Banner variant="error" description={loadError} />}
      {outcome && <Banner variant={outcome.variant} description={outcome.message} />}

      {actions.length === 0 ? (
        <Label variant="secondary" size="sm">
          {t('services:customActions.emptyRun')}
        </Label>
      ) : (
        <Row gap="3" wrap>
          {actions.map(action => (
            <Card key={action.id} padding="var(--space-3)">
              <Stack gap="2">
                <Row gap="2" align="center">
                  <LucideIcon name={action.icon} size={18} />
                  <Label variant="primary" size="sm" weight="semibold">
                    {action.actionName}
                  </Label>
                </Row>
                {action.actionDescription && (
                  <Label variant="muted" size="xs">
                    {action.actionDescription}
                  </Label>
                )}
                <Button
                  variant={action.risk === 'Safe' ? 'secondary' : 'danger'}
                  size="sm"
                  icon={<LucideIcon name={action.icon} size={14} />}
                  onClick={() => onClick(action)}
                  isLoading={runningId === action.id}
                  disabled={runningId !== null}
                >
                  {t('services:customActions.run')}
                </Button>
              </Stack>
            </Card>
          ))}
        </Row>
      )}

      <Modal
        isOpen={!!confirmTarget}
        onClose={() => setConfirmTarget(null)}
        title={t('services:customActions.confirmTitle', { name: confirmTarget?.actionName })}
        description={t('services:customActions.confirmDescription')}
        size="sm"
        footer={
          <Row gap="2" justify="flex-end" full>
            <Button
              variant="ghost"
              onClick={() => setConfirmTarget(null)}
              disabled={runningId !== null}
            >
              {t('services:customActions.cancel')}
            </Button>
            <Button
              variant="danger"
              onClick={() => confirmTarget && run(confirmTarget)}
              isLoading={runningId !== null}
            >
              {t('services:customActions.run')}
            </Button>
          </Row>
        }
      >
        {null}
      </Modal>
    </Stack>
  );
}
