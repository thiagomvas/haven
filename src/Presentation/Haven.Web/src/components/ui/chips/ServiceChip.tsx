import { Chip } from '../Chip';
import { HealthIndicator } from '../HealthIndicator';

export interface ServiceChipProps {
  serviceName: string;
  health: 'healthy' | 'degraded' | 'stopped' | 'died' | 'muted' | 'running' | 'unknown' | string;
  size?: 'sm' | 'md' | 'lg';
}

export function ServiceChip({ serviceName, health, size = 'sm' }: ServiceChipProps) {
  return <Chip icon={<HealthIndicator health={health} />} content={serviceName} size={size} />;
}
