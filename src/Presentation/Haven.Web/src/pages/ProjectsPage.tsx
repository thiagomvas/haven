import { useTranslation } from 'react-i18next'

export function ProjectsPage() {
  const { t } = useTranslation('projects')

  return (
    <div>
      <h1>{t('title')}</h1>
      <p>{t('comingSoon')}</p>
    </div>
  )
}
