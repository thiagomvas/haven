import { ReactNode, useEffect, useRef, useState } from 'react';
import { createPortal } from 'react-dom';

import styles from './Tooltip.module.css';

type TooltipDirection = 'left' | 'right' | 'above' | 'below';

interface TooltipProps {
  content: string;
  children: ReactNode;
  direction?: TooltipDirection;
}

export function Tooltip({ content, children, direction = 'right' }: TooltipProps) {
  const [isVisible, setIsVisible] = useState(false);
  const wrapperRef = useRef<HTMLDivElement>(null);
  const tooltipRef = useRef<HTMLDivElement>(null);
  const [position, setPosition] = useState({ top: 0, left: 0 });

  const calculatePosition = () => {
    if (!wrapperRef.current) return;

    const rect = wrapperRef.current.getBoundingClientRect();
    const tooltipWidth = tooltipRef.current?.offsetWidth || 0;
    const tooltipHeight = tooltipRef.current?.offsetHeight || 0;

    switch (direction) {
      case 'left':
        setPosition({ top: rect.top + rect.height / 2, left: rect.left - tooltipWidth - 12 });
        break;
      case 'above':
        setPosition({ top: rect.top - tooltipHeight - 12, left: rect.left + rect.width / 2 });
        break;
      case 'below':
        setPosition({ top: rect.bottom + 12, left: rect.left + rect.width / 2 });
        break;
      case 'right':
      default:
        setPosition({ top: rect.top + rect.height / 2, left: rect.right + 12 });
    }
  };

  useEffect(() => {
    if (isVisible) {
      calculatePosition();
    }
  }, [isVisible, direction]);

  const handleMouseEnter = () => {
    setIsVisible(true);
  };

  return (
    <>
      <div
        ref={wrapperRef}
        className={styles.tooltipWrapper}
        onMouseEnter={handleMouseEnter}
        onMouseLeave={() => setIsVisible(false)}
      >
        {children}
      </div>
      {isVisible &&
        createPortal(
          <div
            ref={tooltipRef}
            className={`${styles.tooltip} ${styles[`tooltip-${direction}`]}`}
            style={{
              position: 'fixed',
              top: `${position.top}px`,
              left: `${position.left}px`,
            }}
          >
            {content}
            <div className={styles.arrow} />
          </div>,
          document.body
        )}
    </>
  );
}
