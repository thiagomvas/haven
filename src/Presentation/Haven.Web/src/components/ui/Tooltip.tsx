import { ReactNode, useEffect, useRef, useState } from 'react';
import { createPortal } from 'react-dom';

import styles from '@/styles/components/ui/Tooltip.module.css';

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
    const margin = 8;

    let top: number;
    let left: number;

    switch (direction) {
      case 'left':
        top = rect.top + rect.height / 2;
        left = rect.left - tooltipWidth - 12;
        break;
      case 'above':
        top = rect.top - tooltipHeight - 12;
        left = rect.left + rect.width / 2;
        break;
      case 'below':
        top = rect.bottom + 12;
        left = rect.left + rect.width / 2;
        break;
      case 'right':
      default:
        top = rect.top + rect.height / 2;
        left = rect.right + 12;
    }

    // Clamp to the viewport so tooltips near an edge don't render off-screen.
    // 'above'/'below' are horizontally centered via translateX(-50%); 'left'/'right'
    // are vertically centered via translateY(-50%) — account for that when clamping.
    const isHorizontallyCentered = direction === 'above' || direction === 'below';
    const isVerticallyCentered = direction === 'left' || direction === 'right';

    const minLeft = isHorizontallyCentered ? margin + tooltipWidth / 2 : margin;
    const maxLeft = isHorizontallyCentered
      ? window.innerWidth - margin - tooltipWidth / 2
      : window.innerWidth - margin - tooltipWidth;
    left = Math.min(Math.max(left, minLeft), maxLeft);

    const minTop = isVerticallyCentered ? margin + tooltipHeight / 2 : margin;
    const maxTop = isVerticallyCentered
      ? window.innerHeight - margin - tooltipHeight / 2
      : window.innerHeight - margin - tooltipHeight;
    top = Math.min(Math.max(top, minTop), maxTop);

    setPosition({ top, left });
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
