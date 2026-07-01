import { clsx } from 'clsx';
import { InputHTMLAttributes, ReactNode, useEffect, useId, useRef } from 'react';

import styles from '@/styles/components/ui/Checkbox.module.css';

interface CheckboxProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'type'> {
  label: string;
  description?: string;
  icon?: ReactNode;
  indeterminate?: boolean;
}

export function Checkbox({
  label,
  description,
  icon,
  id: idProp,
  className,
  indeterminate,
  ...props
}: CheckboxProps) {
  const generatedId = useId();
  const id = idProp ?? generatedId;
  const ref = useRef<HTMLInputElement>(null);

  useEffect(() => {
    if (ref.current) ref.current.indeterminate = indeterminate ?? false;
  }, [indeterminate]);

  return (
    <label className={clsx(styles.wrapper, className)} htmlFor={id}>
      <input {...props} ref={ref} id={id} type="checkbox" className={styles.input} />
      {icon && <span className={styles.icon}>{icon}</span>}
      <span className={styles.box} aria-hidden="true">
        <svg className={styles.checkmark} viewBox="0 0 10 10">
          <polyline points="1.5,5 4,7.5 8.5,2.5" />
        </svg>
        <span className={styles.dash} />
      </span>
      <span className={styles.text}>
        <span className={styles.label}>{label}</span>
        {description && <span className={styles.description}>{description}</span>}
      </span>
    </label>
  );
}
