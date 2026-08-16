import { SiTraefikproxy } from '@icons-pack/react-simple-icons';
import { Boxes, ScanFace } from 'lucide-react';

import type { SidecarKind } from '@/api/types';

interface SidecarIconProps {
  kind: SidecarKind;
  size?: number;
}

export function SidecarIcon({ kind, size = 28 }: SidecarIconProps) {
  switch (kind) {
    case 'Traefik':
      return <SiTraefikproxy size={size} />;
    case 'Whoami':
      return <ScanFace size={size} />;
    default:
      return <Boxes size={size} />;
  }
}
