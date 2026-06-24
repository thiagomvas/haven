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
  cronExpression: string;
  git: BackupGitOptions;
}

export interface CreateBackupResult {
  snapshotPath: string;
  createdAt: string;
}

export interface SnapshotInfo {
  name: string;
  createdAt: string | null;
}

export interface GitCommitInfo {
  sha: string;
  message: string;
  author: string;
  timestamp: string;
}

export type RestoreSource = 'FileSystem' | 'Git' | 'Manifest';

export interface RestoreBackupRequest {
  source: RestoreSource;
  snapshotName?: string;
  commitSha?: string;
  dryRun: boolean;
}

export interface EntityChangeSummary<T> {
  created: T[];
  updated: T[];
  deleted: T[];
}

export interface ProjectRestoreItem { id: string; name: string; }
export interface EnvironmentRestoreItem { id: string; name: string; projectId: string; projectName?: string; }
export interface NetworkRestoreItem { id: string; name: string; }
export interface ServiceRestoreItem { id: string; name: string; environmentId: string; environmentName?: string; projectName?: string; }
export interface EnvVarRestoreItem { key: string; parentId: string; parentName?: string; }

export interface RestoreBackupResult {
  dryRun: boolean;
  projects: EntityChangeSummary<ProjectRestoreItem>;
  environments: EntityChangeSummary<EnvironmentRestoreItem>;
  networks: EntityChangeSummary<NetworkRestoreItem>;
  services: EntityChangeSummary<ServiceRestoreItem>;
  environmentVariables: EntityChangeSummary<EnvVarRestoreItem>;
}

export const backupsApi = {
  getOptions: () => apiClient.get<BackupOptions>('/backups/options'),
  updateOptions: (data: BackupOptions) =>
    apiClient.put<BackupOptions>('/backups/options', { options: data }),
  createBackup: () => apiClient.post<CreateBackupResult>('/backups', {}),
  listSnapshots: () => apiClient.get<SnapshotInfo[]>('/backups/snapshots'),
  listGitCommits: () => apiClient.get<GitCommitInfo[]>('/backups/commits'),
  restore: (data: RestoreBackupRequest) =>
    apiClient.post<RestoreBackupResult>('/backups/restore', data),
};
