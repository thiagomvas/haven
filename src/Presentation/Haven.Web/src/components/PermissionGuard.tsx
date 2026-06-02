import type { ReactNode } from 'react'
import { usePermission } from '@/hooks/usePermission'

interface Props {
  permission: string
  children: ReactNode
}

export function PermissionGuard({ permission, children }: Props) {
  const has = usePermission(permission)
  return has ? <>{children}</> : null
}
