import { useEffect, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Save } from 'lucide-react';
import { Button } from './Button';
import styles from './CodeEditor.module.css';

interface CodeEditorProps {
  onLoad: () => Promise<string>;
  onSave: (content: string) => Promise<void>;
  placeholder?: string;
  saveLabel?: string;
  savedMessage?: string;
  loadingMessage?: string;
  errorMessage?: string;
}

export function CodeEditor({
  onLoad,
  onSave,
  placeholder,
  saveLabel,
  savedMessage,
  loadingMessage,
  errorMessage,
}: CodeEditorProps) {
  const { t } = useTranslation('common');
  const [content, setContent] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);
  const [isDirty, setIsDirty] = useState(false);
  const [showSaved, setShowSaved] = useState(false);
  const savedTimerRef = useRef<ReturnType<typeof setTimeout> | undefined>(undefined);

  useEffect(() => {
    let active = true;
    (async () => {
      try {
        setLoading(true);
        setError(null);
        const value = await onLoad();
        if (active) {
          setContent(value ?? '');
          setIsDirty(false);
        }
      } catch (err) {
        if (active) setError(err instanceof Error ? err.message : (errorMessage ?? t('labels.error')));
      } finally {
        if (active) setLoading(false);
      }
    })();
    return () => {
      active = false;
    };
    // onLoad identity changing would cause infinite loops; callers should memoize if needed
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const handleSave = async () => {
    try {
      setIsSaving(true);
      setError(null);
      await onSave(content);
      setIsDirty(false);
      setShowSaved(true);
      clearTimeout(savedTimerRef.current);
      savedTimerRef.current = setTimeout(() => setShowSaved(false), 3000);
    } catch (err) {
      setError(err instanceof Error ? err.message : (errorMessage ?? t('labels.error')));
    } finally {
      setIsSaving(false);
    }
  };

  if (loading) {
    return (
      <div className={styles.container}>
        <p className={styles.loadingText}>{loadingMessage ?? t('labels.loading')}</p>
      </div>
    );
  }

  return (
    <div className={styles.container}>
      {error && <div className={styles.error}>{error}</div>}
      {showSaved && <div className={styles.success}>{savedMessage ?? t('labels.saved')}</div>}

      <textarea
        className={styles.editor}
        value={content}
        onChange={e => {
          setContent(e.target.value);
          setIsDirty(true);
        }}
        placeholder={placeholder}
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
          {saveLabel ?? t('actions.save')}
        </Button>
      </div>
    </div>
  );
}
