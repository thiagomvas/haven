import { Container, FileCode, Layers, Terminal } from 'lucide-react';

import { ServiceType } from '@/api/types';

import { Chip } from '../Chip';

export interface ServiceTypeChipProps {
  serviceType: ServiceType;
  size?: 'sm' | 'md' | 'lg';
}

const serviceTypeConfig: Record<
  ServiceType,
  { label: string; icon: React.ReactNode; color: string }
> = {
  DockerImage: {
    label: 'Docker Image',
    icon: <Container size={16} />,
    color: 'var(--color-deploying)',
  },
  Dockerfile: {
    label: 'Dockerfile',
    icon: <FileCode size={16} />,
    color: 'var(--color-deployment-pending)',
  },
  Compose: {
    label: 'Docker Compose',
    icon: <Layers size={16} />,
    color: 'var(--color-info)',
  },
  Process: {
    label: 'Process',
    icon: <Terminal size={16} />,
    color: 'var(--color-danger)',
  },
};

export function ServiceTypeChip({ serviceType, size = 'sm' }: ServiceTypeChipProps) {
  const config = serviceTypeConfig[serviceType];

  if (!config) {
    return <Chip content={serviceType} size={size} />;
  }

  return (
    <Chip
      icon={config.icon}
      content={config.label}
      size={size}
      borderColor={config.color}
      textColor={config.color}
    />
  );
}
