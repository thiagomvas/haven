export type VolumeType = 'Named' | 'HostPath' | 'Managed';

export interface ServiceVolumeDto {
  id: string;
  serviceId: string;
  type: VolumeType;
  name: string;
  source?: string;
  target: string;
  readOnly: boolean;
  backupEnabled: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface AddVolumeInput {
  type: VolumeType;
  name: string;
  target: string;
  source?: string;
  readOnly: boolean;
  backupEnabled: boolean;
}

export interface UpdateVolumeInput {
  name?: string;
  source?: string;
  target?: string;
  readOnly?: boolean;
  backupEnabled?: boolean;
}

export interface ManagedVolumeFileEntry {
  path: string;
  isDirectory: boolean;
  size: number;
}
