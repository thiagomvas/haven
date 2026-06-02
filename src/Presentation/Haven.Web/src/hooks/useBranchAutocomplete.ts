import { useState, useEffect, useRef } from 'react'
import { gitApi } from '../api/git'
import { usePermission } from './usePermission'

export function useBranchAutocomplete(repositoryUrl: string, gitCredentialId?: string) {
  const canView = usePermission('credentials.view')
  const [branches, setBranches] = useState<string[]>([])
  const [isLoading, setIsLoading] = useState(false)
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null)

  useEffect(() => {
    setBranches([])

    if (!repositoryUrl.trim() || !canView) return

    let cancelled = false

    if (debounceRef.current) clearTimeout(debounceRef.current)
    debounceRef.current = setTimeout(async () => {
      try {
        new URL(repositoryUrl)
      } catch {
        return
      }

      setIsLoading(true)
      try {
        const result = await gitApi.getRemoteBranches(repositoryUrl, gitCredentialId)
        if (!cancelled) setBranches(result ?? [])
      } catch {
        if (!cancelled) setBranches([])
      } finally {
        if (!cancelled) setIsLoading(false)
      }
    }, 600)

    return () => {
      cancelled = true
      if (debounceRef.current) clearTimeout(debounceRef.current)
    }
  }, [repositoryUrl, gitCredentialId])

  return { branches, isLoading }
}
