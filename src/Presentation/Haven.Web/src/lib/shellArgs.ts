/**
 * Splits a shell-style command string into arguments.
 * Supports whitespace separation, single quotes (literal), double quotes
 * (with backslash escapes), backslash escapes and `\`-newline line continuations.
 */
export function parseShellArgs(input: string): string[] {
  const args: string[] = [];
  let current = '';
  let inToken = false;
  let quote: '"' | "'" | null = null;

  for (let i = 0; i < input.length; i++) {
    const ch = input[i];

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
      if (inToken) {
        args.push(current);
        current = '';
        inToken = false;
      }
    } else {
      current += ch;
      inToken = true;
    }
  }

  if (inToken) args.push(current);
  return args;
}
