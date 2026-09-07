import { apiClient } from './client';
import {
  SslCertificateDto,
  UploadSslCertificateInput,
  UploadSslCertificateResult,
} from './types/sslCertificate.types';

export const sslCertificatesApi = {
  list: () => apiClient.get<SslCertificateDto[]>('/ssl-certificates'),

  upload: (body: UploadSslCertificateInput) =>
    apiClient.post<UploadSslCertificateResult>('/ssl-certificates', body),

  delete: (certificateId: string) => apiClient.delete<void>(`/ssl-certificates/${certificateId}`),
};
