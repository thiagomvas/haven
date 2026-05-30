import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { CreateUserInput, UserDto } from '@/api/types'
import { usersApi } from '@/api/users'

const USERS_KEY = 'users'

export function useUsers() {
  return useQuery({
    queryKey: [USERS_KEY],
    queryFn: () => usersApi.getAll(),
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
