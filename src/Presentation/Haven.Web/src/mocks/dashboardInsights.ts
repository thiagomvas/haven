import type { DashboardInsightsDto, EnvironmentInsightDto } from '@/api/types';

/**
 * SAMPLE DATA. Mirrors the proposed `DashboardInsightsDto` contract so the home page can be
 * designed against realistic shapes before the endpoints exist. Replace the body of
 * `getDashboardInsights` with an API call when they do.
 */

const MIN = 60_000;
const HOUR = 60 * MIN;

const ago = (ms: number) => new Date(Date.now() - ms).toISOString();
const inFuture = (ms: number) => new Date(Date.now() + ms).toISOString();

/** Deterministic pseudo-random so the mock does not flicker between renders. */
function rng(seed: number) {
  let s = seed;
  return () => {
    s = (s * 1664525 + 1013904223) % 4294967296;
    return s / 4294967296;
  };
}

function series(seed: number, base: number, amplitude: number, spikeAt?: number, spike = 0) {
  const r = rng(seed);
  let v = base;
  return Array.from({ length: 48 }, (_, i) => {
    v += (r() - 0.5) * amplitude;
    v = Math.min(Math.max(v, base - amplitude * 3), base + amplitude * 3);
    const bump = spikeAt !== undefined && Math.abs(i - spikeAt) < 3 ? spike : 0;
    return Math.max(0, Math.round((v + bump) * 10) / 10);
  });
}

export function getDashboardInsights(): DashboardInsightsDto {
  return {
    host: {
      hostname: 'docker-host-01',
      cores: 8,
      uptimeSeconds: 41 * 24 * 3600 + 5 * 3600,
      dockerVersion: '27.3.1',
      runningContainers: 29,
      totalContainers: 35,
      metrics: [
        {
          key: 'cpu',
          value: 38,
          unit: '%',
          capacity: 8,
          percent: 38,
          series: series(11, 34, 6, 44, 28),
        },
        {
          key: 'memory',
          value: 11.4,
          unit: 'GB',
          capacity: 16,
          percent: 71,
          series: series(23, 10.6, 0.5),
        },
        {
          key: 'disk',
          value: 412,
          unit: 'GB',
          capacity: 500,
          percent: 82,
          series: series(37, 400, 1.2),
        },
        { key: 'network', value: 6.8, unit: 'MB/s', series: series(51, 5, 3, 20, 9) },
      ],
    },
    jobs: [
      {
        key: 'backup',
        name: 'Database backup',
        kind: 'backup',
        lastRunTime: ago(5 * HOUR),
        lastStatus: 'Succeeded',
        nextRunTime: inFuture(19 * HOUR),
        detail: '184 MB',
      },
      {
        key: 'docker-cleanup',
        name: 'Docker cleanup',
        kind: 'cleanup',
        lastRunTime: ago(26 * HOUR),
        lastStatus: 'Succeeded',
        nextRunTime: inFuture(22 * HOUR),
        detail: '3.2 GB reclaimed',
      },
      {
        key: 'repo-cleanup',
        name: 'Repository cleanup',
        kind: 'cleanup',
        lastRunTime: ago(26 * HOUR),
        lastStatus: 'Failed',
        nextRunTime: inFuture(22 * HOUR),
        detail: 'Permission denied: /data/repos',
      },
      {
        key: 'manifest-sync',
        name: 'Manifest sync',
        kind: 'sync',
        lastRunTime: ago(12 * MIN),
        lastStatus: 'Succeeded',
        nextRunTime: inFuture(18 * MIN),
      },
    ],
  };
}

/** Per-environment figures keyed by environment id; deterministic from the id. */
export function getEnvironmentInsight(
  environmentId: string,
  runningServices: number
): EnvironmentInsightDto {
  let h = 0;
  for (const c of environmentId) h = (h * 31 + c.charCodeAt(0)) >>> 0;
  const r = rng(Math.imul(h + 1, 2654435761) >>> 0);
  r();
  r();
  return {
    environmentId,
    availability24hPercent: Math.round((98.2 + r() * 1.79) * 100) / 100,
    cpuPercent: Math.round(runningServices * (4 + r() * 14)),
    memoryMb: Math.round(runningServices * (90 + r() * 240)),
  };
}
