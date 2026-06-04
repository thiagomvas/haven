import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  CreateEnvironmentInput,
  EnvironmentDto,
  UpdateEnvironmentInput,
} from '@/api/types'
import { environmentsApi } from '@/api/environments'
import { usePermission } from './usePermission'

const ENVIRONMENTS_KEY = 'environments'

export function useEnvironments(projectId: string) {
  const canView = usePermission('projects.read')
  return useQuery({
    queryKey: [ENVIRONMENTS_KEY, projectId],
    queryFn: () => environmentsApi.getByProjectId(projectId),
    enabled: !!projectId && canView,
  })
}

export function useCreateEnvironment() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({
      projectId,
      data,
    }: {
      projectId: string
      data: CreateEnvironmentInput
    }) => environmentsApi.create(projectId, data),
    onSuccess: (_, { projectId }) => {
      qc.invalidateQueries({
        queryKey: [ENVIRONMENTS_KEY, projectId],
      })
    },
  })
}

export function useUpdateEnvironment() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({
      projectId,
      environmentId,
      data,
    }: {
      projectId: string
      environmentId: string
      data: UpdateEnvironmentInput
    }) =>
      environmentsApi.update(projectId, environmentId, data),
    onSuccess: (_, { projectId }) => {
      qc.invalidateQueries({
        queryKey: [ENVIRONMENTS_KEY, projectId],
      })
    },
  })
}

export function useDeleteEnvironment() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({
      projectId,
      environmentId,
    }: {
      projectId: string
      environmentId: string
    }) => environmentsApi.delete(projectId, environmentId),
    onSuccess: (_, { projectId }) => {
      qc.invalidateQueries({
        queryKey: [ENVIRONMENTS_KEY, projectId],
      })
    },
  })
}
