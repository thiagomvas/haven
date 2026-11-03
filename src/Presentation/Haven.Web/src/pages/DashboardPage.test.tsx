import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import type { MeResponse } from '@/api/auth';
import { dashboardApi } from '@/api/dashboard';
import { eventsApi } from '@/api/events';
import { projectsApi } from '@/api/projects';
import type {
  AttentionServiceDto,
  DashboardOverviewDto,
  EventDto,
  ProjectDashboardDto,
  RecentDeploymentDto,
  ServiceStatisticsDto,
} from '@/api/types';
import { useCurrentUser } from '@/hooks/useCurrentUser';
import { DashboardPage } from '@/pages/DashboardPage';
import { renderWithProviders, screen, waitFor } from '@/test/render';

vi.mock('@/api/dashboard');
vi.mock('@/api/projects');
vi.mock('@/api/events');
vi.mock('@/hooks/useCurrentUser');

const mockNavigate = vi.fn();
vi.mock('react-router-dom', async importOriginal => {
  const actual = await importOriginal<typeof import('react-router-dom')>();
  return { ...actual, useNavigate: () => mockNavigate };
});

const stats = (overrides: Partial<ServiceStatisticsDto> = {}): ServiceStatisticsDto => ({
  total: 0,
  running: 0,
  stopped: 0,
  crashed: 0,
  degraded: 0,
  deploymentPending: 0,
  deploying: 0,
  unknown: 0,
  ...overrides,
});

const overview = (overrides: Partial<DashboardOverviewDto> = {}): DashboardOverviewDto => ({
  totalProjects: 2,
  totalEnvironments: 3,
  serviceStatistics: stats({ total: 5, running: 5 }),
  deploymentsLast24h: 0,
  ...overrides,
});

const project = (overrides: Partial<ProjectDashboardDto> = {}): ProjectDashboardDto => ({
  id: 'proj-1',
  name: 'Acme API',
  environments: [],
  serviceStatistics: stats(),
  totalEnvVars: 0,
  environmentVariables: [],
  ...overrides,
});

const event = (overrides: Partial<EventDto> = {}): EventDto => ({
  id: 'evt-1',
  eventType: 'ServiceDeployed',
  message: 'Service api deployed',
  triggeredAt: new Date().toISOString(),
  ...overrides,
});

const user = (overrides: Partial<MeResponse> = {}): MeResponse => ({
  id: 'user-1',
  name: 'Test User',
  email: 'user@example.com',
  requirePasswordChange: false,
  isAdmin: false,
  permissions: [],
  ...overrides,
});

function mockData({
  overviewData = overview(),
  projects = [project()],
  events = [event()],
  attention = [],
  deployments = [],
}: {
  overviewData?: DashboardOverviewDto;
  projects?: ProjectDashboardDto[];
  events?: EventDto[];
  attention?: AttentionServiceDto[];
  deployments?: RecentDeploymentDto[];
} = {}) {
  vi.mocked(dashboardApi.getAttention).mockResolvedValue(attention);
  vi.mocked(dashboardApi.getRecentDeployments).mockResolvedValue(deployments);
  vi.mocked(dashboardApi.getOverview).mockResolvedValue(overviewData);
  vi.mocked(projectsApi.getDashboard).mockResolvedValue({
    items: projects,
    totalCount: projects.length,
    pageNumber: 1,
    pageSize: 100,
    totalPages: 1,
    hasPreviousPage: false,
    hasNextPage: false,
  });
  vi.mocked(eventsApi.getAll).mockResolvedValue({
    items: events,
    totalCount: events.length,
    pageNumber: 1,
    pageSize: 5,
    totalPages: 1,
    hasPreviousPage: false,
    hasNextPage: false,
  });
}

