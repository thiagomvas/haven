import { useTranslation } from 'react-i18next'
import { useSetBreadcrumbs } from '@/hooks/useSetBreadcrumbs'
import { ConfigurationPageLayout } from '@/components/layout/ConfigurationPageLayout'
import { AboutPage } from './AboutPage'

export function SettingsPage() {
  const { t } = useTranslation('settings')
  useSetBreadcrumbs([{ label: t('title') }])

  const menuItems = [{ id: 'about', label: t('menu.about'), content: <AboutPage /> }]

  return (
    <ConfigurationPageLayout
      mainHeader={<h1>{t('title')}</h1>}
      configHeader={<h1>{t('title')}</h1>}
      menuItems={menuItems}
      isConfigOpen
      hideCloseButton
      hideConfigButton
    />
  )
}
