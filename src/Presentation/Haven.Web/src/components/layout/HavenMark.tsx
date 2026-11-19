interface HavenMarkProps {
  className?: string;
}

/**
 * The Haven mark: an "h" with the three service states (running, degraded,
 * fault) in a row beside the stem. The h follows `currentColor`; the state
 * colors are fixed to the on-dark palette used by the shell.
 */
export function HavenMark({ className }: HavenMarkProps) {
  return (
    <svg className={className} viewBox="0 0 100 100" aria-hidden="true">
      <rect x="22" y="14" width="13" height="72" fill="currentColor" />
      <path
        d="M28.5 58a21.5 21.5 0 0 1 43 0v28"
        fill="none"
        stroke="currentColor"
        strokeWidth="13"
      />
      <circle cx="47" cy="19" r="5" fill="#3fb37a" />
      <path d="M62 12.5 68.5 19 62 25.5 55.5 19z" fill="#e0a030" />
      <path d="M75 13 81 24.5h-12z" fill="#e0574b" />
    </svg>
  );
}
