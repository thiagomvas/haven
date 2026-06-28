import { useState, useEffect } from 'react';
import { projectsApi } from '../api/projects';
import { environmentsApi } from '../api/environments';
import { EnvironmentDto } from '@/api/types/environment.types';
import { ProjectDto } from '@/api/types/project.types';

export function useProjectsAndEnvironments() {
  const [projects, setProjects] = useState<ProjectDto[]>([]);
  const [environments, setEnvironments] = useState<EnvironmentDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const loadProjects = async () => {
      try {
        setLoading(true);
        setError(null);
        const result = await projectsApi.getAll({ pageNumber: 1, pageSize: 100 });
        setProjects(result.items);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load projects');
      } finally {
        setLoading(false);
      }
    };

    loadProjects();
  }, []);

  const loadEnvironments = async (projectId: string) => {
    if (!projectId) {
      setEnvironments([]);
      return;
    }
    try {
      const envs = await environmentsApi.getByProjectId(projectId);
      setEnvironments(envs);
    } catch (err) {
      console.error('Failed to load environments', err);
      setEnvironments([]);
    }
  };

  return {
    projects,
    environments,
    loading,
    error,
    loadEnvironments,
  };
}
