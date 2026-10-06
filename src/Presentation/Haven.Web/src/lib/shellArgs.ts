/**
 * Splits a shell-style command string into arguments.
 * Supports whitespace separation, single quotes (literal), double quotes
 * (with backslash escapes), backslash escapes and `\`-newline line continuations.
 *
 * With `keepQuotes`, each argument is returned exactly as written (quotes and escapes
 * intact) so the parts can be re-joined into a script for `sh -c`.
 */
export function parseShellArgs(input: string, keepQuotes = false): string[] {
  const args: string[] = [];
  let current = '';
  let inToken = false;
  let tokenStart = 0;
  let quote: '"' | "'" | null = null;

  const pushToken = (end: number) => {
    args.push(keepQuotes ? input.slice(tokenStart, end).replace(/\\\r?\n/g, '') : current);
    current = '';
    inToken = false;
  };

  for (let i = 0; i < input.length; i++) {
    const ch = input[i];
    if (
      !inToken &&
      quote === null &&
      !/\s/.test(ch) &&
      !(ch === '\\' && /^\r?\n/.test(input.slice(i + 1)))
    ) {
      tokenStart = i;
    }

    if (quote === "'") {
      if (ch === "'") quote = null;
      else current += ch;
      continue;
    }

    if (ch === '\\') {
      const next = input[i + 1];
      if (next === undefined) {
        current += ch;
        inToken = true;
      } else if (next === '\n' || (next === '\r' && input[i + 2] === '\n')) {
        // line continuation
        i += next === '\r' ? 2 : 1;
      } else if (quote === '"' && !['"', '\\', '$', '`'].includes(next)) {
        current += ch;
        inToken = true;
      } else {
        current += next;
        inToken = true;
        i++;
      }
      continue;
    }

    if (quote === '"') {
      if (ch === '"') quote = null;
      else current += ch;
      continue;
    }

    if (ch === '"' || ch === "'") {
      quote = ch;
      inToken = true;
    } else if (/\s/.test(ch)) {
      if (inToken) pushToken(i);
    } else {
      current += ch;
      inToken = true;
    }
  }

  if (inToken) pushToken(input.length);
  return args;
}
