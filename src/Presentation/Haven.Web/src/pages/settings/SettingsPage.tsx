import { useTranslation } from 'react-i18next'
import { useSetBreadcrumbs } from '@/hooks/useSetBreadcrumbs'
import { ConfigurationPageLayout } from '@/components/layout/ConfigurationPageLayout'
import { useCurrentUser } from '@/hooks/useCurrentUser'
import { AboutPage } from './AboutPage'
import { UsersPage } from './UsersPage'
import { usePermission } from "@/hooks/usePermission";

export function SettingsPage() {
  const { t } = useTranslation('settings')
  const currentUser = useCurrentUser()
  useSetBreadcrumbs([{ label: t('title') }])

  const menuItems = [
    { id: 'about', label: t('menu.about'), content: <AboutPage /> },
    ...(usePermission("system.read_users")
      ? [{ id: 'users', label: t('menu.users'), content: <UsersPage /> }]
      : []),
  ]

  return (
    <ConfigurationPageLayout
      mainHeader={<h1>{t('title')}</h1>}
      configHeader={<h1>{t('title')}</h1>}
      menuItems={menuItems}
      isConfigOpen
      hideCloseButton
      hideConfigButton
    >{null}</ConfigurationPageLayout>
  )
}
