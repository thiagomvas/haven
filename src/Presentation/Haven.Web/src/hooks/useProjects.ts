import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  PagedResult,
} from '@/api/types';
import { UpdateProjectInput } from "@/api/types/project.types";
import { CreateProjectInput } from "@/api/types/project.types";
import { GetProjectsParams } from "@/api/types/project.types";
import { ProjectDashboardDto } from "@/api/types/project.types";
import { ProjectDto } from "@/api/types/project.types";
import { projectsApi } from '@/api/projects';
import { usePermission } from './usePermission';

const PROJECTS_KEY = 'projects';
const PROJECTS_DASHBOARD_KEY = 'projects-dashboard';

export function useProjects(params?: GetProjectsParams) {
  const canView = usePermission('projects.read');
  return useQuery({
    queryKey: [PROJECTS_KEY, params],
    queryFn: () => projectsApi.getAll(params),
    enabled: canView,
  });
}

export function useProjectsDashboard(params?: GetProjectsParams) {
  const canView = usePermission('projects.read');
  return useQuery({
    queryKey: [PROJECTS_DASHBOARD_KEY, params],
    queryFn: () => projectsApi.getDashboard(params),
    enabled: canView,
  });
}

export function useCreateProject() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: projectsApi.create,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: [PROJECTS_KEY] });
    },
  });
}

export function useUpdateProject() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateProjectInput }) =>
      projectsApi.update(id, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: [PROJECTS_KEY] });
    },
  });
}

export function useDeleteProject() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: projectsApi.delete,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: [PROJECTS_KEY] });
    },
  });
}
