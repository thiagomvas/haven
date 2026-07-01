import { clsx } from 'clsx';
import { HTMLAttributes, ReactNode } from 'react';

import styles from '@/styles/components/ui/Card.module.css';

interface SpacingProps {
  padding?: string | number;
  margin?: string | number;
}

interface CardProps extends HTMLAttributes<HTMLDivElement>, SpacingProps {
  children: ReactNode;
}

const getSpacingStyle = (padding?: string | number, margin?: string | number) => ({
  ...(padding !== undefined && {
    padding: typeof padding === 'number' ? `${padding}px` : padding,
  }),
  ...(margin !== undefined && {
    margin: typeof margin === 'number' ? `${margin}px` : margin,
  }),
});

export function Card({
  className,
  children,
  padding = 'var(--space-2)',
  margin,
  style,
  ...props
}: CardProps) {
  return (
    <div
      className={clsx(styles.card, className)}
      style={{ ...getSpacingStyle(padding, margin), ...style }}
      {...props}
    >
      {children}
    </div>
  );
}

export function CardHeader({ className, children, padding, margin, style, ...props }: CardProps) {
  return (
    <div
      className={clsx(styles.cardHeader, className)}
      style={{ ...getSpacingStyle(padding, margin), ...style }}
      {...props}
    >
      {children}
    </div>
  );
}

export function CardTitle({ className, children, ...props }: HTMLAttributes<HTMLHeadingElement>) {
  return (
    <h3 className={clsx(styles.cardTitle, className)} {...props}>
      {children}
    </h3>
  );
}

export function CardContent({ className, children, padding, margin, style, ...props }: CardProps) {
  return (
    <div
      className={clsx(styles.cardContent, className)}
      style={{ ...getSpacingStyle(padding, margin), ...style }}
      {...props}
    >
      {children}
    </div>
  );
}

export function CardFooter({ className, children, padding, margin, style, ...props }: CardProps) {
  return (
    <div
      className={clsx(styles.cardFooter, className)}
      style={{ ...getSpacingStyle(padding, margin), ...style }}
      {...props}
    >
      {children}
    </div>
  );
}
