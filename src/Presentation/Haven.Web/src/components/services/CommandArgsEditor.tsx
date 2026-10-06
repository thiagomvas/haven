import { ChevronDown, ChevronUp } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import { Stack } from '@/components/layout';
import { parseShellArgs } from '@/lib/shellArgs';
import styles from '@/styles/components/services/CommandArgsEditor.module.css';

import { Checkbox } from '../ui/Checkbox';
import { FormGroup, FormLabel } from '../ui/Form';

interface CommandArgsEditorProps {
  commandArgs: string[];
  onChange: (args: string[]) => void;
  disabled?: boolean;
  /** True when the args are joined into a `sh -c` script, so quotes must be preserved. */
  shellMode?: boolean;
}

export function CommandArgsEditor({
  commandArgs,
  onChange,
  disabled,
  shellMode = false,
}: CommandArgsEditorProps) {
  const { t } = useTranslation('services');
  const [pasteOpen, setPasteOpen] = useState(false);
  const [pasteText, setPasteText] = useState('');

  const [keepQuotesOverride, setKeepQuotesOverride] = useState<boolean | null>(null);
  const keepQuotes = keepQuotesOverride ?? shellMode;

  const parsedArgs = parseShellArgs(pasteText, keepQuotes);

  const importArgs = (mode: 'append' | 'replace') => {
    if (parsedArgs.length === 0) return;
    onChange(mode === 'append' ? [...commandArgs, ...parsedArgs] : parsedArgs);
    setPasteText('');
    setPasteOpen(false);
  };

  const updateArg = (idx: number, value: string) => {
    onChange(commandArgs.map((a, i) => (i === idx ? value : a)));
  };

  const removeArg = (idx: number) => {
    onChange(commandArgs.filter((_, i) => i !== idx));
  };

  const addArg = () => {
    onChange([...commandArgs, '']);
  };

  const moveArgUp = (idx: number) => {
    if (idx === 0) return;
    const updated = [...commandArgs];
    [updated[idx - 1], updated[idx]] = [updated[idx], updated[idx - 1]];
    onChange(updated);
  };

  const moveArgDown = (idx: number) => {
    if (idx === commandArgs.length - 1) return;
    const updated = [...commandArgs];
    [updated[idx], updated[idx + 1]] = [updated[idx + 1], updated[idx]];
    onChange(updated);
  };

  return (
    <FormGroup>
      <div className={styles.labelWithHelp}>
        <FormLabel htmlFor="commandArgs">{t('createPage.commandArgs')}</FormLabel>
        <span className={styles.helpText}>{t('createPage.commandArgsHelp')}</span>
      </div>
      <Stack gap="3" className={styles.argsContainer}>
        {commandArgs.length === 0 ? (
          <p className={styles.emptyState}>{t('createPage.noCommandArgs')}</p>
        ) : (
          commandArgs.map((arg, idx) => (
            <div key={idx} className={styles.argRow}>
              <input
                type="text"
                className={styles.argInput}
                placeholder={t('createPage.commandArgPlaceholder')}
                value={arg}
                onChange={e => updateArg(idx, e.target.value)}
                disabled={disabled}
              />
              <button
                type="button"
                className={styles.reorderButton}
                onClick={() => moveArgUp(idx)}
                disabled={disabled || idx === 0}
                aria-label="Move up"
              >
                <ChevronUp size={14} />
              </button>
              <button
                type="button"
                className={styles.reorderButton}
                onClick={() => moveArgDown(idx)}
                disabled={disabled || idx === commandArgs.length - 1}
                aria-label="Move down"
              >
                <ChevronDown size={14} />
              </button>
              <button
                type="button"
                className={styles.argRemove}
                onClick={() => removeArg(idx)}
                disabled={disabled}
              >
                ×
              </button>
            </div>
          ))
        )}
      </Stack>
      <div className={styles.actions}>
        <button type="button" className={styles.addArgButton} onClick={addArg} disabled={disabled}>
          {t('createPage.addCommandArg')}
        </button>
        <button
          type="button"
          className={styles.addArgButton}
          onClick={() => setPasteOpen(o => !o)}
          disabled={disabled}
        >
          {t('createPage.fromCommand')}
        </button>
      </div>
      {pasteOpen && (
        <div className={styles.pastePanel}>
          <textarea
            className={styles.pasteInput}
            placeholder={t('createPage.fromCommandPlaceholder')}
            value={pasteText}
            onChange={e => setPasteText(e.target.value)}
            disabled={disabled}
            rows={4}
            autoFocus
          />
          <Checkbox
            label={t('createPage.fromCommandKeepQuotes')}
            checked={keepQuotes}
            onChange={e => setKeepQuotesOverride(e.target.checked)}
            disabled={disabled}
          />
          <span className={styles.helpText}>
            {t('createPage.fromCommandPreview', { count: parsedArgs.length })}
          </span>
          <div className={styles.actions}>
            <button
              type="button"
              className={styles.addArgButton}
              onClick={() => importArgs('append')}
              disabled={disabled || parsedArgs.length === 0}
            >
              {t('createPage.fromCommandAppend')}
            </button>
            <button
              type="button"
              className={styles.addArgButton}
              onClick={() => importArgs('replace')}
              disabled={disabled || parsedArgs.length === 0}
            >
              {t('createPage.fromCommandReplace')}
            </button>
          </div>
        </div>
      )}
    </FormGroup>
  );
}
