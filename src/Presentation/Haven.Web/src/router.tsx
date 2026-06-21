import { createBrowserRouter, Navigate } from 'react-router-dom';
import { AppShell } from '@/components/layout/AppShell';
import { LoginPage } from '@/pages/LoginPage';
import { SetupPage } from '@/pages/SetupPage';
import { SetPasswordPage } from '@/pages/SetPasswordPage';
import { DashboardPage } from '@/pages/DashboardPage';
import { ProjectsPage } from '@/pages/ProjectsPage';
import { ProjectDetailsPage } from '@/pages/ProjectDetailsPage';
import { EnvironmentsPage } from '@/pages/EnvironmentsPage';
import { EnvironmentDetailsPage } from '@/pages/EnvironmentDetailsPage';
import { ServiceDetailsPage } from '@/pages/ServiceDetailsPage';
import { ServicesPage } from '@/pages/ServicesPage';
import { GitCredentialsPage } from '@/pages/GitCredentialsPage';
import { NotificationChannelsPage } from '@/pages/NotificationChannelsPage';
import { EventsPage } from '@/pages/EventsPage';
import { ServiceRegistryPage } from '@/pages/ServiceRegistryPage';
import { NotFoundPage } from '@/pages/NotFoundPage';
import { SettingsPage } from '@/pages/settings/SettingsPage';
import { CreateServicePage } from './components/services/CreateServicePage';
import { CreateProjectPage } from './components/projects/CreateProjectPage';
import { CreateEnvironmentPage } from './components/environments';

export const router = createBrowserRouter([
  { path: '/login', element: <LoginPage /> },
  { path: '/setup', element: <SetupPage /> },
  { path: '/set-password', element: <SetPasswordPage /> },
  {
    path: '/',
    element: <AppShell />,
    children: [
      { index: true, element: <Navigate to="/dashboard" replace /> },
      { path: 'dashboard', element: <DashboardPage /> },
      { path: 'projects/create', element: <CreateProjectPage /> },
      { path: 'services/create', element: <CreateServicePage /> },
      { path: 'environments/create', element: <CreateEnvironmentPage /> },
      { path: 'projects', element: <ProjectsPage /> },
      { path: 'projects/:projectId', element: <ProjectDetailsPage /> },
      { path: 'projects/:projectId/environments', element: <EnvironmentsPage /> },
      {
        path: 'projects/:projectId/environments/:environmentId',
        element: <EnvironmentDetailsPage />,
      },
      {
        path: 'projects/:projectId/environments/:environmentId/services',
        element: <ServicesPage />,
      },
      {
        path: 'projects/:projectId/environments/:environmentId/services/:serviceId',
        element: <ServiceDetailsPage />,
      },
      { path: 'git-providers', element: <GitCredentialsPage /> },
      { path: 'notification-channels', element: <NotificationChannelsPage /> },
      { path: 'events', element: <EventsPage /> },
      { path: 'service-registry', element: <ServiceRegistryPage /> },
      { path: 'settings', element: <SettingsPage /> },
      { path: '*', element: <NotFoundPage /> },
    ],
  },
]);
