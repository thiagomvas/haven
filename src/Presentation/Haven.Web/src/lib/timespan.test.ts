import { describe, expect, it } from 'vitest';

import { secondsToTimeSpan, timeSpanToSeconds } from './timespan';

describe('timespan', () => {
  it('formats seconds as a TimeSpan', () => {
    expect(secondsToTimeSpan(30)).toBe('00:00:30');
    expect(secondsToTimeSpan(3725)).toBe('01:02:05');
    expect(secondsToTimeSpan(90000)).toBe('1.01:00:00');
  });

  it('parses TimeSpan strings', () => {
    expect(timeSpanToSeconds('00:00:30')).toBe(30);
    expect(timeSpanToSeconds('01:02:05')).toBe(3725);
    expect(timeSpanToSeconds('1.01:00:00')).toBe(90000);
    expect(timeSpanToSeconds('garbage')).toBe(0);
  });
});
