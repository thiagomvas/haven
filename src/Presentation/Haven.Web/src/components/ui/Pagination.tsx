import { ChevronLeft, ChevronRight } from 'lucide-react';
import { useTranslation } from 'react-i18next';

import styles from '@/styles/components/ui/Pagination.module.css';

import { Button } from './Button';

interface PaginationProps {
  pageNumber: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
  onPreviousPage: () => void;
  onNextPage: () => void;
}

export function Pagination({
  pageNumber,
  totalPages,
  hasPreviousPage,
  hasNextPage,
  onPreviousPage,
  onNextPage,
}: PaginationProps) {
  const { t } = useTranslation('common');

  if (totalPages <= 1) return null;

  return (
    <div className={styles.pagination}>
      <Button
        variant="ghost"
        icon={<ChevronLeft size={18} />}
        onClick={onPreviousPage}
        disabled={!hasPreviousPage}
        aria-label={t('labels.previousPage')}
      />
      <span className={styles.info}>
        {t('labels.pageOf', { current: pageNumber, total: totalPages })}
      </span>
      <Button
        variant="ghost"
        icon={<ChevronRight size={18} />}
        onClick={onNextPage}
        disabled={!hasNextPage}
        aria-label={t('labels.nextPage')}
      />
    </div>
  );
}
