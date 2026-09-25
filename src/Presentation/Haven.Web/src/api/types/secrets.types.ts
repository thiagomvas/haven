export type SecretParentType = 'Project' | 'Environment' | 'Service';

export interface SecretsScope {
  parentType: SecretParentType;
  projectId: string;
  environmentId?: string;
  serviceId?: string;
}

export interface SecretVariableDto {
  id: string;
  parentId: string;
  parentType: SecretParentType;
  key: string;
  hasValue: boolean;
}

export interface CreateSecretInput {
  key: string;
  value: string;
}

export interface UpdateSecretInput {
  key?: string;
  value?: string;
}

export interface GetSecretsParams {
  pageNumber?: number;
  pageSize?: number;
}
