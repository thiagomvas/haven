export type DeploymentStatus = 'InProgress' | 'Succeeded' | 'Failed' | 'Cancelled';
export interface DeploymentDto {
  id: string;
  serviceId: string;
  startedAt: string;
  finishedAt?: string;
  status: DeploymentStatus;
  triggeredBy?: string;
}
