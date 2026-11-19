import { describe, expect, it } from 'vitest';

import type { TemplateSuggestionsDto } from '@/api/types';

import { getTemplateCompletions, lintTemplateReferences } from './templateReferences';

const suggestions: TemplateSuggestionsDto = {
  namespaces: ['env', 'secrets', 'runtime', 'outputs', 'services'],
  filters: ['urlencode'],
  isExhaustive: true,
  variables: [
    { namespace: 'env', key: 'PORT' },
    { namespace: 'secrets', key: 'API_KEY' },
    { namespace: 'runtime', key: 'hostname', description: 'Container hostname' },
    { namespace: 'services', key: 'db.outputs.connection_string' },
  ],
};

describe('getTemplateCompletions', () => {
  it('returns null outside of a reference', () => {
    expect(getTemplateCompletions('PORT=80', suggestions)).toBeNull();
    expect(getTemplateCompletions('A=${{ env.PORT }} ', suggestions)).toBeNull();
  });

  it('suggests namespaces right after the opening braces', () => {
    const result = getTemplateCompletions('A=${{ ', suggestions)!;
    expect(result.from).toBe('A=${{ '.length);
    expect(result.options.map(o => o.label)).toEqual(suggestions.namespaces);
    expect(result.options[0]).toMatchObject({ insert: 'env.', continues: true });
  });

  it('prefixes a space when none was typed', () => {
    const result = getTemplateCompletions('A=${{', suggestions)!;
    expect(result.options[0].insert).toBe(' env.');
  });

  it('suggests keys of the typed namespace only', () => {
    const prefix = 'A=${{ secrets.';
    const result = getTemplateCompletions(prefix, suggestions)!;
    expect(result.from).toBe(prefix.length);
    expect(result.options.map(o => o.label)).toEqual(['API_KEY']);
  });

  it('keeps dotted service keys whole', () => {
    const result = getTemplateCompletions('A=${{ services.db.o', suggestions)!;
    expect(result.options.map(o => o.label)).toEqual(['db.outputs.connection_string']);
  });

  it('suggests filters after a pipe', () => {
    const result = getTemplateCompletions('A=${{ env.PORT | ur', suggestions)!;
    expect(result.options.map(o => o.label)).toEqual(['urlencode']);
    expect(result.from).toBe('A=${{ env.PORT | '.length);
  });

  it('returns no options for an unknown namespace', () => {
    expect(getTemplateCompletions('A=${{ nope.', suggestions)!.options).toEqual([]);
  });
});

describe('lintTemplateReferences', () => {
  it('accepts valid references', () => {
    const content =
      'A=${{ env.PORT }}\nB=${{ runtime.hostname | urlencode }}\nC=${{ services.db.outputs.connection_string }}';
    expect(lintTemplateReferences(content, suggestions)).toEqual([]);
  });

  it('flags unknown namespaces as errors over the namespace.key range', () => {
    const content = 'A=${{ inputs.x }}';
    const [d] = lintTemplateReferences(content, suggestions);
    expect(d.severity).toBe('error');
    expect(content.slice(d.from, d.to)).toBe('inputs.x');
  });

  it('flags unknown keys as warnings when the key set is exhaustive', () => {
    const [d] = lintTemplateReferences('A=${{ secrets.NOPE }}', suggestions);
    expect(d.severity).toBe('warning');
    expect(d.message).toContain("'NOPE'");
  });

  it('counts variables defined in the edited document as known env keys', () => {
    expect(lintTemplateReferences('LOCAL=1\nA=${{ env.LOCAL }}', suggestions)).toEqual([]);
  });

  it('does not flag unknown keys for non-exhaustive scopes, except runtime', () => {
    const partial = { ...suggestions, isExhaustive: false };
    expect(lintTemplateReferences('A=${{ env.LATER }}', partial)).toEqual([]);
    expect(lintTemplateReferences('A=${{ runtime.nope }}', partial)).toHaveLength(1);
  });

  it('flags unknown filters', () => {
    const content = 'A=${{ env.PORT | shout }}';
    const [d] = lintTemplateReferences(content, suggestions);
    expect(content.slice(d.from, d.to)).toBe('shout');
  });

  it('explains unknown service aliases and keys', () => {
    const [alias] = lintTemplateReferences('A=${{ services.cache.outputs.url }}', suggestions);
    expect(alias.message).toContain("alias 'cache'");
    const [key] = lintTemplateReferences('A=${{ services.db.outputs.nope }}', suggestions);
    expect(key.message).toContain("Service 'db' has no outputs key 'nope'");
  });

  it('flags malformed references', () => {
    const [d] = lintTemplateReferences('A=${{ PORT }}', suggestions);
    expect(d.message).toContain('Malformed');
  });

  it('ignores commented lines', () => {
    expect(lintTemplateReferences('# ${{ inputs.x }}', suggestions)).toEqual([]);
  });
});
