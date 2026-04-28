/* Enums */
export type ServiceType = 'DockerImage' | 'Compose' | 'Process'
export type ServiceStatus = 'Running' | 'Stopped' | 'Degraded' | 'Unknown'
export type ExposureMode = 'None' | 'Internal' | 'External'
export type RestartPolicy = 'No' | 'Always' | 'UnlessStopped' | 'OnFailure'
export type NetworkType = 'ProjectEnvironment' | 'Shared' | 'External'

/* Response Wrappers */
export interface ApiResponse<T> {
  success: boolean
  data?: T
  message?: string
}

export interface PagedResult<T> {
  items: T[]
  totalCount: number
  pageNumber: number
  pageSize: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}

/* DTOs */
export interface ProjectDto {
  id: string
  name: string
  description?: string
}

export interface EnvironmentDto {
  id: string
  projectId: string
  name: string
  description?: string
  networkName: string
}

export interface DockerConfig {
  image: string
  ports: string[]
  volumes: string[]
  environmentVariables: string[]
  restartPolicy: RestartPolicy
}

export interface ServiceSourceConfig {
  $type?: string
}

export interface ServiceDto {
  id: string
  environmentId: string
  name: string
  type: ServiceType
  exposureMode: ExposureMode
  status: ServiceStatus
  sourceConfig?: ServiceSourceConfig | DockerConfig
  createdAt: string
  updatedAt: string
}

export interface EventDto {
  id: string
  eventType: string
  message: string
  payload?: string
  triggeredAt: string
}

export interface NetworkDto {
  name: string
  type: NetworkType
  metadata?: Record<string, unknown>
}

/* Request Types */
export interface GetProjectsParams {
  pageNumber?: number
  pageSize?: number
}

export interface CreateProjectInput {
  name: string
  description?: string
}

export interface UpdateProjectInput {
  name?: string
  description?: string | null
}

export interface CreateEnvironmentInput {
  name: string
  description?: string
}

export interface UpdateEnvironmentInput {
  name?: string
  description?: string | null
}

export interface CreateServiceInput {
  name: string
  type: ServiceType
  exposureMode: ExposureMode
  dockerConfig?: DockerConfig
}

export interface DeployServiceInput {
  projectId: string
  environmentId: string
  serviceId: string
}

export interface GetEventsParams {
  pageNumber?: number
  pageSize?: number
  eventType?: string
  from?: string
  to?: string
  ascending?: boolean
}
