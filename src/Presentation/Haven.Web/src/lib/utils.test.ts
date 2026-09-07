import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { TimeFormat } from '@/api/setup';
import i18n from '@/i18n';

import { cn, formatDate, formatRelative, getStatusColor } from './utils';

describe('cn', () => {
  it('merges class names, dropping falsy values', () => {
    const showB = false;
    expect(cn('a', showB && 'b', undefined, 'c')).toBe('a c');
  });
});

describe('formatDate', () => {
  it('formats an ISO string without a trailing offset as UTC', () => {
    const result = formatDate('2024-03-15T12:00:00', 'UTC', TimeFormat.Hour24);
    expect(result).toContain('2024');
    expect(result).toContain('12:00');
  });

  it('respects an ISO string that already carries an offset', () => {
    const result = formatDate('2024-03-15T12:00:00+02:00', 'UTC', TimeFormat.Hour24);
    expect(result).toContain('10:00');
  });

  it('renders 12-hour time when timeFormat is Hour12', () => {
    const result = formatDate('2024-03-15T13:00:00Z', 'UTC', TimeFormat.Hour12);
    expect(result).toMatch(/1:00\s*PM/i);
  });

  it('renders 24-hour time when timeFormat is Hour24', () => {
    const result = formatDate('2024-03-15T13:00:00Z', 'UTC', TimeFormat.Hour24);
    expect(result).toContain('13:00');
    expect(result).not.toMatch(/PM/i);
  });
});

describe('formatRelative', () => {
  const t = i18n.getFixedT('en', 'common');

  beforeEach(() => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2024-03-15T12:00:00Z'));
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('returns "just now" for timestamps within the last 10 seconds', () => {
    expect(formatRelative('2024-03-15T11:59:55Z', t)).toBe('just now');
  });

  it('formats past timestamps relative to now', () => {
    expect(formatRelative('2024-03-15T11:55:00Z', t)).toBe('5m ago');
    expect(formatRelative('2024-03-15T10:00:00Z', t)).toBe('2h ago');
    expect(formatRelative('2024-03-13T12:00:00Z', t)).toBe('2d ago');
  });

  it('formats future timestamps relative to now', () => {
    expect(formatRelative('2024-03-15T12:05:00Z', t)).toBe('in 5m');
    expect(formatRelative('2024-03-15T14:00:00Z', t)).toBe('in 2h');
    expect(formatRelative('2024-03-17T12:00:00Z', t)).toBe('in 2d');
  });
});

describe('getStatusColor', () => {
  it.each([
    ['Running', 'success'],
    ['Stopped', 'default'],
    ['Degraded', 'warning'],
    ['DeploymentPending', 'default'],
    ['Unknown', 'default'],
    ['SomethingElse', 'default'],
  ] as const)('maps %s to %s', (status, expected) => {
    expect(getStatusColor(status)).toBe(expected);
  });
});
