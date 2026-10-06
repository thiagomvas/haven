import { describe, expect, it } from 'vitest';

import { parseShellArgs } from './shellArgs';

describe('parseShellArgs', () => {
  it('splits on whitespace', () => {
    expect(parseShellArgs('  psql  -U  admin ')).toEqual(['psql', '-U', 'admin']);
  });

  it('keeps quoted strings together', () => {
    expect(parseShellArgs(`-c "SELECT * FROM t" -c 'a b'`)).toEqual([
      '-c',
      'SELECT * FROM t',
      '-c',
      'a b',
    ]);
  });

  it('handles escapes inside and outside quotes', () => {
    expect(parseShellArgs(String.raw`"say \"hi\"" a\ b 'c\d'`)).toEqual([
      'say "hi"',
      'a b',
      String.raw`c\d`,
    ]);
  });

  it('keeps empty quoted arguments and joins adjacent quoted parts', () => {
    expect(parseShellArgs(`"" --x="a b"c`)).toEqual(['', '--x=a bc']);
  });

  it('handles line continuations and newlines', () => {
    expect(parseShellArgs('psql \\\n  -U admin\n-d db')).toEqual(['psql', '-U', 'admin', '-d', 'db']);
  });
});
