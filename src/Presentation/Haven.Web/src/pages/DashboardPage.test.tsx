import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import type { MeResponse } from '@/api/auth';
import { dashboardApi } from '@/api/dashboard';
import { eventsApi } from '@/api/events';
import { projectsApi } from '@/api/projects';
import type {
  DashboardOverviewDto,
  EventDto,
  ProjectDashboardDto,
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
}: {
  overviewData?: DashboardOverviewDto;
  projects?: ProjectDashboardDto[];
  events?: EventDto[];
} = {}) {
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

    expect(await screen.findByText('4')).toBeInTheDocument();
    expect(screen.getByText('7')).toBeInTheDocument();
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

    const banner = await screen.findByText(/Acme API \/ production/);
    expect(banner).toHaveTextContent('2 services need attention');

    await user.click(banner);

    expect(mockNavigate).toHaveBeenCalledWith('/projects/proj-1/environments/env-1');
  });

  it('navigates to the project page when a project row is clicked', async () => {
    const user = userEvent.setup();
    mockData({ projects: [project({ id: 'proj-42', name: 'Billing Service' })] });

    renderWithProviders(<DashboardPage />);

    const row = (await screen.findByText('Billing Service')).closest('tr');
    expect(row).not.toBeNull();

    await user.click(row!);

    expect(mockNavigate).toHaveBeenCalledWith('/projects/proj-42');
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
    expect(screen.queryByText('System Overview')).not.toBeInTheDocument();
    expect(screen.queryByText('Recent Events')).not.toBeInTheDocument();
    expect(screen.queryByText('Quick Actions')).not.toBeInTheDocument();
  });

  it('only shows the quick-actions card for users who can create a project', async () => {
    vi.mocked(useCurrentUser).mockReturnValue(user({ permissions: ['projects.read'] }));

    renderWithProviders(<DashboardPage />);

    await screen.findByText('System Overview');
    expect(screen.queryByText('Quick Actions')).not.toBeInTheDocument();
  });
});
