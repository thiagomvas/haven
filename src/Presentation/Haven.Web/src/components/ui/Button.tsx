import { clsx } from 'clsx';
import { AnchorHTMLAttributes, ButtonHTMLAttributes, CSSProperties, ReactNode } from 'react';

import styles from '@/styles/components/ui/Button.module.css';

type SpaceScale = 1 | 2 | 3 | 4 | 5 | 6 | 8 | 10 | 12;

type BaseProps = {
  variant?:
    | 'primary'
    | 'secondary'
    | 'danger'
    | 'ghost'
    | 'success'
    | 'warning'
    | 'outline'
    | 'text';
  size?: 'xs' | 'sm' | 'md' | 'lg' | 'xl';
  align?: 'left' | 'center' | 'right';
  /** Horizontal padding, mapped to the `--space-*` scale in index.css. Falls back to the variant default. */
  paddingX?: SpaceScale;
  /** Vertical padding, mapped to the `--space-*` scale in index.css. Falls back to the variant default. */
  paddingY?: SpaceScale;
  isLoading?: boolean;
  icon?: ReactNode;
  children?: ReactNode;
  className?: string;
};

type ButtonProps = BaseProps &
  Omit<ButtonHTMLAttributes<HTMLButtonElement>, keyof BaseProps> & { href?: undefined };

type AnchorProps = BaseProps &
  Omit<AnchorHTMLAttributes<HTMLAnchorElement>, keyof BaseProps> & { href: string };

export function Button({
  variant = 'primary',
  size = 'md',
  align = 'center',
  paddingX,
  paddingY,
  className,
  isLoading,
  icon,
  children,
  style,
  ...props
}: ButtonProps | AnchorProps) {
  const sharedClass = clsx(
    styles.button,
    styles[variant],
    styles[size],
    styles[align],
    (isLoading || (props as ButtonProps).disabled) && styles.disabled,
    className
  );

  const sharedStyle: CSSProperties = {
    ...(paddingX !== undefined && { '--button-padding-x': `var(--space-${paddingX})` }),
    ...(paddingY !== undefined && { '--button-padding-y': `var(--space-${paddingY})` }),
    ...style,
  } as CSSProperties;

  const content = isLoading ? (
    <span className={styles.loadingSpinner} />
  ) : (
    <>
      {icon && <span className={clsx(styles.icon, styles[`icon-${size}`])}>{icon}</span>}
      {children}
    </>
  );

  if ((props as AnchorProps).href !== undefined) {
    const { href, ...anchorProps } = props as AnchorProps;
    return (
      <a className={sharedClass} style={sharedStyle} href={href} {...anchorProps}>
        {content}
      </a>
    );
  }

  const { disabled, ...buttonProps } = props as ButtonProps;
  return (
    <button
      className={sharedClass}
      style={sharedStyle}
      disabled={disabled || isLoading}
      {...buttonProps}
    >
      {content}
    </button>
  );
}
