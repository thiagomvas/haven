export interface SslCertificateDto {
  id: string;
  name: string;
  subjectCommonName?: string;
  notBefore: string;
  notAfter: string;
  isExpired: boolean;
  attachedDomainCount: number;
  createdAt: string;
  updatedAt: string;
}

export interface UploadSslCertificateInput {
  name: string;
  certificatePem: string;
  privateKeyPem: string;
}

export interface UploadSslCertificateResult {
  certificateId: string;
  notAfter: string;
  warnings: string[];
}
