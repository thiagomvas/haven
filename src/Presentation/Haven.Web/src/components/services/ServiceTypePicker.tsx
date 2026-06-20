import { Container, FileCode, Layers, Terminal } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import type { TFunction } from 'i18next';
import type { ServiceType } from '../../api/types';
import styles from './ServiceTypePicker.module.css';

interface ServiceTypeOption {
  type: ServiceType;
  label: string;
  description: string;
  icon: React.ReactNode;
}

const getOptions = (t: TFunction<'services'>): ServiceTypeOption[] => [
  {
    type: 'DockerImage',
    label: t('createPage.dockerImageType'),
    description: t('createPage.dockerImageTypeDescription'),
    icon: <Container size={28} />,
  },
  {
    type: 'Dockerfile',
    label: t('createPage.dockerfileType'),
    description: t('createPage.dockerfileTypeDescription'),
    icon: <FileCode size={28} />,
  },
  {
    type: 'Compose',
    label: t('createPage.composeType'),
    description: t('createPage.composeTypeDescription'),
    icon: <Layers size={28} />,
  },
  {
    type: 'Process',
    label: t('createPage.processType'),
    description: t('createPage.processTypeDescription'),
    icon: <Terminal size={28} />,
  },
];

interface ServiceTypePickerProps {
  value: ServiceType;
  onChange: (type: ServiceType) => void;
  disabled?: boolean;
}

export function ServiceTypePicker({ value, onChange, disabled }: ServiceTypePickerProps) {
  const { t } = useTranslation('services');
  const options = getOptions(t);

  return (
    <div className={styles.typeGrid}>
      {options.map(opt => (
        <button
          key={opt.type}
          type="button"
          className={`${styles.typeCard} ${value === opt.type ? styles.selected : ''}`}
          onClick={() => onChange(opt.type)}
          disabled={disabled}
        >
          <div className={styles.typeIcon}>{opt.icon}</div>
          <span className={styles.typeLabel}>{opt.label}</span>
          <span className={styles.typeDesc}>{opt.description}</span>
        </button>
      ))}
    </div>
  );
}
