import { Download, Save } from 'lucide-react';
import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';

import styles from '@/styles/components/projects/EnvironmentVariablesEditor.module.css';

import { environmentsApi } from '../../api/environments';
import { ExportEnvironmentVariablesModal } from '../environmentVariables/ExportEnvironmentVariablesModal';
import { Button } from '../ui/Button';

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
  const [envContent, setEnvContent] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);
  const [isDirty, setIsDirty] = useState(false);
  const [savedMessage, setSavedMessage] = useState(false);
  const [isExportOpen, setIsExportOpen] = useState(false);

  useEffect(() => {
    let active = true;
    (async () => {
      try {
        setLoading(true);
        setError(null);
        const content = await environmentsApi.getEnvironmentVariables(projectId, environmentId);
        if (active) {
          setEnvContent(content || '');
          setIsDirty(false);
        }
      } catch (err) {
        if (active) setError(err instanceof Error ? err.message : t('error'));
      } finally {
        if (active) setLoading(false);
      }
    })();
    return () => {
      active = false;
    };
  }, [projectId, environmentId, t]);

  const handleSave = async () => {
    try {
      setIsSaving(true);
      await environmentsApi.setEnvironmentVariables(projectId, environmentId, envContent);
      setIsDirty(false);
      setSavedMessage(true);
      setTimeout(() => setSavedMessage(false), 3000);
    } catch (err) {
      setError(err instanceof Error ? err.message : t('error'));
    } finally {
      setIsSaving(false);
    }
  };

  if (loading) {
    return (
      <div className={styles.container}>
        <p>{t('loading')}</p>
      </div>
    );
  }

  return (
    <div className={styles.container}>
      {error && <div className={styles.error}>{error}</div>}
      {savedMessage && <div className={styles.success}>{t('variablesSaved')}</div>}

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

      <textarea
        className={styles.editor}
        value={envContent}
        onChange={e => {
          setEnvContent(e.target.value);
          setIsDirty(true);
        }}
        placeholder={'.env file format\nKEY=value\nDATABASE_URL=postgresql://...'}
        spellCheck="false"
      />

      <div className={styles.footer}>
        <Button
          variant="primary"
          icon={<Save size={18} />}
          onClick={handleSave}
          disabled={!isDirty || isSaving}
          isLoading={isSaving}
        >
          {t('save') || 'Save'}
        </Button>
      </div>

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
