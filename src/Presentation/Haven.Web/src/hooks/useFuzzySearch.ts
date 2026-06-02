import { useEffect, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { fuzzySearchApi } from '@/api/fuzzySearch'
import { usePermission } from './usePermission'

export function useFuzzySearch(query: string, count = 10) {
  const canSearch = usePermission('projects.view')
  const [debouncedQuery, setDebouncedQuery] = useState('')

  useEffect(() => {
    const timer = setTimeout(() => setDebouncedQuery(query), 200)
    return () => clearTimeout(timer)
  }, [query])

  const { data, isLoading, error } = useQuery({
    queryKey: ['fuzzySearch', debouncedQuery, count],
    queryFn: () => fuzzySearchApi.search(debouncedQuery, count),
    enabled: debouncedQuery.length >= 1 && canSearch,
    staleTime: 0,
  })

  return { results: data ?? [], isLoading, error }
}
