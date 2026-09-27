import { clsx } from 'clsx';
import { Check, Copy } from 'lucide-react';
import { HTMLAttributes, ReactNode, useRef, useState } from 'react';

import styles from '@/styles/components/ui/CodeSpan.module.css';

interface CodeSpanProps extends HTMLAttributes<HTMLSpanElement> {
  icon?: ReactNode;
  copyable?: boolean;
  onCopySuccess?: () => void;
  /**
   * When set, called to resolve the text to copy instead of the rendered `children`. Useful when
   * the displayed content is masked (e.g. a secret) but the real value should still be copyable,
   * fetched lazily only when the user actually clicks copy.
   */
  onBeforeCopy?: () => Promise<string>;
}

export function CodeSpan({
  icon,
  copyable = false,
  onCopySuccess,
  onBeforeCopy,
  className,
  children,
  ...props
}: CodeSpanProps) {
  const [copied, setCopied] = useState(false);
  const timeoutRef = useRef<ReturnType<typeof setTimeout> | undefined>(undefined);
  const textContent = typeof children === 'string' ? children : '';

  const copyWithFallback = (text: string) => {
    const textarea = document.createElement('textarea');
    textarea.value = text;
    textarea.style.position = 'fixed';
    textarea.style.opacity = '0';
    document.body.appendChild(textarea);
    textarea.focus();
    textarea.select();
    try {
      return document.execCommand('copy');
    } finally {
      document.body.removeChild(textarea);
    }
  };

  const handleCopy = async () => {
    try {
      const text = onBeforeCopy ? await onBeforeCopy() : textContent;
      if (navigator.clipboard) {
        await navigator.clipboard.writeText(text);
      } else if (!copyWithFallback(text)) {
        throw new Error('Copy command was unsuccessful');
      }
      setCopied(true);
      onCopySuccess?.();
      timeoutRef.current = setTimeout(() => setCopied(false), 2000);
    } catch (err) {
      console.error('Failed to copy:', err);
    }
  };

  return (
    <span className={clsx(styles.codeSpan, className)} {...props}>
      <code className={styles.content}>
        {icon && <span className={styles.icon}>{icon}</span>}
        {children}
        {copyable && (
          <button
            className={clsx(styles.copyButton, copied && styles.copied)}
            onClick={handleCopy}
            title={copied ? 'Copied!' : 'Copy'}
            type="button"
            aria-label="Copy code"
          >
            {copied ? <Check size={16} /> : <Copy size={16} />}
          </button>
        )}
      </code>
    </span>
  );
}
