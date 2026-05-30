import { useEffect, useState } from 'react'
import { authApi, MeResponse } from '@/api/auth'

export function useCurrentUser() {
  const [user, setUser] = useState<MeResponse | null>(null)

  useEffect(() => {
    authApi.me().then(setUser).catch(() => setUser(null))
  }, [])

  return user
}
