import { useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { servicesApi } from '../../api/services';
import { CodeEditor } from '../ui/CodeEditor';

interface ServiceManifestEditorProps {
  projectId: string;
  environmentId: string;
  serviceId: string;
  onApplied?: () => void;
}

export function ServiceManifestEditor({
  projectId,
  environmentId,
  serviceId,
  onApplied,
}: ServiceManifestEditorProps) {
  const { t } = useTranslation('services');

  const handleLoad = useCallback(
    () => servicesApi.getManifest(projectId, environmentId, serviceId).then(v => v ?? ''),
    [projectId, environmentId, serviceId]
  );

  const handleSave = useCallback(
    async (content: string) => {
      await servicesApi.applyManifest(projectId, environmentId, serviceId, content);
      onApplied?.();
    },
    [projectId, environmentId, serviceId, onApplied]
  );

  return (
    <CodeEditor
      onLoad={handleLoad}
      onSave={handleSave}
      placeholder="YAML manifest"
      saveLabel={t('manifest.apply')}
      savedMessage={t('manifest.saved')}
      loadingMessage={t('manifest.loading')}
    />
  );
}
