import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  CreateProjectInput,
  GetProjectsParams,
  PagedResult,
  ProjectDto,
  UpdateProjectInput,
} from '@/api/types'
import { projectsApi } from '@/api/projects'

const PROJECTS_KEY = 'projects'

export function useProjects(params?: GetProjectsParams) {
  return useQuery({
    queryKey: [PROJECTS_KEY, params],
    queryFn: () => projectsApi.getAll(params),
  })
}

export function useCreateProject() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: projectsApi.create,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: [PROJECTS_KEY] })
    },
  })
}

export function useUpdateProject() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateProjectInput }) =>
      projectsApi.update(id, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: [PROJECTS_KEY] })
    },
  })
}

export function useDeleteProject() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: projectsApi.delete,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: [PROJECTS_KEY] })
    },
  })
}
