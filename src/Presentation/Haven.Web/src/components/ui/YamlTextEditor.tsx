import { yaml } from '@codemirror/lang-yaml';
import { syntaxHighlighting } from '@codemirror/language';
import { EditorState } from '@codemirror/state';
import { placeholder as placeholderExtension } from '@codemirror/view';
import { basicSetup, EditorView } from 'codemirror';
import { useEffect, useRef } from 'react';

import styles from '@/styles/components/ui/YamlTextEditor.module.css';

import { havenHighlightStyle, havenTheme } from './CodeEditor';

interface YamlTextEditorProps {
  id?: string;
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  disabled?: boolean;
  minLines?: number;
}

export function YamlTextEditor({
  id,
  value,
  onChange,
  placeholder,
  disabled,
  minLines = 16,
}: YamlTextEditorProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const viewRef = useRef<EditorView | null>(null);
  const onChangeRef = useRef(onChange);

  useEffect(() => {
    onChangeRef.current = onChange;
  });

  useEffect(() => {
    if (!containerRef.current) return;

    const view = new EditorView({
      state: EditorState.create({
        doc: value,
        extensions: [
          basicSetup,
          yaml(),
          havenTheme,
          syntaxHighlighting(havenHighlightStyle),
          EditorView.contentAttributes.of({ ...(id ? { id } : {}), spellcheck: 'false' }),
          EditorView.theme({
            '.cm-content, .cm-gutters': { minHeight: `${minLines * 1.6 * 14 + 24}px` },
            '.cm-scroller': { overflowY: 'hidden' },
          }),
          EditorView.editable.of(!disabled),
          placeholder ? placeholderExtension(placeholder) : [],
          EditorView.updateListener.of(update => {
            if (update.docChanged) onChangeRef.current(update.state.doc.toString());
          }),
        ],
      }),
      parent: containerRef.current,
    });
    viewRef.current = view;

    return () => {
      view.destroy();
      viewRef.current = null;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [disabled, placeholder, id, minLines]);

  // Sync external value changes (e.g. a reset) into the editor.
  useEffect(() => {
    const view = viewRef.current;
    if (view && view.state.doc.toString() !== value) {
      view.dispatch({ changes: { from: 0, to: view.state.doc.length, insert: value } });
    }
  }, [value]);

  return <div ref={containerRef} className={styles.wrapper} />;
}
