export type NetworkType = 'ProjectEnvironment' | 'Shared' | 'External';
export interface NetworkDto {
  name: string;
  type: NetworkType;
  metadata?: Record<string, unknown>;
}
