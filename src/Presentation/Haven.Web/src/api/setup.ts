import { apiClient } from './client'
import { AuthResponse } from './auth'

export const SetupStage = {
  NotStarted: 'NotStarted',
  InstanceConfigured: 'InstanceConfigured',
  SuperUserCreated: 'SuperUserCreated',
  Completed: 'Completed',
} as const
export type SetupStage = (typeof SetupStage)[keyof typeof SetupStage]

export interface SetupStatusResponse {
  stage: SetupStage
}

export interface RegisterInput {
  name: string
  email: string
  password: string
}

export interface ConfigureInstanceInput {
  instanceName: string
  timezone: string
}

export interface ConfigureNetworkInput {
  host?: string
  port?: number
  enableTls: boolean
}

export const setupApi = {
  getStatus: () => apiClient.get<SetupStatusResponse>('/setup/status'),
  register: (input: RegisterInput) => apiClient.post<AuthResponse>('/setup/register', input),
  configureInstance: (input: ConfigureInstanceInput) =>
    apiClient.post<void>('/setup/instance', input),
  configureNetwork: (input: ConfigureNetworkInput) =>
    apiClient.post<void>('/setup/network', input),
}
