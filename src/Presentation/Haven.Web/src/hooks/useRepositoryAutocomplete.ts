import { useEffect, useState } from 'react';

import { gitApi } from '../api/git';
import { GitRepositorySummaryDto } from '../api/types';
import { usePermission } from './usePermission';

export function useRepositoryAutocomplete(gitCredentialId?: string) {
  const canView = usePermission('system.read_git_credentials');
  const [repositories, setRepositories] = useState<GitRepositorySummaryDto[]>([]);
  const [isLoading, setIsLoading] = useState(false);

  useEffect(() => {
    if (!gitCredentialId || !canView) {
      setRepositories([]);
      return;
    }

    let cancelled = false;

    setIsLoading(true);
    gitApi
      .getAccessibleRepositories(gitCredentialId)
      .then(result => {
        if (!cancelled) setRepositories(result ?? []);
      })
      .catch(() => {
        if (!cancelled) setRepositories([]);
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, [gitCredentialId, canView]);

  return { repositories, isLoading };
}
