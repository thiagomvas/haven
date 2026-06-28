import { clsx } from 'clsx';
import { HTMLAttributes, ReactNode } from 'react';

import styles from './Chip.module.css';

interface ChipProps extends Omit<HTMLAttributes<HTMLDivElement>, 'content'> {
  icon?: ReactNode;
  content: ReactNode;
  variant?: 'primary' | 'success' | 'warning' | 'danger' | 'default';
  size?: 'sm' | 'md' | 'lg';
  borderColor?: string;
  textColor?: string;
}

function hexToRgba(hex: string, alpha: number): string {
  const r = parseInt(hex.slice(1, 3), 16);
  const g = parseInt(hex.slice(3, 5), 16);
  const b = parseInt(hex.slice(5, 7), 16);
  return `rgba(${r}, ${g}, ${b}, ${alpha})`;
}

export function Chip({
  icon,
  content,
  variant = 'default',
  size = 'md',
  borderColor,
  textColor,
  className,
  style,
  ...props
}: ChipProps) {
  const customStyle = borderColor
    ? {
        backgroundColor: borderColor.startsWith('var(')
          ? `color-mix(in srgb, ${borderColor} 10%, transparent)`
          : hexToRgba(borderColor, 0.1),
        borderColor,
        color: textColor || borderColor,
        ...style,
      }
    : style;

  return (
    <div
      className={clsx(
        styles.chip,
        borderColor && styles.outlined,
        !borderColor && styles[variant],
        styles[size],
        className
      )}
      style={customStyle}
      {...props}
    >
      {icon && <div className={styles.icon}>{icon}</div>}
      <div className={styles.content}>{content}</div>
    </div>
  );
}
