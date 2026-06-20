import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { backupsApi, BackupOptions } from '@/api/backups';

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
