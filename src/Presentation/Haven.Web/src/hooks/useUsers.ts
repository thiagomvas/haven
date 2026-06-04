import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { CreateUserInput, UserDto } from '@/api/types'
import { usersApi } from '@/api/users'
import { systemApi } from '@/api/system'
import { usePermission } from './usePermission'

const USERS_KEY = 'users'
const USER_PERMISSIONS_KEY = 'userPermissions'
const ALL_PERMISSIONS_KEY = 'allPermissions'

export function useUsers() {
  const canView = usePermission('system.read_users')
  return useQuery({
    queryKey: [USERS_KEY],
    queryFn: () => usersApi.getAll(),
    enabled: canView,
  })
}

export function useCreateUser() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (input: CreateUserInput) => usersApi.create(input),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: [USERS_KEY] })
    },
  })
}

export function useDeleteUser() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => usersApi.delete(id),
    onMutate: async (id: string) => {
      await qc.cancelQueries({ queryKey: [USERS_KEY] })
      const previous = qc.getQueryData<UserDto[]>([USERS_KEY])
      qc.setQueryData<UserDto[]>([USERS_KEY], (old) => old?.filter((u) => u.id !== id))
      return { previous }
    },
    onError: (_err, _id, context) => {
      if (context?.previous) {
        qc.setQueryData<UserDto[]>([USERS_KEY], context.previous)
      }
    },
    onSettled: () => {
      qc.invalidateQueries({ queryKey: [USERS_KEY] })
    },
  })
}

export function useUserPermissions(userId: string | null) {
  const canView = usePermission('system.read_users')
  return useQuery({
    queryKey: [USER_PERMISSIONS_KEY, userId],
    queryFn: () => usersApi.getPermissions(userId!),
    enabled: !!userId && canView,
  })
}

export function useAllPermissions() {
  const canManage = usePermission('users.manage_permissions')
  return useQuery({
    queryKey: [ALL_PERMISSIONS_KEY],
    queryFn: systemApi.getAllPermissions,
    enabled: canManage,
    staleTime: Infinity,
  })
}

export function useSetUserPermissions() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ userId, permissions }: { userId: string; permissions: string[] }) =>
      usersApi.setPermissions(userId, permissions),
    onSuccess: (_data, { userId }) => {
      qc.invalidateQueries({ queryKey: [USER_PERMISSIONS_KEY, userId] })
    },
  })
}
