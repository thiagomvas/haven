import { Zap } from 'lucide-react';
import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';

import { CustomActionDto } from '@/api/types';
import { usePermission } from '@/hooks/usePermission';
import styles from '@/styles/components/services/CustomActionsCard.module.css';

import { customActionsApi } from '../../api/customActions';
import { Row, Stack } from '../layout';
import { Banner } from '../ui/Banner';
import { Button } from '../ui/Button';
import { Card, CardContent, CardHeader, CardTitle } from '../ui/Card';
import { LucideIcon } from '../ui/LucideIcon';
import { Modal } from '../ui/Modal';

interface CustomActionsCardProps {
  projectId: string;
  environmentId: string;
  serviceId: string;
}

type Outcome = { variant: 'success' | 'error'; message: string };

/** Overview card listing a service's custom actions with a run button each. Renders nothing if there are none. */
export function CustomActionsCard({ projectId, environmentId, serviceId }: CustomActionsCardProps) {
  const { t } = useTranslation(['services']);
  const canRun = usePermission('projects.manage_deploys');

  const [actions, setActions] = useState<CustomActionDto[]>([]);
  const [runningId, setRunningId] = useState<string | null>(null);
  const [confirmTarget, setConfirmTarget] = useState<CustomActionDto | null>(null);
  const [outcome, setOutcome] = useState<Outcome | null>(null);

  useEffect(() => {
    if (!canRun) return;
    let cancelled = false;
    customActionsApi
      .list(projectId, environmentId, serviceId)
      .then(result => {
        if (!cancelled) setActions(result ?? []);
      })
      .catch(() => {
        if (!cancelled) setActions([]);
      });
    return () => {
      cancelled = true;
    };
  }, [canRun, projectId, environmentId, serviceId]);

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

  const handleRun = (action: CustomActionDto) => {
    if (action.risk === 'RequireConfirmation') setConfirmTarget(action);
    else void run(action);
  };

  if (!canRun || actions.length === 0) return null;

  return (
    <>
      <Card padding="var(--space-4)">
        <CardHeader>
          <CardTitle>
            <Row gap="2" align="center">
              <Zap size={16} />
              {t('services:customActions.title')}
            </Row>
          </CardTitle>
        </CardHeader>
        <CardContent>
          <Stack gap="3">
            {outcome && <Banner variant={outcome.variant} description={outcome.message} />}
            <div className={styles.grid}>
              {actions.map(action => (
                <Button
                  key={action.id}
                  variant={action.risk === 'RequireConfirmation' ? 'danger' : 'secondary'}
                  size="md"
                  align="left"
                  icon={<LucideIcon name={action.icon} size={20} />}
                  title={action.actionDescription || action.actionName}
                  onClick={() => handleRun(action)}
                  isLoading={runningId === action.id}
                  disabled={runningId !== null}
                >
                  {action.actionName}
                </Button>
              ))}
            </div>
          </Stack>
        </CardContent>
      </Card>

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
    </>
  );
}
