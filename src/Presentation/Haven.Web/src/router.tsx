import { createBrowserRouter, Navigate } from 'react-router-dom'
import { AppShell } from '@/components/layout/AppShell'
import { DashboardPage } from '@/pages/DashboardPage'
import { ProjectsPage } from '@/pages/ProjectsPage'
import { ProjectDetailsPage } from '@/pages/ProjectDetailsPage'
import { EnvironmentsPage } from '@/pages/EnvironmentsPage'
import { ServicesPage } from '@/pages/ServicesPage'
import { EventsPage } from '@/pages/EventsPage'

export const router = createBrowserRouter([
  {
    path: '/',
    element: <AppShell />,
    children: [
      { index: true, element: <Navigate to="/dashboard" replace /> },
      { path: 'dashboard', element: <DashboardPage /> },
      { path: 'projects', element: <ProjectsPage /> },
      { path: 'projects/:projectId', element: <ProjectDetailsPage /> },
      { path: 'projects/:projectId/environments', element: <EnvironmentsPage /> },
      { path: 'projects/:projectId/environments/:environmentId/services', element: <ServicesPage /> },
      { path: 'events', element: <EventsPage /> },
    ],
  },
])
