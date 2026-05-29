import { useSetBreadcrumbs } from '@/hooks/useSetBreadcrumbs'
import { ConfigurationPageLayout } from '@/components/layout/ConfigurationPageLayout'
import { AboutPage } from './AboutPage'

export function SettingsPage() {
  useSetBreadcrumbs([{ label: 'Settings' }])

  const menuItems = [{ id: 'about', label: 'About', content: <AboutPage /> }]

  return (
    <ConfigurationPageLayout
      mainHeader={<h1>Settings</h1>}
      configHeader={<h1>Settings</h1>}
      menuItems={menuItems}
      isConfigOpen
      hideCloseButton
      hideConfigButton
    />
  )
}
