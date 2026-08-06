export type NetworkType = 'ProjectEnvironment' | 'Shared' | 'External';

export interface NetworkServiceDto {
  id: string;
  name: string;
  status: string;
}

export interface NetworkDto {
  id: string;
  name: string;
  type: NetworkType;
  projectId?: string;
  projectName?: string;
  environmentId?: string;
  environmentName?: string;
  serviceCount: number;
  services: NetworkServiceDto[];
  createdAt: string;
}

/* Request Types */

export interface GetNetworksParams {
  pageNumber?: number;
  pageSize?: number;
  type?: NetworkType;
}
