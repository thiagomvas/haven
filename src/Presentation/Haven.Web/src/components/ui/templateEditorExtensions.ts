import {
  autocompletion,
  type Completion,
  type CompletionContext,
  type CompletionResult,
  startCompletion,
} from '@codemirror/autocomplete';
import { type Diagnostic, linter } from '@codemirror/lint';
import type { Extension } from '@codemirror/state';
import type { EditorView } from '@codemirror/view';

import type { TemplateSuggestionsDto } from '@/api/types';

import { getTemplateCompletions, lintTemplateReferences } from '../../lib/templateReferences';

/**
 * Autocompletion and inline linting for `${{ namespace.key }}` references. Suggestions are read
 * lazily through `getSuggestions`, so they can arrive after the editor was created.
 */
export function templateSupport(getSuggestions: () => TemplateSuggestionsDto | null): Extension[] {
  const complete = (context: CompletionContext): CompletionResult | null => {
    const suggestions = getSuggestions();
    if (!suggestions) return null;

    const line = context.state.doc.lineAt(context.pos);
    const prefix = line.text.slice(0, context.pos - line.from);
    const result = getTemplateCompletions(prefix, suggestions);
    if (!result || (result.options.length === 0 && !context.explicit)) return null;

    return {
      from: line.from + result.from,
      validFor: /^[^\s|}]*$/,
      options: result.options.map(
        (o): Completion => ({
          label: o.label,
          detail: o.detail,
          type: o.type,
          apply: (view, _completion, from, to) =>
            applyOption(view, from, to, o.insert, o.continues),
        })
      ),
    };
  };

  const lint = (view: EditorView): Diagnostic[] => {
    const suggestions = getSuggestions();
    if (!suggestions) return [];
    return lintTemplateReferences(view.state.doc.toString(), suggestions);
  };

  return [autocompletion({ override: [complete] }), linter(lint)];
}

function applyOption(
  view: EditorView,
  from: number,
  to: number,
  insert: string,
  continues: boolean
) {
  let text = insert;
  if (!continues) {
    // Close the reference unless it already is (closeBrackets usually adds the `}}` for us).
    const after = view.state.doc.sliceString(to, Math.min(to + 4, view.state.doc.length));
    if (!/^\s*(\||\}\})/.test(after)) text += ' }}';
  }

  view.dispatch({
    changes: { from, to, insert: text },
    selection: { anchor: from + insert.length },
    userEvent: 'input.complete',
  });
  if (continues) startCompletion(view);
}
