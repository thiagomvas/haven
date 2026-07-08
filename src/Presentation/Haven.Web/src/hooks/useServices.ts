import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { servicesApi } from '@/api/services';
import { CreateServiceInput } from '@/api/types';
import { ServiceDto } from '@/api/types';

import { usePermission } from './usePermission';

const SERVICES_KEY = 'services';

export function useServices(projectId: string, environmentId: string) {
  const canView = usePermission('projects.read');
  return useQuery({
    queryKey: [SERVICES_KEY, projectId, environmentId],
    queryFn: () => servicesApi.getByEnvironmentId(projectId, environmentId),
    enabled: !!projectId && !!environmentId && canView,
  });
}

export function useCreateService() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      projectId,
      environmentId,
      data,
    }: {
      projectId: string;
      environmentId: string;
      data: CreateServiceInput;
    }) => servicesApi.create(projectId, environmentId, data),
    onSuccess: (_, { projectId, environmentId }) => {
      qc.invalidateQueries({
        queryKey: [SERVICES_KEY, projectId, environmentId],
      });
    },
  });
}

export function useDeployService() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      projectId,
      environmentId,
      serviceId,
    }: {
      projectId: string;
      environmentId: string;
      serviceId: string;
    }) => servicesApi.deploy(projectId, environmentId, serviceId),
    onSuccess: (_, { projectId, environmentId }) => {
      qc.invalidateQueries({
        queryKey: [SERVICES_KEY, projectId, environmentId],
      });
    },
  });
}

export function useRestartService() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      projectId,
      environmentId,
      serviceId,
    }: {
      projectId: string;
      environmentId: string;
      serviceId: string;
    }) => servicesApi.restart(projectId, environmentId, serviceId),
    onSuccess: (_, { projectId, environmentId }) => {
      qc.invalidateQueries({
        queryKey: [SERVICES_KEY, projectId, environmentId],
      });
    },
  });
}

export function useStopService() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      projectId,
      environmentId,
      serviceId,
    }: {
      projectId: string;
      environmentId: string;
      serviceId: string;
    }) => servicesApi.stop(projectId, environmentId, serviceId),
    onSuccess: (_, { projectId, environmentId }) => {
      qc.invalidateQueries({
        queryKey: [SERVICES_KEY, projectId, environmentId],
      });
    },
  });
}
