import { useTranslation } from 'react-i18next';
import { useBuildInfo } from '@/hooks/useBuildInfo';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/Card';
import { Chip } from '@/components/ui/Chip';
import { Label } from '@/components/ui/Label';
import { Row } from '@/components/layout';
import { Spinner } from '@/components/ui/Spinner';
import { KeyValueList, KeyValueRow } from '@/components/ui/KeyValueList';
import styles from './AboutPage.module.css';
import { Button } from '@/components/ui/Button';
import { Code, ScrollText } from 'lucide-react';
import { Badge } from '@/components/ui/Badge';

export function AboutPage() {
  const { t } = useTranslation('settings');
  const { t: tCommon } = useTranslation('common');
  const { data: buildInfo, isLoading: buildInfoLoading } = useBuildInfo();

  return (
    <>
      <Card padding="var(--space-2)" style={{ marginBottom: 'var(--space-2)' }}>
        <CardHeader>
          <CardTitle>
            <Row>
              {t('about.title')}
              <Chip content={t('about.version', { version: buildInfo?.version })} />
            </Row>
          </CardTitle>
          <Label variant="muted" className={styles.description}>
            {t('about.description')}
          </Label>
        </CardHeader>
        <CardContent>
          <Row>
            <Button
              icon={<ScrollText />}
              variant="text"
              href="https://github.com/thiagomvas/haven"
              target="_blank"
              rel="noreferrer"
            >
              {t('about.releaseNotes')}
            </Button>
            <Button
              icon={<Code />}
              variant="text"
              href="https://github.com/thiagomvas/haven"
              target="_blank"
              rel="noreferrer"
            >
              {t('about.repository')}
            </Button>
          </Row>
        </CardContent>
      </Card>
      <Card>
        <CardHeader>
          <CardTitle>{t('about.buildInfo')}</CardTitle>
          <Label variant="muted" className={styles.description}>
            {t('about.buildInfoDescription')}
          </Label>
        </CardHeader>
        <CardContent>
          {buildInfoLoading ? (
            <Row justify="center">
              <Spinner />
            </Row>
          ) : (
            <KeyValueList>
              <KeyValueRow label={tCommon('labels.havenBuild')}>
                {buildInfo?.version} ; {buildInfo?.buildDate || 'N/A'} ; commit{' '}
                {buildInfo?.commitSha}
              </KeyValueRow>
              <KeyValueRow label={tCommon('labels.netVersion')}>
                {buildInfo?.dotNetVersion || 'N/A'}
              </KeyValueRow>
              <KeyValueRow label={tCommon('labels.database')}>
                {buildInfo?.database.provider} {buildInfo?.database.version || 'N/A'} ;{' '}
                {buildInfo?.database.path || 'N/A'}
              </KeyValueRow>
              <KeyValueRow label={tCommon('labels.buildSystem')}>
                {buildInfo?.buildSystem || 'N/A'}
              </KeyValueRow>
              <KeyValueRow label={tCommon('labels.dockerEngine')}>
                {buildInfo?.dockerEngine?.isConnected ? buildInfo.dockerEngine.version : null}
                <Badge variant={buildInfo?.dockerEngine?.isConnected ? 'success' : 'danger'}>
                  {buildInfo?.dockerEngine?.isConnected
                    ? tCommon('statuses.connected')
                    : tCommon('statuses.disconnected')}
                </Badge>
              </KeyValueRow>
            </KeyValueList>
          )}
        </CardContent>
      </Card>
    </>
  );
}
