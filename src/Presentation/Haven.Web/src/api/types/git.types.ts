export type GitProviderType = 'Generic' | 'GitHub' | 'GitLab' | 'Bitbucket' | 'Gitea';
export type GitAuthMethod = 'Token' | 'Ssh' | 'OAuth';
/* Git Credentials */

export interface GitCredentialDto {
  id: string;
  providerType: GitProviderType;
  hostUrl?: string;
  authMethod: GitAuthMethod;
  displayName: string;
  isActive: boolean;
  lastValidatedAt: string;
}
export interface CreateGitCredentialInput {
  providerType: GitProviderType;
  hostUrl?: string;
  authMethod: GitAuthMethod;
  primaryCredential: string;
  secondaryCredential?: string;
  webhookSecret?: string;
  displayName: string;
}
export interface UpdateGitCredentialInput {
  displayName?: string;
  isActive?: boolean;
}
export interface GetGitCredentialsParams {
  pageNumber?: number;
  pageSize?: number;
}
