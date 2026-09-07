import { RefObject, useLayoutEffect, useState } from 'react';

export interface FloatingPosition {
  top: number;
  left: number;
  width: number;
  maxHeight: number;
}

const GAP = 4;
const MIN_HEIGHT = 100;
const PREFERRED_MAX_HEIGHT = 240;

/**
 * Tracks the viewport-relative position for a dropdown/popover anchored to `triggerRef`,
 * flipping above the trigger when there isn't enough room below. Meant to back a
 * `position: fixed` portal so the dropdown escapes any scrollable/clipping ancestor
 * (e.g. a Modal's scrollable body).
 */
export function useFloatingPosition(
  open: boolean,
  triggerRef: RefObject<HTMLElement | null>
): FloatingPosition | null {
  const [position, setPosition] = useState<FloatingPosition | null>(null);

  useLayoutEffect(() => {
    if (!open || !triggerRef.current) {
      setPosition(null);
      return;
    }

    const update = () => {
      const trigger = triggerRef.current;
      if (!trigger) return;

      const rect = trigger.getBoundingClientRect();
      const spaceBelow = window.innerHeight - rect.bottom - GAP;
      const spaceAbove = rect.top - GAP;

      if (spaceBelow >= MIN_HEIGHT || spaceBelow >= spaceAbove) {
        setPosition({
          top: rect.bottom + GAP,
          left: rect.left,
          width: rect.width,
          maxHeight: Math.max(Math.min(PREFERRED_MAX_HEIGHT, spaceBelow), MIN_HEIGHT),
        });
      } else {
        const maxHeight = Math.max(Math.min(PREFERRED_MAX_HEIGHT, spaceAbove), MIN_HEIGHT);
        setPosition({
          top: rect.top - GAP - maxHeight,
          left: rect.left,
          width: rect.width,
          maxHeight,
        });
      }
    };

    update();
    window.addEventListener('scroll', update, true);
    window.addEventListener('resize', update);
    return () => {
      window.removeEventListener('scroll', update, true);
      window.removeEventListener('resize', update);
    };
  }, [open, triggerRef]);

  return position;
}
