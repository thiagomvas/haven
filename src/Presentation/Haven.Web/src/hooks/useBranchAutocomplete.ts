import { useState, useEffect, useRef } from 'react';
import { gitApi } from '../api/git';
import { usePermission } from './usePermission';

export function useBranchAutocomplete(repositoryUrl: string, gitCredentialId?: string) {
  const canView = usePermission('system.read_git_credentials');
  const [branches, setBranches] = useState<string[]>([]);
  const [lastFetchedUrl, setLastFetchedUrl] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    if (!repositoryUrl.trim() || !canView) return;

    let cancelled = false;

    if (debounceRef.current) clearTimeout(debounceRef.current);
    debounceRef.current = setTimeout(async () => {
      try {
        new URL(repositoryUrl);
      } catch {
        return;
      }

      setIsLoading(true);
      try {
        const result = await gitApi.getRemoteBranches(repositoryUrl, gitCredentialId);
        if (!cancelled) {
          setBranches(result ?? []);
          setLastFetchedUrl(repositoryUrl);
        }
      } catch {
        if (!cancelled) setBranches([]);
      } finally {
        if (!cancelled) setIsLoading(false);
      }
    }, 600);

    return () => {
      cancelled = true;
      if (debounceRef.current) clearTimeout(debounceRef.current);
    };
  }, [repositoryUrl, gitCredentialId, canView]);

  // Only expose branches when they match the current URL; otherwise return empty
  // to avoid showing stale results while a new fetch is pending or URL is invalid.
  const effectiveBranches =
    repositoryUrl.trim() && canView && lastFetchedUrl === repositoryUrl ? branches : [];

  return { branches: effectiveBranches, isLoading };
}
