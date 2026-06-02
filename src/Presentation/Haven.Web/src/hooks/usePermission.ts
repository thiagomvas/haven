import { useCurrentUser } from './useCurrentUser'

export function usePermission(permission: string): boolean {
  const user = useCurrentUser()
  if (!user) return false
  if (user.isAdmin) return true
  return user.permissions.includes(permission)
}
