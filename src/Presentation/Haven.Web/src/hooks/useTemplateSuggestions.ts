import { useQuery } from '@tanstack/react-query';

import { environmentVariablesApi } from '@/api/environmentVariables';

import { usePermission } from './usePermission';

export interface TemplateSuggestionsScope {
  projectId: string;
  environmentId?: string;
  serviceId?: string;
}

/**
 * Loads the `${{ namespace.key }}` references available when editing the variables of the
 * given project, environment or service. Failures (e.g. a missing permission) just mean no
 * suggestions, so they are never retried or surfaced.
 */
export function useTemplateSuggestions({
  projectId,
  environmentId,
  serviceId,
}: TemplateSuggestionsScope) {
  const canView = usePermission('projects.manage_config');
  return useQuery({
    queryKey: ['template-suggestions', projectId, environmentId, serviceId],
    queryFn: () => {
      if (serviceId && environmentId)
        return environmentVariablesApi.getTemplateSuggestionsForService(
          projectId,
          environmentId,
          serviceId
        );
      if (environmentId)
        return environmentVariablesApi.getTemplateSuggestionsForEnvironment(
          projectId,
          environmentId
        );
      return environmentVariablesApi.getTemplateSuggestionsForProject(projectId);
    },
    enabled: canView,
    retry: false,
    staleTime: 30_000,
  });
}
