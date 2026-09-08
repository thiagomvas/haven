import { Sparkles } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import { useCurrentUser } from '@/hooks/useCurrentUser';
import { useLatestVersion } from '@/hooks/useLatestVersion';
import styles from '@/styles/components/layout/Sidebar.module.css';

import { Button } from '../ui/Button';
import { Modal } from '../ui/Modal';
import { Tooltip } from '../ui/Tooltip';

interface UpdateAvailableButtonProps {
  collapsed?: boolean;
}

export function UpdateAvailableButton({ collapsed = false }: UpdateAvailableButtonProps) {
  const { t } = useTranslation('layout');
  const user = useCurrentUser();
  const { data: latestVersion } = useLatestVersion();
  const [isModalOpen, setIsModalOpen] = useState(false);

  if (!user?.isAdmin || !latestVersion?.isUpdateAvailable) {
    return null;
  }

  const button = (
    <Button
      variant="outline"
      size="sm"
      icon={<Sparkles size={16} />}
      onClick={() => setIsModalOpen(true)}
      className={styles.updateButton}
      aria-label={t('sidebar.updateAvailable')}
    >
      {!collapsed && t('sidebar.updateAvailable')}
    </Button>
  );

  return (
    <>
      {collapsed ? <Tooltip content={t('sidebar.updateAvailable')}>{button}</Tooltip> : button}

      <Modal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        title={latestVersion.name ?? t('updateModal.title')}
        description={t('updateModal.description')}
        size="md"
        footer={
          latestVersion.htmlUrl ? (
            <Button href={latestVersion.htmlUrl} target="_blank" rel="noopener noreferrer">
              {t('updateModal.viewOnGithub')}
            </Button>
          ) : undefined
        }
      >
        <div className={styles.updateModalVersions}>
          <span>
            {t('updateModal.currentVersion')}: <strong>{latestVersion.currentVersion}</strong>
          </span>
          <span>
            {t('updateModal.latestVersion')}: <strong>{latestVersion.latestVersion}</strong>
          </span>
        </div>
        {latestVersion.body && (
          <pre className={styles.updateModalChangelog}>{latestVersion.body}</pre>
        )}
      </Modal>
    </>
  );
}
