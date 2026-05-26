import styles from './ProjectAvatar.module.css'

interface ProjectAvatarProps {
  name: string
  description?: string
  showText?: boolean
}

function getColorFromName(name: string): string {
  let hash = 0
  for (let i = 0; i < name.length; i++) {
    hash = name.charCodeAt(i) + ((hash << 5) - hash)
  }
  const hue = Math.abs(hash) % 360
  return `hsl(${hue}, 70%, 50%)`
}

export function ProjectAvatar({ name, description, showText }: ProjectAvatarProps) {
  return (
    <div className={styles.projectIdentity}>
      <div
        className={styles.projectAvatar}
        style={{
          backgroundColor: getColorFromName(name),
        }}
      >
        {name.charAt(0).toUpperCase()}
      </div>
      {showText && (
        <div className={styles.projectInfo}>
          <div className={styles.projectName}>{name}</div>
          {description && (
            <div className={styles.projectDescription}>{description}</div>
          )}
        </div>
      )}
    </div>
  )
}