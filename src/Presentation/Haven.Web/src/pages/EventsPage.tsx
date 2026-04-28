import { useTranslation } from 'react-i18next'

export function EventsPage() {
  const { t } = useTranslation('events')

  return (
    <div>
      <h1>{t('title')}</h1>
      <p>{t('comingSoon')}</p>
    </div>
  )
}
