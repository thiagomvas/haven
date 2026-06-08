import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { instanceApi, UpdateInstanceInput } from '@/api/instance'

export function useInstance() {
  return useQuery({
    queryKey: ['instance'],
    queryFn: instanceApi.get,
  })
}

export function useUpdateInstance() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (data: UpdateInstanceInput) => instanceApi.update(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['instance'] })
    },
  })
}
