import { clsx } from 'clsx';
import { HTMLAttributes, ReactNode, useRef, useState } from 'react';

import styles from '@/styles/components/ui/CodeBlock.module.css';

interface CodeBlockProps extends HTMLAttributes<HTMLDivElement> {
  header?: ReactNode;
  icon?: ReactNode;
  copyable?: boolean;
  onCopySuccess?: () => void;
  code: string;
}

export function CodeBlock({
  header,
  icon,
  copyable = false,
  onCopySuccess,
  code,
  className,
  children,
  ...props
}: CodeBlockProps) {
  const [copied, setCopied] = useState(false);
  const timeoutRef = useRef<ReturnType<typeof setTimeout> | undefined>(undefined);

  const handleCopy = async () => {
    try {
      await navigator.clipboard.writeText(code);
      setCopied(true);
      onCopySuccess?.();
      timeoutRef.current = setTimeout(() => setCopied(false), 2000);
    } catch (err) {
      console.error('Failed to copy:', err);
    }
  };

  return (
    <div className={clsx(styles.codeBlock, className)} {...props}>
      {(header || icon) && (
        <div className={styles.header}>
          <div className={styles.headerContent}>
            {icon && <span className={styles.icon}>{icon}</span>}
            {header && <span className={styles.headerText}>{header}</span>}
          </div>
          {copyable && (
            <button
              className={clsx(styles.copyButton, copied && styles.copied)}
              onClick={handleCopy}
              title={copied ? 'Copied!' : 'Copy'}
              type="button"
              aria-label="Copy code block"
            >
              {copied ? '✓' : '📋'}
            </button>
          )}
        </div>
      )}
      <pre className={styles.pre}>
        <code className={styles.code}>{code}</code>
      </pre>
      {!header && !icon && copyable && (
        <button
          className={clsx(styles.copyButtonFloating, copied && styles.copied)}
          onClick={handleCopy}
          title={copied ? 'Copied!' : 'Copy'}
          type="button"
          aria-label="Copy code block"
        >
          {copied ? '✓' : '📋'}
        </button>
      )}
    </div>
  );
}
