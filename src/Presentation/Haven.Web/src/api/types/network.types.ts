import { NetworkType } from "./network.types";

export type NetworkType = 'ProjectEnvironment' | 'Shared' | 'External';export interface NetworkDto {
  name: string;
  type: NetworkType;
  metadata?: Record<string, unknown>;
}

