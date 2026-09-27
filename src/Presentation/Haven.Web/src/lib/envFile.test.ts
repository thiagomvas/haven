import { describe, expect, it } from 'vitest';

import { mergeEnvFiles, parseEnvFile, serializeEnvFile } from './envFile';

describe('parseEnvFile', () => {
  it('parses KEY=VALUE lines into a record', () => {
    expect(parseEnvFile('FOO=bar\nBAZ=qux')).toEqual({ FOO: 'bar', BAZ: 'qux' });
  });

  it('ignores blank lines and comments', () => {
    expect(parseEnvFile('# comment\n\nFOO=bar\n')).toEqual({ FOO: 'bar' });
  });

  it('ignores lines with no "="', () => {
    expect(parseEnvFile('FOO=bar\nnotanenvline')).toEqual({ FOO: 'bar' });
  });
});

describe('serializeEnvFile', () => {
  it('renders a record back into KEY=VALUE lines', () => {
    expect(serializeEnvFile({ FOO: 'bar', BAZ: 'qux' })).toBe('FOO=bar\nBAZ=qux');
  });
});

describe('mergeEnvFiles', () => {
  it('keeps keys only present in the base', () => {
    expect(mergeEnvFiles('FOO=bar', '')).toBe('FOO=bar');
  });

  it('adds keys only present in the overrides', () => {
    expect(parseEnvFile(mergeEnvFiles('FOO=bar', 'BAZ=qux'))).toEqual({ FOO: 'bar', BAZ: 'qux' });
  });

  it('lets overrides win on key collisions, without duplicating the key', () => {
    const merged = mergeEnvFiles('FOO=bar', 'FOO=overridden');

    expect(parseEnvFile(merged)).toEqual({ FOO: 'overridden' });
    expect(merged.match(/FOO=/g)).toHaveLength(1);
  });
});
