import { useQuery } from '@tanstack/react-query';

import { serviceTemplatesApi } from '@/api/serviceTemplates';

const SERVICE_TEMPLATES_KEY = 'service-templates';

export function useServiceTemplates() {
  return useQuery({
    queryKey: [SERVICE_TEMPLATES_KEY],
    queryFn: () => serviceTemplatesApi.getAll(),
  });
}

export function useServiceTemplate(id: string | undefined) {
  return useQuery({
    queryKey: [SERVICE_TEMPLATES_KEY, id],
    queryFn: () => serviceTemplatesApi.getById(id!),
    enabled: !!id,
  });
}
