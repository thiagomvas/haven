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

  it('keeps punctuation and inner quotes inside a quoted SQL argument', () => {
    expect(
      parseShellArgs(
        `psql -U postgres -d achvo -c "UPDATE sync.sync_state SET next_due_utc = now() WHERE job_name = 'steam-app-list';"`
      )
    ).toEqual([
      'psql',
      '-U',
      'postgres',
      '-d',
      'achvo',
      '-c',
      "UPDATE sync.sync_state SET next_due_utc = now() WHERE job_name = 'steam-app-list';",
    ]);
  });

  it('keeps quotes and escapes verbatim with keepQuotes', () => {
    expect(parseShellArgs(`psql -c "UPDATE t SET a = 'x';" 'a b' a\\ b ""`, true)).toEqual([
      'psql',
      '-c',
      `"UPDATE t SET a = 'x';"`,
      `'a b'`,
      String.raw`a\ b`,
      '""',
    ]);
  });

  it('drops line continuations with keepQuotes', () => {
    expect(parseShellArgs('psql \\\n  -U admin', true)).toEqual(['psql', '-U', 'admin']);
  });

  it('handles line continuations and newlines', () => {
    expect(parseShellArgs('psql \\\n  -U admin\n-d db')).toEqual([
      'psql',
      '-U',
      'admin',
      '-d',
      'db',
    ]);
  });
});
