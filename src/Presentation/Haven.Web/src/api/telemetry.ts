import { apiClient } from './client';

export type OtlpProtocol = 'Grpc' | 'HttpProtobuf';

export interface TelemetryOptions {
  enabled: boolean;
  otlpEndpoint: string;
  serviceName: string;
  protocol: OtlpProtocol;
}

export interface UpdateTelemetryInput {
  options: TelemetryOptions;
}

export const telemetryApi = {
  get: () => apiClient.get<TelemetryOptions>('/configuration/telemetry'),
  update: (options: TelemetryOptions) =>
    apiClient.put<TelemetryOptions>('/configuration/telemetry', { options }),
};
