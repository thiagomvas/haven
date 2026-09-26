import { apiClient } from './client';
import { CreateServiceFromTemplateInput } from './types/serviceTemplate.types';
import { ServiceTemplateDto } from './types/serviceTemplate.types';
import { ServiceTemplateSummaryDto } from './types/serviceTemplate.types';

export const serviceTemplatesApi = {
  getAll: () => apiClient.get<ServiceTemplateSummaryDto[]>('/service-templates'),

  getById: (id: string) => apiClient.get<ServiceTemplateDto>(`/service-templates/${id}`),

  createService: (
    projectId: string,
    environmentId: string,
    templateId: string,
    body: CreateServiceFromTemplateInput
  ) =>
    apiClient.post<string>(
      `/projects/${projectId}/environments/${environmentId}/services/from-template/${templateId}`,
      body
    ),
};
