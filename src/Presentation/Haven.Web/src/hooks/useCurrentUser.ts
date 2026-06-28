import { useQuery } from '@tanstack/react-query';

import { authApi, MeResponse } from '@/api/auth';

export function useCurrentUser() {
  const { data: user = null } = useQuery<MeResponse | null>({
    queryKey: ['currentUser'],
    queryFn: () => authApi.me(),
    retry: false,
  });

  return user;
}
