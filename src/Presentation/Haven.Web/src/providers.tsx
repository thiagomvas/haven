import { ReactNode } from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { I18nextProvider } from 'react-i18next';
import { ThemeProvider } from '@/context/ThemeContext';
import { BreadcrumbProvider } from '@/context/BreadcrumbContext';
import i18n from '@/i18n';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 1000 * 60 * 5, // 5 minutes
      gcTime: 1000 * 60 * 10, // 10 minutes
      retry: 1,
      refetchOnWindowFocus: true,
      refetchOnReconnect: true,
    },
  },
});

if (typeof window !== 'undefined') {
  const handleRouteChange = () => {
    queryClient.invalidateQueries({ queryKey: ['currentUser'] });
  };

  window.addEventListener('popstate', handleRouteChange);

  const originalPushState = window.history.pushState;
  window.history.pushState = function (...args) {
    handleRouteChange();
    return originalPushState.apply(window.history, args);
  };
}

export function Providers({ children }: { children: ReactNode }) {
  return (
    <I18nextProvider i18n={i18n}>
      <ThemeProvider>
        <BreadcrumbProvider>
          <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
        </BreadcrumbProvider>
      </ThemeProvider>
    </I18nextProvider>
  );
}
