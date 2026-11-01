import { Download } from 'lucide-react';
import { useCallback, useState } from 'react';
import { useTranslation } from 'react-i18next';

import styles from '@/styles/components/projects/EnvironmentVariablesEditor.module.css';

import { environmentsApi } from '../../api/environments';
import { useTemplateSuggestions } from '../../hooks/useTemplateSuggestions';
import { ExportEnvironmentVariablesModal } from '../environmentVariables/ExportEnvironmentVariablesModal';
import { Button } from '../ui/Button';
import { CodeEditor } from '../ui/CodeEditor';

interface EnvironmentVariablesEditorProps {
  projectId: string;
  environmentId: string;
  environmentName: string;
}

export function EnvironmentVariablesEditor({
  projectId,
  environmentId,
  environmentName,
}: EnvironmentVariablesEditorProps) {
  const { t } = useTranslation('environments');
  const { t: tCommon } = useTranslation('common');
  const [isExportOpen, setIsExportOpen] = useState(false);
  const { data: templateSuggestions } = useTemplateSuggestions({ projectId, environmentId });

  const handleLoad = useCallback(
    () => environmentsApi.getEnvironmentVariables(projectId, environmentId).then(v => v ?? ''),
    [projectId, environmentId]
  );

  const handleSave = useCallback(
    (content: string) =>
      environmentsApi.setEnvironmentVariables(projectId, environmentId, content).then(() => {}),
    [projectId, environmentId]
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
        templateSuggestions={templateSuggestions}
        savedMessage={t('variablesSaved')}
        loadingMessage={t('loading')}
        errorMessage={t('error')}
      />

      <ExportEnvironmentVariablesModal
        isOpen={isExportOpen}
        onClose={() => setIsExportOpen(false)}
        parentId={environmentId}
        parentType="Environment"
        name={environmentName}
      />
    </div>
  );
}
