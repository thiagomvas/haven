export type NetworkType = 'ProjectEnvironment' | 'Shared' | 'External';

export interface NetworkServiceDto {
  id: string;
  name: string;
  status: string;
  ipAddress?: string;
  projectId?: string;
  projectName?: string;
}

export interface NetworkDto {
  id: string;
  name: string;
  type: NetworkType;
  projectId?: string;
  projectName?: string;
  environmentId?: string;
  environmentName?: string;
  subnet?: string;
  gateway?: string;
  serviceCount: number;
  services: NetworkServiceDto[];
  createdAt: string;
}

/* Request Types */

export interface GetNetworksParams {
  type?: NetworkType;
}
