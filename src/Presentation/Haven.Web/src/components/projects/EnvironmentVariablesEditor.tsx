import { Download } from 'lucide-react';
import { useCallback, useState } from 'react';
import { useTranslation } from 'react-i18next';

import styles from '@/styles/components/projects/EnvironmentVariablesEditor.module.css';

import { projectsApi } from '../../api/projects';
import { useTemplateSuggestions } from '../../hooks/useTemplateSuggestions';
import { ExportEnvironmentVariablesModal } from '../environmentVariables/ExportEnvironmentVariablesModal';
import { Button } from '../ui/Button';
import { CodeEditor } from '../ui/CodeEditor';

interface EnvironmentVariablesEditorProps {
  projectId: string;
  projectName: string;
}

export function EnvironmentVariablesEditor({
  projectId,
  projectName,
}: EnvironmentVariablesEditorProps) {
  const { t } = useTranslation('projects');
  const { t: tCommon } = useTranslation('common');
  const [isExportOpen, setIsExportOpen] = useState(false);
  const { data: templateSuggestions } = useTemplateSuggestions({ projectId });

  const handleLoad = useCallback(
    () => projectsApi.getEnvironmentVariables(projectId).then(v => v ?? ''),
    [projectId]
  );

  const handleSave = useCallback(
    (content: string) => projectsApi.setEnvironmentVariables(projectId, content).then(() => {}),
    [projectId]
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
        parentId={projectId}
        parentType="Project"
        name={projectName}
      />
    </div>
  );
}
