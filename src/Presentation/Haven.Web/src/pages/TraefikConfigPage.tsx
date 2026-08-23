import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate, useParams } from 'react-router-dom';

import { RestartPolicy, SidecarDto } from '@/api/types';
import { Stack } from '@/components/layout';
import { CommandArgsEditor } from '@/components/services/CommandArgsEditor';
import { Button } from '@/components/ui/Button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/Card';
import { Checkbox } from '@/components/ui/Checkbox';
import { ErrorAlert } from '@/components/ui/ErrorAlert';
import { FormGroup, FormLabel, FormSelect } from '@/components/ui/Form';
import { Spinner } from '@/components/ui/Spinner';
import { usePermission } from '@/hooks/usePermission';
import { useSetBreadcrumbs } from '@/hooks/useSetBreadcrumbs';
import { useSidecars, useUpdateSidecar } from '@/hooks/useSidecars';
import styles from '@/styles/pages/TraefikConfigPage.module.css';

const DEFAULT_COMMAND_ARGS = [
  '--providers.docker=true',
  '--providers.docker.exposedbydefault=false',
  '--entrypoints.web.address=:80',
];
const DEFAULT_PORTS = ['80:80'];

const FLAG_DASHBOARD = '--api.dashboard=true';
const FLAG_ACCESSLOG = '--accesslog=true';
const FLAG_METRICS = '--metrics.prometheus=true';
const FLAG_API_INSECURE = '--api.insecure=true';
const EXPOSED_BY_DEFAULT_PREFIX = '--providers.docker.exposedbydefault=';
const DASHBOARD_PORT = '8080:8080';

const FLAG_WEBSECURE_ENTRYPOINT = '--entrypoints.websecure.address=:443';
const FLAG_ACME_HTTPCHALLENGE = '--certificatesresolvers.letsencrypt.acme.httpchallenge=true';
const FLAG_ACME_HTTPCHALLENGE_ENTRYPOINT =
  '--certificatesresolvers.letsencrypt.acme.httpchallenge.entrypoint=web';
const ACME_EMAIL_PREFIX = '--certificatesresolvers.letsencrypt.acme.email=';
const FLAG_ACME_STORAGE = '--certificatesresolvers.letsencrypt.acme.storage=/letsencrypt/acme.json';
const FLAG_ACME_STAGING_CASERVER =
  '--certificatesresolvers.letsencrypt.acme.caserver=https://acme-staging-v02.api.letsencrypt.org/directory';
const HTTPS_PORT = '443:443';

function toggleFlag(args: string[], flag: string, enabled: boolean): string[] {
  if (enabled) return args.includes(flag) ? args : [...args, flag];
  return args.filter(a => a !== flag);
}

function togglePort(ports: string[], port: string, enabled: boolean): string[] {
  if (enabled) return ports.includes(port) ? ports : [...ports, port];
  return ports.filter(p => p !== port);
}

function isExposedByDefault(args: string[]): boolean {
  return args.includes(`${EXPOSED_BY_DEFAULT_PREFIX}true`);
}

function setExposedByDefault(args: string[], enabled: boolean): string[] {
  const filtered = args.filter(a => !a.startsWith(EXPOSED_BY_DEFAULT_PREFIX));
  return [...filtered, `${EXPOSED_BY_DEFAULT_PREFIX}${enabled}`];
}

function getArgValue(args: string[], prefix: string): string {
  return args.find(a => a.startsWith(prefix))?.slice(prefix.length) ?? '';
}

function setArgValue(args: string[], prefix: string, value: string): string[] {
  const filtered = args.filter(a => !a.startsWith(prefix));
  return [...filtered, `${prefix}${value}`];
}

function isSslEnabled(args: string[]): boolean {
  return args.includes(FLAG_ACME_HTTPCHALLENGE);
}

function setSslEnabled(args: string[], ports: string[], enabled: boolean, email: string): string[] {
  let next = args;
  next = toggleFlag(next, FLAG_WEBSECURE_ENTRYPOINT, enabled);
  next = toggleFlag(next, FLAG_ACME_HTTPCHALLENGE, enabled);
  next = toggleFlag(next, FLAG_ACME_HTTPCHALLENGE_ENTRYPOINT, enabled);
  next = toggleFlag(next, FLAG_ACME_STORAGE, enabled);
  next = enabled ? setArgValue(next, ACME_EMAIL_PREFIX, email) : next.filter(a => !a.startsWith(ACME_EMAIL_PREFIX));
  if (!enabled) next = next.filter(a => a !== FLAG_ACME_STAGING_CASERVER);
  return next;
}

