import { createBrowserRouter, Navigate } from 'react-router-dom'
import { AppShell } from '@/components/layout/AppShell'
import { DashboardPage } from '@/pages/DashboardPage'
import { ProjectsPage } from '@/pages/ProjectsPage'
import { ProjectDetailsPage } from '@/pages/ProjectDetailsPage'
import { EnvironmentsPage } from '@/pages/EnvironmentsPage'
import { EnvironmentDetailsPage } from '@/pages/EnvironmentDetailsPage'
import { ServiceDetailsPage } from '@/pages/ServiceDetailsPage'
import { ServicesPage } from '@/pages/ServicesPage'
import { GitCredentialsPage } from '@/pages/GitCredentialsPage'
import { EventsPage } from '@/pages/EventsPage'
import { NotFoundPage } from '@/pages/NotFoundPage'
import { CreateServicePage } from './components/services/CreateServicePage'

export const router = createBrowserRouter([
  {
    path: '/',
    element: <AppShell />,
    children: [
      { index: true, element: <Navigate to="/dashboard" replace /> },
      { path: 'dashboard', element: <DashboardPage /> },
      { path: 'services/create', element: <CreateServicePage /> },
      { path: 'projects', element: <ProjectsPage /> },
      { path: 'projects/:projectId', element: <ProjectDetailsPage /> },
      { path: 'projects/:projectId/environments', element: <EnvironmentsPage /> },
      { path: 'projects/:projectId/environments/:environmentId', element: <EnvironmentDetailsPage /> },
      { path: 'projects/:projectId/environments/:environmentId/services', element: <ServicesPage /> },
      { path: 'projects/:projectId/environments/:environmentId/services/:serviceId', element: <ServiceDetailsPage /> },
      { path: 'git-providers', element: <GitCredentialsPage /> },
      { path: 'events', element: <EventsPage /> },
      { path: '*', element: <NotFoundPage /> },
    ],
  },
])
