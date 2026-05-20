import { apiClient } from './client'

export const gitApi = {
  getRemoteBranches: (repositoryUrl: string, gitCredentialId?: string) =>
    apiClient.get<string[]>('/git/branches', {
      repositoryUrl,
      gitCredentialId,
    }),
}
