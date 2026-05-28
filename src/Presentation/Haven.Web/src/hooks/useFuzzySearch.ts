import { useEffect, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { fuzzySearchApi } from '@/api/fuzzySearch'

export function useFuzzySearch(query: string, count = 10) {
  const [debouncedQuery, setDebouncedQuery] = useState('')

  useEffect(() => {
    const timer = setTimeout(() => setDebouncedQuery(query), 200)
    return () => clearTimeout(timer)
  }, [query])

  const { data, isLoading, error } = useQuery({
    queryKey: ['fuzzySearch', debouncedQuery, count],
    queryFn: () => fuzzySearchApi.search(debouncedQuery, count),
    enabled: debouncedQuery.length >= 1,
    staleTime: 0,
  })

  return { results: data ?? [], isLoading, error }
}
