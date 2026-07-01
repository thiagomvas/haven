import styles from '@/styles/components/ui/SimpleUserAvatar.module.css';

interface SimpleUserAvatarProps {
  name: string;
  email?: string;
  showText?: boolean;
}

function getColorFromName(name: string): string {
  let hash = 0;
  for (let i = 0; i < name.length; i++) {
    hash = name.charCodeAt(i) + ((hash << 5) - hash);
  }
  const hue = Math.abs(hash) % 360;
  return `hsl(${hue}, 70%, 50%)`;
}

function getInitials(name: string): string {
  return name
    .split(' ')
    .map(part => part.charAt(0))
    .join('')
    .toUpperCase()
    .slice(0, 2);
}

export function SimpleUserAvatar({ name, email, showText }: SimpleUserAvatarProps) {
  return (
    <div className={styles.userIdentity}>
      <div
        className={styles.userAvatar}
        style={{
          backgroundColor: getColorFromName(name),
        }}
      >
        {getInitials(name)}
      </div>
      {showText && (
        <div className={styles.userInfo}>
          <div className={styles.userName}>{name}</div>
          {email && <div className={styles.userEmail}>{email}</div>}
        </div>
      )}
    </div>
  );
}
