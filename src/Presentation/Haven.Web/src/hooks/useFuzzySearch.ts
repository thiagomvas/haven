import { useQuery } from '@tanstack/react-query';
import { useEffect, useState } from 'react';

import { fuzzySearchApi } from '@/api/fuzzySearch';

import { usePermission } from './usePermission';

export function useFuzzySearch(query: string, count = 10, scopes?: readonly string[]) {
  const canSearch = usePermission('projects.read');
  const [debouncedQuery, setDebouncedQuery] = useState('');

  useEffect(() => {
    const timer = setTimeout(() => setDebouncedQuery(query), 200);
    return () => clearTimeout(timer);
  }, [query]);

  const { data, isLoading, error } = useQuery({
    queryKey: ['fuzzySearch', debouncedQuery, count, scopes],
    queryFn: () => fuzzySearchApi.search(debouncedQuery, count, scopes),
    enabled: debouncedQuery.length >= 1 && canSearch,
    staleTime: 0,
  });

  return { results: data ?? [], isLoading, error };
}
