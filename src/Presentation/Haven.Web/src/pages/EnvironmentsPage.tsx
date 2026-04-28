import { useTranslation } from 'react-i18next'

export function EnvironmentsPage() {
  const { t } = useTranslation('environments')

  return (
    <div>
      <h1>{t('title')}</h1>
      <p>{t('comingSoon')}</p>
    </div>
  )
}
