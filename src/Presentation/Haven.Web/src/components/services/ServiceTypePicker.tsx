import type { TFunction } from 'i18next';
import { Container, FileCode, FileText, LayoutTemplate } from 'lucide-react';
import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';

import type { ServiceTemplateSummaryDto, ServiceType } from '@/api/types';
import { Grid, Stack } from '@/components/layout';
import { Button } from '@/components/ui/Button';
import { Label } from '../ui/Label';

interface ServiceTypeOption {
  type: ServiceType;
  label: string;
  description: string;
  icon: ReactNode;
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

interface TypeCardProps {
  selected: boolean;
  disabled?: boolean;
  icon: ReactNode;
  label: string;
  description: string;
  onClick: () => void;
}

function TypeCard({ selected, disabled, icon, label, description, onClick }: TypeCardProps) {
  return (
    <Button
      variant={selected ? 'outline' : 'ghost'}
      onClick={onClick}
      disabled={disabled}
      aria-pressed={selected}
      paddingX={4}
      paddingY={4}
      style={{ whiteSpace: 'normal', height: '100%' }}
    >
      <Stack gap="2" align="center">
        {icon}
        <Label variant="primary" weight="bold" size="lg">
          {label}
        </Label>
        <Label>{description}</Label>
      </Stack>
    </Button>
  );
}

interface ServiceTypePickerProps {
  value: ServiceType;
  onChange: (type: ServiceType) => void;
  disabled?: boolean;
  selectedTemplate?: ServiceTemplateSummaryDto | null;
  onPickTemplate: () => void;
  onPickManifest: () => void;
}

export function ServiceTypePicker({
  value,
  onChange,
  disabled,
  selectedTemplate,
  onPickTemplate,
  onPickManifest,
}: ServiceTypePickerProps) {
  const { t } = useTranslation('services');
  const options = getOptions(t);

  return (
    <Grid columnTemplate="repeat(auto-fit, minmax(180px, 1fr))" gap="3">
      {options.map(opt => (
        <TypeCard
          key={opt.type}
          selected={value === opt.type && !selectedTemplate}
          disabled={disabled}
          icon={opt.icon}
          label={opt.label}
          description={opt.description}
          onClick={() => onChange(opt.type)}
        />
      ))}

      <TypeCard
        selected={!!selectedTemplate}
        disabled={disabled}
        icon={<LayoutTemplate size={28} />}
        label={selectedTemplate ? selectedTemplate.name : t('createPage.pickFromTemplate')}
        description={
          selectedTemplate
            ? t('createPage.selectedTemplateCategory', { category: selectedTemplate.category })
            : t('createPage.pickATemplate')
        }
        onClick={onPickTemplate}
      />

      <TypeCard
        selected={false}
        disabled={false}
        icon={<FileText size={28} />}
        label={t('createPage.importFromManifest')}
        description={t('createPage.importFromManifestDescription')}
        onClick={onPickManifest}
      />
    </Grid>
  );
}
