import { useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { servicesApi } from '../../api/services';
import { CodeEditor } from '../ui/CodeEditor';

interface ServiceVariablesEditorProps {
  projectId: string;
  environmentId: string;
  serviceId: string;
}

export function ServiceVariablesEditor({
  projectId,
  environmentId,
  serviceId,
}: ServiceVariablesEditorProps) {
  const { t } = useTranslation('services');

  const handleLoad = useCallback(
    () => servicesApi.getEnvironmentVariables(projectId, environmentId, serviceId).then(v => v ?? ''),
    [projectId, environmentId, serviceId]
  );

  const handleSave = useCallback(
    (content: string) =>
      servicesApi.setEnvironmentVariables(projectId, environmentId, serviceId, content).then(() => {}),
    [projectId, environmentId, serviceId]
  );

  return (
    <CodeEditor
      onLoad={handleLoad}
      onSave={handleSave}
      placeholder={'.env file format\nKEY=value\nDATABASE_URL=postgresql://...'}
      savedMessage={t('variablesSaved')}
      loadingMessage={t('loading')}
      errorMessage={t('error')}
    />
  );
}
