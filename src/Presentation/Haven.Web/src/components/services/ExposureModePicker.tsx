import type { TFunction } from 'i18next';
import { Globe, Lock, SlidersHorizontal, Wifi } from 'lucide-react';
import { useTranslation } from 'react-i18next';

import type { ExposureMode } from '@/api/types';
import styles from '@/styles/components/services/ExposureModePicker.module.css';

interface ExposureModeOption {
  mode: ExposureMode;
  label: string;
  description: string;
  icon: React.ReactNode;
}

const getOptions = (t: TFunction<'services'>): ExposureModeOption[] => [
  {
    mode: 'None',
    label: t('createPage.exposureNone'),
    description: t('createPage.exposureNoneDescription'),
    icon: <Lock size={20} />,
  },
  {
    mode: 'Internal',
    label: t('createPage.exposureInternal'),
    description: t('createPage.exposureInternalDescription'),
    icon: <Wifi size={20} />,
  },
  {
    mode: 'External',
    label: t('createPage.exposureExternal'),
    description: t('createPage.exposureExternalDescription'),
    icon: <Globe size={20} />,
  },
  {
    mode: 'Custom',
    label: t('createPage.exposureCustom'),
    description: t('createPage.exposureCustomDescription'),
    icon: <SlidersHorizontal size={20} />,
  },
];

interface ExposureModePickerProps {
  value: ExposureMode;
  onChange: (mode: ExposureMode) => void;
  disabled?: boolean;
}

export function ExposureModePicker({ value, onChange, disabled }: ExposureModePickerProps) {
  const { t } = useTranslation('services');
  const options = getOptions(t);

  return (
    <div className={styles.exposureGrid}>
      {options.map(({ mode, label, description, icon }) => (
        <button
          key={mode}
          type="button"
          className={`${styles.exposureCard} ${value === mode ? styles.selected : ''}`}
          onClick={() => onChange(mode)}
          disabled={disabled}
        >
          <div className={styles.exposureIcon}>{icon}</div>
          <span className={styles.exposureLabel}>{label}</span>
          <span className={styles.exposureDescription}>{description}</span>
        </button>
      ))}
    </div>
  );
}
