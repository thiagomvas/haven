import { useTranslation } from 'react-i18next';
import styles from './RoleSelector.module.css';

interface Props {
  value: 'user' | 'admin';
  onChange: (value: 'user' | 'admin') => void;
  disabled?: boolean;
}

export function RoleSelector({ value, onChange, disabled = false }: Props) {
  const { t } = useTranslation('settings');

  return (
    <div className={styles.container}>
      <label className={styles.option}>
        <input
          type="radio"
          name="role"
          value="user"
          checked={value === 'user'}
          onChange={() => onChange('user')}
          disabled={disabled}
        />
        <div className={styles.optionContent}>
          <div className={styles.optionTitle}>{t('users.roles.user')}</div>
          <div className={styles.optionDescription}>{t('users.roles.userDescription')}</div>
        </div>
      </label>

      <label className={styles.option}>
        <input
          type="radio"
          name="role"
          value="admin"
          checked={value === 'admin'}
          onChange={() => onChange('admin')}
          disabled={disabled}
        />
        <div className={styles.optionContent}>
          <div className={styles.optionTitle}>{t('users.roles.admin')}</div>
          <div className={styles.optionDescription}>{t('users.roles.adminDescription')}</div>
        </div>
      </label>
    </div>
  );
}
