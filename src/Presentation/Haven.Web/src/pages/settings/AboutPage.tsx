import { useTranslation } from 'react-i18next'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/Card'
import { Chip } from '@/components/ui/Chip'
import { Label } from '@/components/ui/Label'
import { Row } from '@/components/layout'
import styles from './AboutPage.module.css'
import { Button } from '@/components/ui/Button'
import { Code, ScrollText } from 'lucide-react'

export function AboutPage() {
  const { t } = useTranslation('settings')

  return (
    <Card padding="var(--space-2)">
      <CardHeader>
        <CardTitle>
          <Row>
            {t('about.title')}
            <Chip content={t('about.version', { version: 'v0.1.0-alpha' })} />
          </Row>
        </CardTitle>
        <Label variant="muted" className={styles.description}>
          {t('about.description')}
        </Label>
      </CardHeader>
      <CardContent>

      <Row>
        <Button icon={<ScrollText />} variant="text" href="https://github.com/thiagomvas/haven" target="_blank" rel="noreferrer">
          {t('about.releaseNotes')}
        </Button>
        <Button icon={<Code />} variant="text" href="https://github.com/thiagomvas/haven" target="_blank" rel="noreferrer">
          {t('about.repository')}
        </Button>
      </Row>
      </CardContent>
    </Card>
  )
}
