import { useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/Card';
import { Label } from '@/components/ui/Label';
import { CodeEditor } from '@/components/ui/CodeEditor';
import { configurationManifestApi } from '@/api/instance';

export function ConfigurationManifestPage() {
  const { t } = useTranslation('settings');

  const handleLoad = useCallback(() => configurationManifestApi.get().then(v => v ?? ''), []);

  const handleSave = useCallback(async (content: string) => {
    await configurationManifestApi.apply(content);
  }, []);

  return (
    <Card>
      <CardHeader>
        <CardTitle>{t('configManifest.title')}</CardTitle>
        <Label variant="muted">{t('configManifest.description')}</Label>
      </CardHeader>
      <CardContent>
        <CodeEditor
          onLoad={handleLoad}
          onSave={handleSave}
          placeholder="YAML configuration"
          saveLabel={t('configManifest.apply')}
          savedMessage={t('configManifest.saved')}
          loadingMessage={t('configManifest.loading')}
        />
      </CardContent>
    </Card>
  );
}
