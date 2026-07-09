import { apiClient } from './client';

export interface GitHubAppSettingsDto {
  clientId: string;
  redirectUri: string;
  isConfigured: boolean;
}

export interface UpdateGitHubAppSettingsInput {
  clientId: string;
  clientSecret?: string;
}

export const githubAppApi = {
  get: () => apiClient.get<GitHubAppSettingsDto>('/configuration/github-app'),
  update: (data: UpdateGitHubAppSettingsInput) =>
    apiClient.put<GitHubAppSettingsDto>('/configuration/github-app', data),
};
