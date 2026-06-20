import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { AlertCircle, Home, ArrowLeft } from 'lucide-react';
import { useSetBreadcrumbs } from '@/hooks/useSetBreadcrumbs';
import styles from './NotFoundPage.module.css';

export function NotFoundPage() {
  const { t } = useTranslation('pages');
  const navigate = useNavigate();

  useSetBreadcrumbs([{ label: 'Not Found' }]);

  return (
    <div className={styles.container}>
      <div className={styles.content}>
        <div className={styles.iconWrapper}>
          <AlertCircle size={64} className={styles.icon} />
        </div>

        <h1 className={styles.title}>{t('notFound.title')}</h1>
        <p className={styles.message}>{t('notFound.message')}</p>

        <div className={styles.actions}>
          <button className={styles.primaryButton} onClick={() => navigate('/dashboard')}>
            <Home size={18} />
            {t('notFound.goHome')}
          </button>
          <button className={styles.secondaryButton} onClick={() => navigate(-1)}>
            <ArrowLeft size={18} />
            {t('notFound.goBack')}
          </button>
        </div>

        <p className={styles.footer}>{t('notFound.footer')}</p>
      </div>
    </div>
  );
}
