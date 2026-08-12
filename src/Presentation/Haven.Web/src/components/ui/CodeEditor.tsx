import { yaml } from '@codemirror/lang-yaml';
import { HighlightStyle, syntaxHighlighting } from '@codemirror/language';
import { EditorState } from '@codemirror/state';
import { tags } from '@lezer/highlight';
import { basicSetup, EditorView } from 'codemirror';
import { Save } from 'lucide-react';
import { useEffect, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';

import styles from '@/styles/components/ui/CodeEditor.module.css';

import { Button } from './Button';

const havenTheme = EditorView.theme({
  '&': {
    fontSize: '14px',
    fontFamily: "'Monaco', 'Menlo', 'Ubuntu Mono', monospace",
    backgroundColor: 'var(--color-surface)',
    color: 'var(--color-text-primary)',
  },
  '&.cm-focused': {
    outline: 'none',
  },
  '.cm-scroller': {
    fontFamily: "'Monaco', 'Menlo', 'Ubuntu Mono', monospace",
    lineHeight: '1.6',
    overflow: 'auto',
  },
  '.cm-content': {
    caretColor: 'var(--color-text-primary)',
    padding: '12px 0',
  },
  '.cm-gutters': {
    backgroundColor: 'var(--color-surface-2)',
    color: 'var(--color-text-muted)',
    border: 'none',
    borderRight: '1px solid var(--color-border)',
    userSelect: 'none',
  },
  '.cm-lineNumbers .cm-gutterElement': {
    padding: '0 8px 0 12px',
    minWidth: '40px',
  },
  '.cm-activeLineGutter': {
    backgroundColor: 'var(--color-surface-hover)',
  },
  '.cm-activeLine': {
    backgroundColor: 'transparent !important',
  },
  '.cm-selectionBackground': {
    backgroundColor: 'rgba(var(--color-primary-rgb), 0.35) !important',
  },
  '&.cm-focused .cm-selectionBackground': {
    backgroundColor: 'rgba(var(--color-primary-rgb), 0.35) !important',
  },
  '.cm-cursor': {
    borderLeftColor: 'var(--color-text-primary)',
  },
  '.cm-foldGutter': {
    color: 'var(--color-text-muted)',
  },
});

const havenHighlightStyle = HighlightStyle.define([
  { tag: tags.propertyName, color: 'var(--color-teal-600)' },
  { tag: tags.string, color: 'var(--color-amber-600)' },
  { tag: tags.comment, color: 'var(--color-text-muted)', fontStyle: 'italic' },
  { tag: tags.number, color: 'var(--color-deploying)' },
  { tag: tags.bool, color: 'var(--color-danger)' },
  { tag: tags.null, color: 'var(--color-text-muted)' },
  { tag: tags.keyword, color: 'var(--color-primary)' },
  { tag: tags.punctuation, color: 'var(--color-text-secondary)' },
  { tag: tags.meta, color: 'var(--color-text-muted)' },
  { tag: tags.emphasis, fontStyle: 'italic' },
  { tag: tags.strong, fontWeight: 'bold' },
]);

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
  saveLabel,
  savedMessage,
  loadingMessage,
  errorMessage,
}: CodeEditorProps) {
  const { t } = useTranslation('common');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);
  const [isDirty, setIsDirty] = useState(false);
  const [showSaved, setShowSaved] = useState(false);
  const [initialContent, setInitialContent] = useState<string | null>(null);
  const savedTimerRef = useRef<ReturnType<typeof setTimeout> | undefined>(undefined);
  const editorContainerRef = useRef<HTMLDivElement>(null);
  const viewRef = useRef<EditorView | null>(null);

  useEffect(() => {
    let active = true;
    (async () => {
      try {
        setLoading(true);
        setError(null);
        const value = await onLoad();
        if (active) setInitialContent(value ?? '');
      } catch (err) {
        if (active)
          setError(err instanceof Error ? err.message : (errorMessage ?? t('labels.error')));
      } finally {
        if (active) setLoading(false);
      }
    })();
    return () => {
      active = false;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  useEffect(() => {
    if (initialContent === null || !editorContainerRef.current) return;

    const view = new EditorView({
      state: EditorState.create({
        doc: initialContent,
        extensions: [
          basicSetup,
          yaml(),
          havenTheme,
          syntaxHighlighting(havenHighlightStyle),
          EditorView.updateListener.of(update => {
            if (update.docChanged) setIsDirty(true);
          }),
        ],
      }),
      parent: editorContainerRef.current,
    });
    viewRef.current = view;

    return () => {
      view.destroy();
      viewRef.current = null;
    };
  }, [initialContent]);

  const handleSave = async () => {
    if (!viewRef.current) return;
    try {
      setIsSaving(true);
      setError(null);
      await onSave(viewRef.current.state.doc.toString());
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

      <div ref={editorContainerRef} className={styles.editorWrapper} />

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
