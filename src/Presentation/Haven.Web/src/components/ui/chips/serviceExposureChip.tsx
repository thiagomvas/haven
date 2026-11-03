import { Globe, Lock, SlidersHorizontal, Wifi } from 'lucide-react';

import { ExposureMode } from '@/api/types';

import { Chip } from '../Chip';

export interface ServiceExposureChipProps {
  exposureMode: ExposureMode;
  size?: 'sm' | 'md' | 'lg';
}

const exposureModeConfig: Record<
  ExposureMode,
  { label: string; icon: React.ReactNode; color: string }
> = {
  None: {
    label: 'Not Exposed',
    icon: <Lock size={16} />,
    color: 'var(--color-network-none)',
  },
  Internal: {
    label: 'Internal Network',
    icon: <Wifi size={16} />,
    color: 'var(--color-network-internal)',
  },
  External: {
    label: 'External Access',
    icon: <Globe size={16} />,
    color: 'var(--color-network-public)',
  },
  Custom: {
    label: 'Custom',
    icon: <SlidersHorizontal size={16} />,
    color: 'var(--color-deployment-pending)',
  },
};

export function ServiceExposureChip({ exposureMode, size = 'sm' }: ServiceExposureChipProps) {
  const config = exposureModeConfig[exposureMode];

  if (!config) {
    return <Chip content={exposureMode} size={size} />;
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
