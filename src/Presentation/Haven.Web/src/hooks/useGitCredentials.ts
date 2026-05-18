import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { gitCredentialsApi } from '@/api/gitCredentials'
import { GetGitCredentialsParams, CreateGitCredentialInput } from '@/api/types'

export function useGitCredentials(params?: GetGitCredentialsParams) {
  return useQuery({
    queryKey: ['gitCredentials', params],
    queryFn: () => gitCredentialsApi.getAll(params),
  })
}

export function useCreateGitCredential() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (data: CreateGitCredentialInput) => gitCredentialsApi.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['gitCredentials'] })
    },
  })
}
