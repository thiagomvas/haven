import { useEffect, useState } from 'react';

import { gitApi } from '../api/git';
import { GitRepositorySummaryDto } from '../api/types';
import { usePermission } from './usePermission';

export function useRepositoryAutocomplete(gitCredentialId?: string) {
  const canView = usePermission('system.read_git_credentials');
  const [repositories, setRepositories] = useState<GitRepositorySummaryDto[]>([]);
  const [loadedFor, setLoadedFor] = useState<string | undefined>(undefined);

  const enabled = !!gitCredentialId && canView;

  useEffect(() => {
    if (!enabled || !gitCredentialId) {
      return;
    }

    let cancelled = false;

    gitApi
      .getAccessibleRepositories(gitCredentialId)
      .then(result => {
        if (!cancelled) setRepositories(result ?? []);
      })
      .catch(() => {
        if (!cancelled) setRepositories([]);
      })
      .finally(() => {
        if (!cancelled) setLoadedFor(gitCredentialId);
      });

    return () => {
      cancelled = true;
    };
  }, [enabled, gitCredentialId]);

  return {
    repositories: enabled ? repositories : [],
    isLoading: enabled && loadedFor !== gitCredentialId,
  };
}
