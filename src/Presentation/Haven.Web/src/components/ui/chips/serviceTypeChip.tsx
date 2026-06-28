import { ServiceType } from '@/api/types/service.types';
import { Chip } from '../Chip';
import { Container, FileCode, Layers, Terminal } from 'lucide-react';

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
    color: '#3498db',
  },
  Dockerfile: {
    label: 'Dockerfile',
    icon: <FileCode size={16} />,
    color: '#9b59b6',
  },
  Compose: {
    label: 'Docker Compose',
    icon: <Layers size={16} />,
    color: '#1abc9c',
  },
  Process: {
    label: 'Process',
    icon: <Terminal size={16} />,
    color: '#e74c3c',
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
