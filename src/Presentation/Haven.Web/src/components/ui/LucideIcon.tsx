import { Zap } from 'lucide-react';
import { DynamicIcon, IconName, iconNames } from 'lucide-react/dynamic';

interface LucideIconProps {
  /** Kebab-case lucide icon name, e.g. "rotate-cw". */
  name: string;
  size?: number;
  className?: string;
}

const KNOWN = new Set<string>(iconNames);

export function isLucideIconName(name: string): name is IconName {
  return KNOWN.has(name);
}

export function searchLucideIcons(query: string, limit = 24): IconName[] {
  const q = query.trim().toLowerCase();
  if (!q) return iconNames.slice(0, limit);
  const matches = iconNames.filter(n => n.includes(q));
  matches.sort((a, b) => Number(b.startsWith(q)) - Number(a.startsWith(q)) || a.length - b.length);
  return matches.slice(0, limit);
}

/** Renders a lucide icon by name, loading it on demand; falls back to a generic icon if unknown. */
export function LucideIcon({ name, size = 16, className }: LucideIconProps) {
  if (!isLucideIconName(name)) return <Zap size={size} className={className} />;
  return (
    <DynamicIcon
      name={name}
      size={size}
      className={className}
      fallback={() => <span style={{ display: 'inline-block', width: size, height: size }} />}
    />
  );
}
