import { useInstance } from './useInstance';
import { formatDate } from '@/lib/utils';

export function useFormatDate() {
  const { data: instance } = useInstance();

  return (iso: string) => formatDate(iso, instance?.timezone, instance?.timeFormat);
}
