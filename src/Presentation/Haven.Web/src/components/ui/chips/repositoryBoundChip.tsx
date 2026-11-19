import { GitBranch } from 'lucide-react';
import { useTranslation } from 'react-i18next';

import { RepositoryBindingDto } from '@/api/types';

import { Chip } from '../Chip';

export interface RepositoryBoundChipProps {
  binding: RepositoryBindingDto;
  size?: 'sm' | 'md' | 'lg';
}

export function RepositoryBoundChip({ binding, size = 'sm' }: RepositoryBoundChipProps) {
  const { t } = useTranslation('services');

  return (
    <Chip
      icon={<GitBranch size={16} />}
      content={t('repositoryBound.chip')}
      size={size}
      borderColor="var(--color-warning)"
      textColor="var(--color-warning)"
      title={t('repositoryBound.tooltip', {
        filePath: binding.filePath ?? '',
        branch: binding.branch ?? '',
      })}
    />
  );
}
