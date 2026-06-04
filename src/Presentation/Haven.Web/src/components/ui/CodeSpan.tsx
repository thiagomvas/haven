import { HTMLAttributes, ReactNode, useRef, useState } from 'react'
import { clsx } from 'clsx'
import styles from './CodeSpan.module.css'
import { Check, Copy } from 'lucide-react'

interface CodeSpanProps extends HTMLAttributes<HTMLSpanElement> {
  icon?: ReactNode
  copyable?: boolean
  onCopySuccess?: () => void
}

export function CodeSpan({
  icon,
  copyable = false,
  onCopySuccess,
  className,
  children,
  ...props
}: CodeSpanProps) {
  const [copied, setCopied] = useState(false)
  const timeoutRef = useRef<NodeJS.Timeout>()
  const textContent = typeof children === 'string' ? children : ''

  const handleCopy = async () => {
    try {
      await navigator.clipboard.writeText(textContent)
      setCopied(true)
      onCopySuccess?.()
      timeoutRef.current = setTimeout(() => setCopied(false), 2000)
    } catch (err) {
      console.error('Failed to copy:', err)
    }
  }

  return (
    <span
      className={clsx(styles.codeSpan, className)}
      {...props}
    >
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
  )
}
