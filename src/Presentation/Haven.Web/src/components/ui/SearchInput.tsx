import { clsx } from 'clsx';
import { Search } from 'lucide-react';
import { InputHTMLAttributes } from 'react';

import styles from '@/styles/components/ui/SearchInput.module.css';

import { Input } from './Input';

interface SearchInputProps extends InputHTMLAttributes<HTMLInputElement> {
  wrapperClassName?: string;
}

export function SearchInput({ wrapperClassName, className, ...props }: SearchInputProps) {
  return (
    <div className={clsx(styles.wrapper, wrapperClassName)}>
      <Search size={16} className={styles.icon} />
      <Input className={clsx(styles.input, className)} {...props} />
    </div>
  );
}