describe('DashboardPage', () => {
  beforeEach(() => {
    vi.mocked(useCurrentUser).mockReturnValue(user({ isAdmin: true }));
    mockData();
  });

  it('renders overview stats, project list, and recent events once data loads', async () => {
    mockData({
      overviewData: overview({ totalProjects: 4, totalEnvironments: 7 }),
      projects: [project({ name: 'Acme API' })],
      events: [event({ message: 'Service api deployed' })],
    });

    renderWithProviders(<DashboardPage />);

    expect(await screen.findByText('4 projects · 7 environments')).toBeInTheDocument();
    expect(screen.getByText('Acme API')).toBeInTheDocument();
    expect(screen.getByText('Service api deployed')).toBeInTheDocument();
  });

  it('shows the all-healthy banner when there is no environment needing attention', async () => {
    mockData({ overviewData: overview({ attentionEnvironment: undefined }) });

    renderWithProviders(<DashboardPage />);

    expect(await screen.findByText('All environments healthy')).toBeInTheDocument();
  });

  it('shows an attention banner and navigates to the affected environment on click', async () => {
    const user = userEvent.setup();
    mockData({
      overviewData: overview({
        attentionEnvironment: {
          projectId: 'proj-1',
          projectName: 'Acme API',
          environmentId: 'env-1',
          environmentName: 'production',
          status: 'Degraded' as never,
          affectedServiceCount: 2,
        },
      }),
    });

    renderWithProviders(<DashboardPage />);

    const title = await screen.findByText(/Acme API \/ production/);
    const banner = title.closest('[role="button"]') as HTMLElement;
    expect(banner).toHaveTextContent('2 services need attention');

    await user.click(banner);

    expect(mockNavigate).toHaveBeenCalledWith('/projects/proj-1/environments/env-1');
  });

  it('navigates to the environment page when an environment row is clicked', async () => {
    const user = userEvent.setup();
    mockData({
      projects: [
        project({
          id: 'proj-42',
          name: 'Billing Service',
          environments: [
            {
              id: 'env-7',
              name: 'staging',
              projectId: 'proj-42',
              projectName: 'Billing Service',
              networkName: 'net',
              serviceStatistics: stats({ total: 2, running: 2 }),
              status: 'Running' as never,
              totalEnvVars: 0,
              environmentVariables: [],
              services: [],
              serviceStatusMap: {},
            },
          ],
        }),
      ],
    });

    renderWithProviders(<DashboardPage />);

    const row = (await screen.findByText('staging')).closest('[role="row"]');
    expect(row).not.toBeNull();

    await user.click(row as HTMLElement);

    expect(mockNavigate).toHaveBeenCalledWith('/projects/proj-42/environments/env-7');
  });

  it('"Needs attention" keeps degraded and crashed environments but not intentionally stopped ones', async () => {
    const user = userEvent.setup();
    const env = (id: string, name: string, statistics: ServiceStatisticsDto) => ({
      id,
      name,
      projectId: 'proj-1',
      projectName: 'Acme API',
      networkName: 'net',
      serviceStatistics: statistics,
      status: 'Running' as never,
      totalEnvVars: 0,
      environmentVariables: [],
      services: [],
      serviceStatusMap: {},
    });
    mockData({
      projects: [
        project({
          environments: [
            env('e1', 'healthy-env', stats({ total: 2, running: 2 })),
            env('e2', 'stopped-env', stats({ total: 2, stopped: 2 })),
            env('e3', 'crashed-env', stats({ total: 2, running: 1, stopped: 1, crashed: 1 })),
            env('e4', 'degraded-env', stats({ total: 2, running: 2, degraded: 1 })),
          ],
        }),
      ],
    });

    renderWithProviders(<DashboardPage />);
    await screen.findByText('healthy-env');

    await user.click(screen.getByRole('button', { name: 'Needs attention' }));

    expect(screen.getByText('crashed-env')).toBeInTheDocument();
    expect(screen.getByText('degraded-env')).toBeInTheDocument();
    expect(screen.queryByText('healthy-env')).not.toBeInTheDocument();
    expect(screen.queryByText('stopped-env')).not.toBeInTheDocument();
  });

  it('lists services that need attention with the reason and a link to the service', async () => {
    mockData({
      attention: [
        {
          serviceId: 'svc-1',
          serviceName: 'worker',
          projectId: 'proj-1',
          projectName: 'Acme API',
          environmentId: 'env-1',
          environmentName: 'production',
          reason: 'Crashed',
          since: new Date(Date.now() - 5 * 60_000).toISOString(),
        },
      ],
    });

    renderWithProviders(<DashboardPage />);

    const link = await screen.findByRole('link', { name: 'worker' });
    expect(link).toHaveAttribute('href', '/projects/proj-1/environments/env-1/services/svc-1');
    expect(screen.getByText('Container exited unexpectedly')).toBeInTheDocument();
  });

  it('shows an all-clear message when no service needs attention', async () => {
    mockData({ attention: [] });

    renderWithProviders(<DashboardPage />);

    expect(await screen.findByText(/Nothing needs attention/)).toBeInTheDocument();
  });

  it('shows recent deployments with their outcome', async () => {
    mockData({
      deployments: [
        {
          id: 'dep-1',
          serviceId: 'svc-9',
          serviceName: 'gateway',
          projectId: 'proj-1',
          projectName: 'Acme API',
          environmentId: 'env-1',
          environmentName: 'production',
          status: 'Failed',
          triggeredBy: 'webhook',
          identifier: 'a08f3b9c1',
          commitMessage: 'Tighten CORS allow-list',
          startedAt: new Date(Date.now() - 60 * 60_000).toISOString(),
          finishedAt: new Date(Date.now() - 60 * 60_000 + 71_000).toISOString(),
        },
      ],
    });

    renderWithProviders(<DashboardPage />);

    expect(await screen.findByText('Tighten CORS allow-list')).toBeInTheDocument();
    expect(screen.getByText('a08f3b9')).toBeInTheDocument();
    expect(screen.getByText('failed after 1m 11s')).toBeInTheDocument();
  });

  it('shows an error message when the attention list cannot be loaded', async () => {
    mockData();
    vi.mocked(dashboardApi.getAttention).mockRejectedValue(new Error('boom'));

    renderWithProviders(<DashboardPage />);

    expect(
      await screen.findByText('Could not load services that need attention.')
    ).toBeInTheDocument();
  });

  it('shows an empty state when there are no projects', async () => {
    mockData({ projects: [] });

    renderWithProviders(<DashboardPage />);

    expect(
      await screen.findByText('No projects yet. Create one to get started!')
    ).toBeInTheDocument();
  });

  it('shows an empty state when there are no recent events', async () => {
    mockData({ events: [] });

    renderWithProviders(<DashboardPage />);

    expect(
      await screen.findByText('No events yet. Start by creating a project!')
    ).toBeInTheDocument();
  });

  it('hides project and event sections for users without read permission', async () => {
    vi.mocked(useCurrentUser).mockReturnValue(user({ permissions: [] }));

    renderWithProviders(<DashboardPage />);

    await waitFor(() => expect(dashboardApi.getOverview).not.toHaveBeenCalled());
    expect(screen.queryByRole('heading', { name: 'Projects' })).not.toBeInTheDocument();
    expect(screen.queryByText('Recent Events')).not.toBeInTheDocument();
    expect(screen.queryByText('Create a new Service')).not.toBeInTheDocument();
  });

  it('only shows the create actions for users who can create a project', async () => {
    vi.mocked(useCurrentUser).mockReturnValue(user({ permissions: ['projects.read'] }));

    renderWithProviders(<DashboardPage />);

    await screen.findByRole('heading', { name: 'Projects' });
    expect(screen.queryByText('Create a new Service')).not.toBeInTheDocument();
  });
});