export function TraefikConfigPage() {
  const { sidecarSlug } = useParams<{ sidecarSlug: string }>();
  const { t } = useTranslation('sidecars');
  const { data: sidecars, isLoading, isError } = useSidecars();

  const sidecar = useMemo(
    () => sidecars?.find(s => (s.alias ?? s.name) === sidecarSlug),
    [sidecars, sidecarSlug]
  );

  useSetBreadcrumbs([
    { label: t('title'), to: '/sidecars' },
    { label: sidecar?.name ?? sidecarSlug ?? '…' },
  ]);

  if (isLoading) {
    return (
      <div className={styles.spinner}>
        <Spinner />
      </div>
    );
  }

  if (isError || !sidecar) {
    return <ErrorAlert message={t('traefikConfig.notFound')} variant="block" />;
  }

  return <TraefikConfigForm sidecar={sidecar} />;
}

function TraefikConfigForm({ sidecar }: { sidecar: SidecarDto }) {
  const navigate = useNavigate();
  const { t } = useTranslation(['sidecars', 'services', 'common']);
  const canManage = usePermission('sidecars.manage');
  const updateMutation = useUpdateSidecar();

  const [image, setImage] = useState(sidecar.image ?? 'traefik:v3.7');
  const [ports, setPorts] = useState<string[]>(
    sidecar.ports.length > 0 ? sidecar.ports : DEFAULT_PORTS
  );
  const [commandArgs, setCommandArgs] = useState<string[]>(
    sidecar.commandArgs.length > 0 ? sidecar.commandArgs : DEFAULT_COMMAND_ARGS
  );
  const [restartPolicy, setRestartPolicy] = useState<RestartPolicy>(
    sidecar.restartPolicy ?? 'Always'
  );
  const [acmeEmail, setAcmeEmail] = useState(() => getArgValue(commandArgs, ACME_EMAIL_PREFIX));
  const [saveError, setSaveError] = useState<string | undefined>(undefined);
  const [saved, setSaved] = useState(false);

  const sslEnabled = isSslEnabled(commandArgs);

  const handleReset = () => {
    setPorts(DEFAULT_PORTS);
    setCommandArgs(DEFAULT_COMMAND_ARGS);
    setAcmeEmail('');
  };

  const handleSave = async () => {
    setSaveError(undefined);
    setSaved(false);
    try {
      await updateMutation.mutateAsync({
        sidecarId: sidecar.id,
        body: { dockerConfig: { image, ports, commandArgs, restartPolicy } },
      });
      setSaved(true);
    } catch (err) {
      setSaveError(err instanceof Error ? err.message : t('traefikConfig.saveError'));
    }
  };

  const handlePortChange = (index: number, value: string) => {
    setPorts(p => p.map((port, i) => (i === index ? value : port)));
  };

  const handleRemovePort = (index: number) => {
    setPorts(p => p.filter((_, i) => i !== index));
  };

  return (
    <Stack gap="5" className={styles.container}>
      <div className={styles.header}>
        <h1 className={styles.title}>{t('traefikConfig.title')}</h1>
        <p className={styles.subtitle}>{t('traefikConfig.subtitle')}</p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>{t('traefikConfig.quickSetup')}</CardTitle>
          <p className={styles.sectionHelp}>{t('traefikConfig.quickSetupHelp')}</p>
        </CardHeader>
        <CardContent>
          <Stack gap="3">
            <Checkbox
              label={t('traefikConfig.dashboard')}
              description={t('traefikConfig.dashboardHelp')}
              disabled={!canManage}
              checked={commandArgs.includes(FLAG_DASHBOARD)}
              onChange={e => {
                setCommandArgs(a => toggleFlag(a, FLAG_DASHBOARD, e.target.checked));
                setPorts(p => togglePort(p, DASHBOARD_PORT, e.target.checked));
              }}
            />
            <Checkbox
              label={t('traefikConfig.accessLog')}
              description={t('traefikConfig.accessLogHelp')}
              disabled={!canManage}
              checked={commandArgs.includes(FLAG_ACCESSLOG)}
              onChange={e => setCommandArgs(a => toggleFlag(a, FLAG_ACCESSLOG, e.target.checked))}
            />
            <Checkbox
              label={t('traefikConfig.exposedByDefault')}
              description={t('traefikConfig.exposedByDefaultHelp')}
              disabled={!canManage}
              checked={isExposedByDefault(commandArgs)}
              onChange={e => setCommandArgs(a => setExposedByDefault(a, e.target.checked))}
            />
            <Checkbox
              label={t('traefikConfig.metrics')}
              description={t('traefikConfig.metricsHelp')}
              disabled={!canManage}
              checked={commandArgs.includes(FLAG_METRICS)}
              onChange={e => setCommandArgs(a => toggleFlag(a, FLAG_METRICS, e.target.checked))}
            />
            <Checkbox
              label={t('traefikConfig.apiInsecure')}
              description={t('traefikConfig.apiInsecureHelp')}
              disabled={!canManage}
              checked={commandArgs.includes(FLAG_API_INSECURE)}
              onChange={e =>
                setCommandArgs(a => toggleFlag(a, FLAG_API_INSECURE, e.target.checked))
              }
            />
          </Stack>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>{t('traefikConfig.ssl')}</CardTitle>
          <p className={styles.sectionHelp}>{t('traefikConfig.sslHelp')}</p>
        </CardHeader>
        <CardContent>
          <Stack gap="3">
            <Checkbox
              label={t('traefikConfig.sslEnable')}
              description={t('traefikConfig.sslEnableHelp')}
              disabled={!canManage}
              checked={sslEnabled}
              onChange={e => {
                const enabled = e.target.checked;
                setCommandArgs(a => setSslEnabled(a, ports, enabled, acmeEmail));
                setPorts(p => togglePort(p, HTTPS_PORT, enabled));
                if (!enabled) setCommandArgs(a => a.filter(f => f !== FLAG_ACME_STAGING_CASERVER));
              }}
            />
            {sslEnabled && (
              <>
                <FormGroup>
                  <FormLabel htmlFor="traefik-acme-email">{t('traefikConfig.sslEmail')}</FormLabel>
                  <input
                    id="traefik-acme-email"
                    type="email"
                    className={styles.textInput}
                    value={acmeEmail}
                    disabled={!canManage}
                    placeholder="you@example.com"
                    onChange={e => {
                      const email = e.target.value;
                      setAcmeEmail(email);
                      setCommandArgs(a => setArgValue(a, ACME_EMAIL_PREFIX, email));
                    }}
                  />
                </FormGroup>
                <Checkbox
                  label={t('traefikConfig.sslStaging')}
                  description={t('traefikConfig.sslStagingHelp')}
                  disabled={!canManage}
                  checked={commandArgs.includes(FLAG_ACME_STAGING_CASERVER)}
                  onChange={e =>
                    setCommandArgs(a => toggleFlag(a, FLAG_ACME_STAGING_CASERVER, e.target.checked))
                  }
                />
              </>
            )}
          </Stack>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>{t('traefikConfig.advanced')}</CardTitle>
          <p className={styles.sectionHelp}>{t('traefikConfig.advancedHelp')}</p>
        </CardHeader>
        <CardContent>
          <Stack gap="4">
            <FormGroup>
              <FormLabel htmlFor="traefik-image">{t('traefikConfig.image')}</FormLabel>
              <input
                id="traefik-image"
                type="text"
                className={styles.textInput}
                value={image}
                disabled={!canManage}
                onChange={e => setImage(e.target.value)}
              />
            </FormGroup>

            <FormGroup>
              <div className={styles.labelWithHelp}>
                <FormLabel htmlFor="traefik-ports">{t('traefikConfig.ports')}</FormLabel>
                <span className={styles.helpText}>{t('traefikConfig.portsHelp')}</span>
              </div>
              <Stack gap="2">
                {ports.map((port, idx) => (
                  <div key={idx} className={styles.portRow}>
                    <input
                      type="text"
                      className={styles.textInput}
                      value={port}
                      disabled={!canManage}
                      placeholder="80:80"
                      onChange={e => handlePortChange(idx, e.target.value)}
                    />
                    <button
                      type="button"
                      className={styles.removeButton}
                      disabled={!canManage}
                      onClick={() => handleRemovePort(idx)}
                    >
                      ×
                    </button>
                  </div>
                ))}
              </Stack>
              <button
                type="button"
                className={styles.addButton}
                disabled={!canManage}
                onClick={() => setPorts(p => [...p, ''])}
              >
                {t('traefikConfig.addPort')}
              </button>
            </FormGroup>

            <FormGroup>
              <FormLabel htmlFor="traefik-restart-policy">
                {t('traefikConfig.restartPolicy')}
              </FormLabel>
              <FormSelect
                id="traefik-restart-policy"
                value={restartPolicy}
                disabled={!canManage}
                onChange={e => setRestartPolicy(e.target.value as RestartPolicy)}
              >
                <option value="No">No</option>
                <option value="Always">Always</option>
                <option value="UnlessStopped">Unless Stopped</option>
                <option value="OnFailure">On Failure</option>
              </FormSelect>
            </FormGroup>

            <CommandArgsEditor
              commandArgs={commandArgs}
              onChange={setCommandArgs}
              disabled={!canManage}
            />
          </Stack>
        </CardContent>
      </Card>

      {saveError && <ErrorAlert message={saveError} variant="block" />}
      {saved && <p className={styles.savedNote}>{t('traefikConfig.savedNote')}</p>}

      <div className={styles.actions}>
        <Button variant="secondary" onClick={handleReset} disabled={!canManage}>
          {t('traefikConfig.resetToDefaults')}
        </Button>
        <Button variant="ghost" onClick={() => navigate('/sidecars')}>
          {t('common:actions.cancel')}
        </Button>
        <Button
          variant="primary"
          onClick={handleSave}
          isLoading={updateMutation.isPending}
          disabled={!canManage}
        >
          {t('traefikConfig.save')}
        </Button>
      </div>
    </Stack>
  );
}
