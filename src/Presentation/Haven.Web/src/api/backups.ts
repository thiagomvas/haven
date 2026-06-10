import { apiClient } from './client';

export interface BackupGitOptions {
  enabled: boolean;
  remoteUrl?: string;
  branch: string;
  gitCredentialsId?: string;
}

export interface BackupOptions {
  enabled: boolean;
  backupsPath: string;
  retentionCount: number;
  git: BackupGitOptions;
}

export interface CreateBackupResult {
  snapshotPath: string;
  createdAt: string;
}

export const backupsApi = {
  getOptions: () => apiClient.get<BackupOptions>('/backups/options'),
  updateOptions: (data: BackupOptions) => apiClient.put<BackupOptions>('/backups/options', { options: data }),
  createBackup: () => apiClient.post<CreateBackupResult>('/backups', {}),
};
