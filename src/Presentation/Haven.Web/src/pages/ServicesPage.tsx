import { useTranslation } from 'react-i18next'

export function ServicesPage() {
  const { t } = useTranslation('services')

  return (
    <div>
      <h1>{t('title')}</h1>
      <p>{t('comingSoon')}</p>
    </div>
  )
}
