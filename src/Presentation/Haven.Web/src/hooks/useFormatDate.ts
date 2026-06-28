import { formatDate } from '@/lib/utils';

import { useInstance } from './useInstance';

export function useFormatDate() {
  const { data: instance } = useInstance();

  return (iso: string) => formatDate(iso, instance?.timezone, instance?.timeFormat);
}
