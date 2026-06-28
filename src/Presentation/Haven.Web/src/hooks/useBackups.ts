import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { BackupOptions, backupsApi, RestoreBackupRequest } from '@/api/backups';

export function useBackupOptions() {
  return useQuery({
    queryKey: ['backupOptions'],
    queryFn: backupsApi.getOptions,
  });
}

export function useUpdateBackupOptions() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: BackupOptions) => backupsApi.updateOptions(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['backupOptions'] });
    },
  });
}

export function useCreateBackup() {
  return useMutation({
    mutationFn: backupsApi.createBackup,
  });
}

export function useSnapshots() {
  return useQuery({
    queryKey: ['backupSnapshots'],
    queryFn: backupsApi.listSnapshots,
  });
}

export function useGitCommits() {
  return useQuery({
    queryKey: ['backupGitCommits'],
    queryFn: backupsApi.listGitCommits,
  });
}

export function useRestoreBackup() {
  return useMutation({
    mutationFn: (data: RestoreBackupRequest) => backupsApi.restore(data),
  });
}
