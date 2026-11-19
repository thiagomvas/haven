import { apiClient } from './client';
import type { TemplateSuggestionsDto } from './types';

export type EnvironmentVariableParentType = 'Project' | 'Environment' | 'Service';

export interface ExportEnvExampleInput {
  parentId: string;
  parentType: EnvironmentVariableParentType;
  includeValues: boolean;
  includeFeatureFlags: boolean;
}

export const environmentVariablesApi = {
  exportExample: (input: ExportEnvExampleInput) =>
    apiClient.get<string>('/env/export-example', input),

  getTemplateSuggestionsForProject: (projectId: string) =>
    apiClient.get<TemplateSuggestionsDto>(`/projects/${projectId}/template-suggestions`),

  getTemplateSuggestionsForEnvironment: (projectId: string, environmentId: string) =>
    apiClient.get<TemplateSuggestionsDto>(
      `/projects/${projectId}/environments/${environmentId}/template-suggestions`
    ),

  getTemplateSuggestionsForService: (projectId: string, environmentId: string, serviceId: string) =>
    apiClient.get<TemplateSuggestionsDto>(
      `/projects/${projectId}/environments/${environmentId}/services/${serviceId}/template-suggestions`
    ),
};
