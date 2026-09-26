export function parseEnvFile(content: string): Record<string, string> {
  const result: Record<string, string> = {};

  for (const rawLine of content.split(/\r\n|\n/)) {
    const line = rawLine.trim();
    if (!line || line.startsWith('#')) continue;

    const eqIndex = line.indexOf('=');
    if (eqIndex === -1) continue;

    const key = line.slice(0, eqIndex).trim();
    const value = line.slice(eqIndex + 1).trim();
    if (key) result[key] = value;
  }

  return result;
}

export function serializeEnvFile(vars: Record<string, string>): string {
  return Object.entries(vars)
    .map(([key, value]) => `${key}=${value}`)
    .join('\n');
}

/** Merges two .env-style texts by key, with values from `overrides` taking precedence. */
export function mergeEnvFiles(base: string, overrides: string): string {
  return serializeEnvFile({ ...parseEnvFile(base), ...parseEnvFile(overrides) });
}
