import type { TFunction } from 'i18next';
import { Container, FileCode, LayoutTemplate } from 'lucide-react';
import { useTranslation } from 'react-i18next';

import type { ServiceTemplateSummaryDto, ServiceType } from '@/api/types';
import styles from '@/styles/components/services/ServiceTypePicker.module.css';

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
];

interface ServiceTypePickerProps {
  value: ServiceType;
  onChange: (type: ServiceType) => void;
  disabled?: boolean;
  selectedTemplate?: ServiceTemplateSummaryDto | null;
  onPickTemplate: () => void;
}

export function ServiceTypePicker({
  value,
  onChange,
  disabled,
  selectedTemplate,
  onPickTemplate,
}: ServiceTypePickerProps) {
  const { t } = useTranslation('services');
  const options = getOptions(t);

  return (
    <div className={styles.typeGrid}>
      {options.map(opt => (
        <button
          key={opt.type}
          type="button"
          className={`${styles.typeCard} ${value === opt.type && !selectedTemplate ? styles.selected : ''}`}
          onClick={() => onChange(opt.type)}
          disabled={disabled}
        >
          <div className={styles.typeIcon}>{opt.icon}</div>
          <span className={styles.typeLabel}>{opt.label}</span>
          <span className={styles.typeDesc}>{opt.description}</span>
        </button>
      ))}

      <button
        key="template"
        type="button"
        className={`${styles.typeCard} ${selectedTemplate ? styles.selected : ''}`}
        onClick={onPickTemplate}
        disabled={disabled}
      >
        <div className={styles.typeIcon}>
          <LayoutTemplate size={28} />
        </div>
        <span className={styles.typeLabel}>
          {selectedTemplate ? selectedTemplate.name : 'Pick from Template'}
        </span>
        <span className={styles.typeDesc}>
          {selectedTemplate
            ? `${selectedTemplate.category} template — click to change`
            : 'Choose from built-in templates'}
        </span>
      </button>
    </div>
  );
}
