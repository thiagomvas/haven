/** Converts seconds to a .NET TimeSpan string ("hh:mm:ss", or "d.hh:mm:ss" past a day). */
export function secondsToTimeSpan(totalSeconds: number): string {
  const s = Math.max(0, Math.floor(totalSeconds));
  const days = Math.floor(s / 86400);
  const hours = Math.floor((s % 86400) / 3600);
  const minutes = Math.floor((s % 3600) / 60);
  const seconds = s % 60;
  const pad = (n: number) => String(n).padStart(2, '0');
  const clock = `${pad(hours)}:${pad(minutes)}:${pad(seconds)}`;
  return days > 0 ? `${days}.${clock}` : clock;
}

/** Parses a .NET TimeSpan string ("[d.]hh:mm:ss[.fff]") to whole seconds. Returns 0 if unparseable. */
export function timeSpanToSeconds(value: string): number {
  const match = /^(?:(\d+)\.)?(\d+):(\d{2}):(\d{2})(?:\.\d+)?$/.exec(value.trim());
  if (!match) return 0;
  const [, days, hours, minutes, seconds] = match;
  return Number(days ?? 0) * 86400 + Number(hours) * 3600 + Number(minutes) * 60 + Number(seconds);
}
