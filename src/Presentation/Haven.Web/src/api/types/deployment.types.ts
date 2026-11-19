export type DeploymentStatus =
  | 'Queued'
  | 'Building'
  | 'Starting'
  | 'Succeeded'
  | 'Failed'
  | 'Cancelled';
export type DeploymentIdentifierType = 'Commit' | 'ImageDigest' | 'ImageId';
export interface DeploymentDto {
  id: string;
  serviceId: string;
  startedAt: string;
  finishedAt?: string;
  status: DeploymentStatus;
  triggeredBy?: string;
  identifierType?: DeploymentIdentifierType;
  identifier?: string;
  commitMessage?: string;
}
