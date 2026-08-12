import { apiClient } from './client';

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
};
