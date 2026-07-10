import { useTranslation } from 'react-i18next';

import type { NotificationAttemptDto } from '@/api/types';
import { CodeBlock } from '@/components/ui/CodeBlock';
import { Modal } from '@/components/ui/Modal';
import styles from '@/styles/components/notifications/AttemptPayloadModal.module.css';

function formatJson(value: string | null): string | null {
  if (!value) return value;
  try {
    return JSON.stringify(JSON.parse(value), null, 2);
  } catch {
    return value;
  }
}

interface AttemptPayloadModalProps {
  attempt: NotificationAttemptDto | null;
  onClose: () => void;
}

export function AttemptPayloadModal({ attempt, onClose }: AttemptPayloadModalProps) {
  const { t } = useTranslation('notificationChannels');

  return (
    <Modal
      isOpen={attempt !== null}
      onClose={onClose}
      title={t('attemptsTab.payloadModal.title', { event: attempt?.eventType })}
      size="lg"
    >
      {attempt && (
        <div className={styles.sections}>
          <CodeBlock
            header={t('attemptsTab.payloadModal.eventPayload')}
            code={formatJson(attempt.eventPayload) ?? ''}
            copyable
          />
          <CodeBlock
            header={t('attemptsTab.payloadModal.sentPayload')}
            code={formatJson(attempt.payload) ?? t('attemptsTab.payloadModal.none')}
            copyable={attempt.payload !== null}
          />
          <CodeBlock
            header={t('attemptsTab.payloadModal.response')}
            code={formatJson(attempt.response) ?? t('attemptsTab.payloadModal.none')}
            copyable={attempt.response !== null}
          />
        </div>
      )}
    </Modal>
  );
}
