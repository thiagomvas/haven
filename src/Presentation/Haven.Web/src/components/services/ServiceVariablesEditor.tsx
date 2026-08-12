import { Download } from 'lucide-react';
import { useCallback, useState } from 'react';
import { useTranslation } from 'react-i18next';

import styles from '@/styles/components/services/ServiceVariablesEditor.module.css';

import { servicesApi } from '../../api/services';
import { ExportEnvironmentVariablesModal } from '../environmentVariables/ExportEnvironmentVariablesModal';
import { Button } from '../ui/Button';
import { CodeEditor } from '../ui/CodeEditor';

interface ServiceVariablesEditorProps {
  projectId: string;
  environmentId: string;
  serviceId: string;
  serviceName: string;
}

export function ServiceVariablesEditor({
  projectId,
  environmentId,
  serviceId,
  serviceName,
}: ServiceVariablesEditorProps) {
  const { t } = useTranslation('services');
  const { t: tCommon } = useTranslation('common');
  const [isExportOpen, setIsExportOpen] = useState(false);

  const handleLoad = useCallback(
    () =>
      servicesApi.getEnvironmentVariables(projectId, environmentId, serviceId).then(v => v ?? ''),
    [projectId, environmentId, serviceId]
  );

  const handleSave = useCallback(
    (content: string) =>
      servicesApi
        .setEnvironmentVariables(projectId, environmentId, serviceId, content)
        .then(() => {}),
    [projectId, environmentId, serviceId]
  );

  return (
    <div className={styles.container}>
      <div className={styles.toolbar}>
        <Button
          variant="outline"
          size="sm"
          icon={<Download size={16} />}
          onClick={() => setIsExportOpen(true)}
        >
          {tCommon('actions.export')}
        </Button>
      </div>

      <CodeEditor
        onLoad={handleLoad}
        onSave={handleSave}
        placeholder={'.env file format\nKEY=value\nDATABASE_URL=postgresql://...'}
        savedMessage={t('variablesSaved')}
        loadingMessage={t('loading')}
        errorMessage={t('error')}
      />

      <ExportEnvironmentVariablesModal
        isOpen={isExportOpen}
        onClose={() => setIsExportOpen(false)}
        parentId={serviceId}
        parentType="Service"
        name={serviceName}
      />
    </div>
  );
}
