import { useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';

import { environmentsApi } from '../api/environments';
import { Spinner } from '../components/ui/Spinner';

export function EnvironmentRedirectPage() {
  const { environmentId } = useParams<{ environmentId: string }>();
  const navigate = useNavigate();

  useEffect(() => {
    if (!environmentId) return;

    environmentsApi.resolve(environmentId).then(location => {
      if (location) {
        navigate(`/projects/${location.projectId}/environments/${location.environmentId}`, {
          replace: true,
        });
      } else {
        navigate('/not-found', { replace: true });
      }
    });
  }, [environmentId, navigate]);

  return (
    <div
      style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100%' }}
    >
      <Spinner />
    </div>
  );
}
