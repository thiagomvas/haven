import { useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { servicesApi } from '../api/services';
import { Spinner } from '../components/ui/Spinner';

export function ServiceRedirectPage() {
  const { serviceId } = useParams<{ serviceId: string }>();
  const navigate = useNavigate();

  useEffect(() => {
    if (!serviceId) return;

    servicesApi.resolve(serviceId).then(location => {
      if (location) {
        navigate(
          `/projects/${location.projectId}/environments/${location.environmentId}/services/${location.serviceId}`,
          { replace: true }
        );
      } else {
        navigate('/not-found', { replace: true });
      }
    });
  }, [serviceId, navigate]);

  return (
    <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100%' }}>
      <Spinner />
    </div>
  );
}
