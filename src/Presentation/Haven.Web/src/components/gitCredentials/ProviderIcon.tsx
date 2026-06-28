import { GitProviderType } from '@/api/types/git.types';
import { SiGithub, SiGitlab, SiBitbucket, SiGitea, SiGit } from '@icons-pack/react-simple-icons';

interface ProviderIconProps {
  provider: GitProviderType;
  size?: number;
}

export function ProviderIcon({ provider, size = 24 }: ProviderIconProps) {
  switch (provider) {
    case 'GitHub':
      return <SiGithub size={size} color="var(--color-github)" />;

    case 'GitLab':
      return <SiGitlab size={size} color="var(--color-gitlab)" />;
    case 'Bitbucket':
      return <SiBitbucket size={size} color="var(--color-bitbucket)" />;

    case 'Gitea':
      return <SiGitea size={size} color="var(--color-gitea)" />;

    case 'Generic':
    default:
      return <SiGit size={size} color="var(--color-git-generic)" />;
  }
}

interface ProviderBadgeProps {
  provider: GitProviderType;
  size?: 'sm' | 'md' | 'lg';
  bgColor?: boolean;
}

export function ProviderBadge({ provider, size = 'md', bgColor = true }: ProviderBadgeProps) {
  const getProviderLabel = (p: GitProviderType): string => {
    const labels: Record<GitProviderType, string> = {
      Generic: 'Generic Git',
      GitHub: 'GitHub',
      GitLab: 'GitLab',
      Bitbucket: 'Bitbucket',
      Gitea: 'Gitea',
    };
    return labels[p];
  };

  const getProviderColor = (p: GitProviderType): string => {
    const colors: Record<GitProviderType, string> = {
      GitHub: '#24292e',
      GitLab: '#fc6d26',
      Bitbucket: '#0052cc',
      Gitea: '#609926',
      Generic: '#6366f1',
    };
    return colors[p];
  };

  const sizeClasses = {
    sm: 'px-2 py-1 text-xs',
    md: 'px-3 py-1.5 text-sm',
    lg: 'px-4 py-2 text-base',
  };

  const color = getProviderColor(provider);

  return (
    <span
      className={`inline-flex items-center gap-1.5 rounded-full font-medium ${sizeClasses[size]}`}
      style={{
        backgroundColor: bgColor ? `${color}20` : 'transparent',
        color: color,
        border: `1.5px solid ${color}40`,
      }}
    >
      <ProviderIcon provider={provider} size={size === 'sm' ? 14 : size === 'md' ? 16 : 18} />
      {getProviderLabel(provider)}
    </span>
  );
}
