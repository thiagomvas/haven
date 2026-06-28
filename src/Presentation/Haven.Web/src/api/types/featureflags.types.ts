/* Feature Flags */

import { FeatureFlagType, FeatureFlagValueType } from './featureflags.types';

export type FeatureFlagType = 'EnvironmentVariable';
export type FeatureFlagValueType = 'String' | 'Bool' | 'Number';
export interface FeatureFlagDto {
  id: string;
  serviceId: string;
  name: string;
  type: FeatureFlagType;
  key?: string;
  description?: string;
  value: string;
  valueType: FeatureFlagValueType;
}
export interface CreateFeatureFlagInput {
  name: string;
  type: FeatureFlagType;
  key?: string;
  description?: string;
  value: string;
  valueType: FeatureFlagValueType;
}
export interface UpdateFeatureFlagInput {
  name?: string;
  type?: FeatureFlagType;
  key?: string;
  description?: string;
  value?: string;
  valueType?: FeatureFlagValueType;
}
