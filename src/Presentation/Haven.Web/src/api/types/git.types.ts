import { GitProviderType, GitAuthMethod } from './git.types';

export type GitProviderType = 'Generic' | 'GitHub' | 'GitLab' | 'Bitbucket' | 'Gitea';
export type GitAuthMethod = 'Token' | 'Ssh';
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
export interface GetGitCredentialsParams {
  pageNumber?: number;
  pageSize?: number;
}
