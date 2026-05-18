import { useTranslation } from 'react-i18next'
import { GitCredentialDto } from '@/api/types'
import { ProviderIcon, ProviderBadge } from './ProviderIcon'
import styles from './GitCredentialCard.module.css'

interface GitCredentialCardProps {
  credential: GitCredentialDto
}

const PROVIDER_COLORS: Record<string, string> = {
  GitHub: '#24292e',
  GitLab: '#fc6d26',
  Bitbucket: '#0052cc',
  Gitea: '#609926',
  Generic: '#6366f1',
}

export function GitCredentialCard({ credential }: GitCredentialCardProps) {
  const { t } = useTranslation('gitCredentials')

  const providerColor = PROVIDER_COLORS[credential.providerType] || PROVIDER_COLORS.Generic

  const formatDate = (date: string) => {
    return new Date(date).toLocaleDateString(undefined, {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    })
  }

  const getAuthMethodLabel = () => {
    return credential.authMethod === 'Token' ? t('auth.token') : t('auth.ssh')
  }

  const getHostUrlDisplay = () => {
    if (!credential.hostUrl) {
      return t('card.cloudHosted')
    }
    return t('card.selfHosted', { url: credential.hostUrl })
  }

  return (
    <div className={styles.card}>
      <div className={styles.cardHeader}>
        <div
          className={styles.iconContainer}
          style={{ backgroundColor: `${providerColor}15` }}
        >
          <ProviderIcon provider={credential.providerType} size={32} color={providerColor} />
        </div>

        <div className={styles.headerContent}>
          <h3 className={styles.displayName}>{credential.displayName}</h3>

          <div className={styles.badges}>
            <span className={styles.authBadge}>{getAuthMethodLabel()}</span>
          </div>

          <p className={styles.hostUrl}>{getHostUrlDisplay()}</p>
        </div>
      </div>

      <div className={styles.cardFooter}>
        <div>
          <span className={`${styles.statusBadge} ${credential.isActive ? styles.active : styles.inactive}`}>
            <span className={styles.statusDot} />
            {credential.isActive ? t('card.active') : t('card.inactive')}
          </span>
        </div>

        <div className={styles.dateInfo}>
          <span>{t('card.lastValidated', { date: formatDate(credential.lastValidatedAt) })}</span>
        </div>
      </div>
    </div>
  )
}
