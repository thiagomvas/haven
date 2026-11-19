import type { TemplateSuggestionsDto } from '@/api/types';

import { parseEnvFile } from './envFile';

/**
 * Pure helpers for `${{ namespace.key | filter }}` references: working out what to suggest at
 * the cursor and what to flag as invalid. Kept free of CodeMirror so they are easy to test;
 * see templateEditorExtensions.ts for the editor wiring.
 */

// Mirrors the backend TemplateExpressionResolver pattern.
const REFERENCE = /\$\{\{\s*([a-zA-Z0-9_]+)\.([^\s|}]+)\s*(?:\|\s*(\w+)\s*)?\}\}/g;
const ANY_REFERENCE = /\$\{\{([^}]*)\}\}/g;

export interface TemplateCompletionOption {
  label: string;
  /** Text replacing the range from `CompletionContext.from` to the cursor. */
  insert: string;
  detail?: string;
  type: 'namespace' | 'variable' | 'function';
  /** Whether accepting the option should immediately offer the next level of suggestions. */
  continues: boolean;
}

export interface TemplateCompletionContext {
  /** Offset within the line prefix where the replaced range starts. */
  from: number;
  options: TemplateCompletionOption[];
}

/**
 * Works out the suggestions for the text on the current line up to the cursor, or `null` when
 * the cursor is not inside an unfinished `${{ ... ` reference.
 */
export function getTemplateCompletions(
  linePrefix: string,
  suggestions: TemplateSuggestionsDto
): TemplateCompletionContext | null {
  const filterMatch = /\$\{\{\s*[^\s|}]+\s*\|\s*(\w*)$/.exec(linePrefix);
  if (filterMatch) {
    const typed = filterMatch[1];
    return {
      from: linePrefix.length - typed.length,
      options: suggestions.filters.map(f => ({
        label: f,
        insert: f,
        type: 'function',
        continues: false,
      })),
    };
  }

  const refMatch = /\$\{\{(\s*)([^\s|}]*)$/.exec(linePrefix);
  if (!refMatch) return null;

  const [, space, typed] = refMatch;
  const typedStart = linePrefix.length - typed.length;
  const dot = typed.indexOf('.');

  if (dot === -1) {
    const lead = space.length === 0 ? ' ' : '';
    return {
      from: typedStart,
      options: suggestions.namespaces.map(ns => ({
        label: ns,
        insert: `${lead}${ns}.`,
        type: 'namespace',
        continues: true,
      })),
    };
  }

  const namespace = typed.slice(0, dot);
  return {
    from: typedStart + dot + 1,
    options: suggestions.variables
      .filter(v => v.namespace === namespace)
      .map(v => ({
        label: v.key,
        insert: v.key,
        detail: v.description ?? undefined,
        type: 'variable',
        continues: false,
      })),
  };
}

export interface TemplateDiagnostic {
  from: number;
  to: number;
  severity: 'error' | 'warning';
  message: string;
}

/**
 * Flags references that the backend would leave unresolved: unknown namespaces, unknown filters
 * and (where the full key set is known) unknown keys. `documentContent` is the .env text being
 * edited, so variables defined in it count as known even before they are saved.
 */
export function lintTemplateReferences(
  documentContent: string,
  suggestions: TemplateSuggestionsDto
): TemplateDiagnostic[] {
  const diagnostics: TemplateDiagnostic[] = [];
  const documentKeys = new Set(Object.keys(parseEnvFile(documentContent)));
  const keysByNamespace = new Map<string, Set<string>>();
  for (const v of suggestions.variables) {
    if (!keysByNamespace.has(v.namespace)) keysByNamespace.set(v.namespace, new Set());
    keysByNamespace.get(v.namespace)!.add(v.key);
  }

  const aliases = new Set(Array.from(keysByNamespace.get('services') ?? [], k => k.split('.')[0]));

  for (const wellFormed of documentContent.matchAll(ANY_REFERENCE)) {
    const index = wellFormed.index;
    if (isCommentLine(documentContent, index)) continue;

    const strict = new RegExp(REFERENCE.source).exec(wellFormed[0]);
    if (!strict) {
      diagnostics.push({
        from: index,
        to: index + wellFormed[0].length,
        severity: 'warning',
        message: 'Malformed reference; expected ${{ namespace.key }}.',
      });
      continue;
    }

    const [, namespace, key, filter] = strict;
    const refStart = index + wellFormed[0].indexOf(`${namespace}.${key}`);
    const refEnd = refStart + namespace.length + 1 + key.length;

    if (!suggestions.namespaces.includes(namespace)) {
      diagnostics.push({
        from: refStart,
        to: refEnd,
        severity: 'error',
        message: `Unknown namespace '${namespace}'. Available: ${suggestions.namespaces.join(', ')}.`,
      });
      continue;
    }

    if (filter && !suggestions.filters.includes(filter)) {
      const filterStart = index + wellFormed[0].lastIndexOf(filter);
      diagnostics.push({
        from: filterStart,
        to: filterStart + filter.length,
        severity: 'warning',
        message: `Unknown filter '${filter}'. Available: ${suggestions.filters.join(', ')}.`,
      });
    }

    const known = keysByNamespace.get(namespace) ?? new Set<string>();
    const isExhaustive = suggestions.isExhaustive || namespace === 'runtime';
    if (!isExhaustive || known.has(key) || (namespace === 'env' && documentKeys.has(key))) continue;

    diagnostics.push({
      from: refStart,
      to: refEnd,
      severity: 'warning',
      message: unknownKeyMessage(namespace, key, aliases),
    });
  }

  return diagnostics;
}

function unknownKeyMessage(namespace: string, key: string, aliases: Set<string>): string {
  if (namespace !== 'services')
    return `Unknown ${namespace} key '${key}'; it will not be resolved.`;

  const [alias, scope, ...rest] = key.split('.');
  if (!aliases.has(alias))
    return `No service with alias '${alias}' in this environment; it will not be resolved.`;
  if (!scope || rest.length === 0)
    return `Malformed service reference; expected services.<alias>.<scope>.<key>.`;
  return `Service '${alias}' has no ${scope} key '${rest.join('.')}'; it will not be resolved.`;
}

function isCommentLine(content: string, index: number): boolean {
  const lineStart = content.lastIndexOf('\n', index - 1) + 1;
  return content.slice(lineStart, index).trimStart().startsWith('#');
}
