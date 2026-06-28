import { useLayoutEffect } from 'react';

import { BreadcrumbItem, useBreadcrumbContext } from '@/context/BreadcrumbContext';

export function useSetBreadcrumbs(items: BreadcrumbItem[]) {
  const { setBreadcrumbs } = useBreadcrumbContext();

  useLayoutEffect(() => {
    setBreadcrumbs(items);
    return () => setBreadcrumbs([]);
  }, [JSON.stringify(items), setBreadcrumbs]);
}
