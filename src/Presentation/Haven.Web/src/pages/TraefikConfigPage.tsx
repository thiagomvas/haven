import {
  AlertTriangle,
  ChevronDown,
  ChevronUp,
  Gauge,
  KeyRound,
  LayoutDashboard,
  Lock,
  Plus,
  ScrollText,
  ShieldCheck,
  SlidersHorizontal,
  Terminal,
  Trash2,
  Unlock,
} from 'lucide-react';
import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate, useParams } from 'react-router-dom';

import { RestartPolicy, SidecarDto } from '@/api/types';
import { Row, Stack } from '@/components/layout';
import { CommandArgsEditor } from '@/components/services/CommandArgsEditor';
import { DashboardDomainEditor } from '@/components/sidecars/DashboardDomainEditor';
import { Badge } from '@/components/ui/Badge';
import { Banner } from '@/components/ui/Banner';
import { Button } from '@/components/ui/Button';
import { Checkbox } from '@/components/ui/Checkbox';
import { CodeBlock } from '@/components/ui/CodeBlock';
import { ErrorAlert } from '@/components/ui/ErrorAlert';
import { HealthIndicator } from '@/components/ui/HealthIndicator';
import { Input } from '@/components/ui/Input';
import { SelectInput } from '@/components/ui/SelectInput';
import { Spinner } from '@/components/ui/Spinner';
import { Tabs } from '@/components/ui/Tabs';
import { usePermission } from '@/hooks/usePermission';
import { useSetBreadcrumbs } from '@/hooks/useSetBreadcrumbs';
import {
  useSidecars,
  useTraefikDashboardAuth,
  useUpdateSidecar,
  useUpdateTraefikDashboardAuth,
} from '@/hooks/useSidecars';
import { useUrlState } from '@/hooks/useUrlState';
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

const RESTART_POLICY_OPTIONS: { value: RestartPolicy; label: string }[] = [
  { value: 'No', label: 'No' },
  { value: 'Always', label: 'Always' },
  { value: 'UnlessStopped', label: 'Unless Stopped' },
  { value: 'OnFailure', label: 'On Failure' },
];

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
  next = enabled
    ? setArgValue(next, ACME_EMAIL_PREFIX, email)
    : next.filter(a => !a.startsWith(ACME_EMAIL_PREFIX));
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
  const [activeTab, setActiveTab] = useUrlState('tab', 'general');

  const initialImage = sidecar.image ?? 'traefik:v3.7';
  const initialPorts = sidecar.ports.length > 0 ? sidecar.ports : DEFAULT_PORTS;
  const initialCommandArgs =
    sidecar.commandArgs.length > 0 ? sidecar.commandArgs : DEFAULT_COMMAND_ARGS;
  const initialRestartPolicy = sidecar.restartPolicy ?? 'Always';

  const [image, setImage] = useState(initialImage);
  const [ports, setPorts] = useState<string[]>(initialPorts);
  const [commandArgs, setCommandArgs] = useState<string[]>(initialCommandArgs);
  const [restartPolicy, setRestartPolicy] = useState<RestartPolicy>(initialRestartPolicy);
  const [acmeEmail, setAcmeEmail] = useState(() => getArgValue(commandArgs, ACME_EMAIL_PREFIX));
  const [showRawArgs, setShowRawArgs] = useState(false);
  const [saveError, setSaveError] = useState<string | undefined>(undefined);
  const [saved, setSaved] = useState(false);

  const sslEnabled = isSslEnabled(commandArgs);

  const isDirty =
    image !== initialImage ||
    restartPolicy !== initialRestartPolicy ||
    JSON.stringify(ports) !== JSON.stringify(initialPorts) ||
    JSON.stringify(commandArgs) !== JSON.stringify(initialCommandArgs);

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
        <Row gap="2" align="center" wrap>
          <h1 className={styles.title}>{t('traefikConfig.title')}</h1>
          <HealthIndicator health={sidecar.status} useTooltip />
          {isDirty && <Badge variant="warning">{t('traefikConfig.unsavedChanges')}</Badge>}
        </Row>
        <p className={styles.subtitle}>{t('traefikConfig.subtitle')}</p>
      </div>

      <Tabs
        activeTab={activeTab}
        onChange={setActiveTab}
        items={[
          {
            id: 'general',
            label: t('traefikConfig.quickSetup'),
            icon: <SlidersHorizontal size={15} />,
            content: (
              <Stack gap="3">
                <p className={styles.sectionHelp}>{t('traefikConfig.quickSetupHelp')}</p>
                <div className={styles.settingsList}>
                  <div className={styles.settingRow}>
                    <Checkbox
                      label={t('traefikConfig.dashboard')}
                      description={t('traefikConfig.dashboardHelp')}
                      icon={<LayoutDashboard size={16} className={styles.settingIcon} />}
                      disabled={!canManage}
                      checked={commandArgs.includes(FLAG_DASHBOARD)}
                      onChange={e => {
                        setCommandArgs(a => toggleFlag(a, FLAG_DASHBOARD, e.target.checked));
                        setPorts(p => togglePort(p, DASHBOARD_PORT, e.target.checked));
                      }}
                    />
                  </div>
                  <div className={styles.settingRow}>
                    <Checkbox
                      label={t('traefikConfig.accessLog')}
                      description={t('traefikConfig.accessLogHelp')}
                      icon={<ScrollText size={16} className={styles.settingIcon} />}
                      disabled={!canManage}
                      checked={commandArgs.includes(FLAG_ACCESSLOG)}
                      onChange={e =>
                        setCommandArgs(a => toggleFlag(a, FLAG_ACCESSLOG, e.target.checked))
                      }
                    />
                  </div>
                  <div className={styles.settingRow}>
                    <Checkbox
                      label={t('traefikConfig.exposedByDefault')}
                      description={t('traefikConfig.exposedByDefaultHelp')}
                      icon={<ShieldCheck size={16} className={styles.settingIcon} />}
                      disabled={!canManage}
                      checked={isExposedByDefault(commandArgs)}
                      onChange={e => setCommandArgs(a => setExposedByDefault(a, e.target.checked))}
                    />
                  </div>
                  <div className={styles.settingRow}>
                    <Checkbox
                      label={t('traefikConfig.metrics')}
                      description={t('traefikConfig.metricsHelp')}
                      icon={<Gauge size={16} className={styles.settingIcon} />}
                      disabled={!canManage}
                      checked={commandArgs.includes(FLAG_METRICS)}
                      onChange={e =>
                        setCommandArgs(a => toggleFlag(a, FLAG_METRICS, e.target.checked))
                      }
                    />
                  </div>
                  <div className={styles.settingRowLast}>
                    <Row align="center" gap="1" className={styles.sectionHelp}>
                      <span>{t('traefikConfig.dashboardAccessHint')}</span>
                    </Row>
                  </div>
                </div>
              </Stack>
            ),
          },
          {
            id: 'access',
            label: t('traefikConfig.access'),
            icon: <Lock size={15} />,
            content: <AccessTab sidecar={sidecar} canManage={canManage} />,
          },
          {
            id: 'ssl',
            label: t('traefikConfig.ssl'),
            icon: <ShieldCheck size={15} />,
            content: (
              <Stack gap="4">
                <p className={styles.sectionHelp}>{t('traefikConfig.sslHelp')}</p>
                <div className={styles.settingsList}>
                  <div className={sslEnabled ? styles.settingRow : styles.settingRowLast}>
                    <Checkbox
                      label={t('traefikConfig.sslEnable')}
                      description={t('traefikConfig.sslEnableHelp')}
                      disabled={!canManage}
                      checked={sslEnabled}
                      onChange={e => {
                        const enabled = e.target.checked;
                        setCommandArgs(a => setSslEnabled(a, ports, enabled, acmeEmail));
                        setPorts(p => togglePort(p, HTTPS_PORT, enabled));
                        if (!enabled)
                          setCommandArgs(a => a.filter(f => f !== FLAG_ACME_STAGING_CASERVER));
                      }}
                    />
                  </div>
                  {sslEnabled && (
                    <div className={styles.settingRowLast}>
                      <Stack gap="4">
                        <Banner
                          variant="info"
                          description={t('traefikConfig.sslReachabilityNote')}
                        />
                        <Input
                          id="traefik-acme-email"
                          label={t('traefikConfig.sslEmail')}
                          type="email"
                          value={acmeEmail}
                          disabled={!canManage}
                          placeholder="you@example.com"
                          onChange={e => {
                            const email = e.target.value;
                            setAcmeEmail(email);
                            setCommandArgs(a => setArgValue(a, ACME_EMAIL_PREFIX, email));
                          }}
                        />
                        <Checkbox
                          label={t('traefikConfig.sslStaging')}
                          description={t('traefikConfig.sslStagingHelp')}
                          disabled={!canManage}
                          checked={commandArgs.includes(FLAG_ACME_STAGING_CASERVER)}
                          onChange={e =>
                            setCommandArgs(a =>
                              toggleFlag(a, FLAG_ACME_STAGING_CASERVER, e.target.checked)
                            )
                          }
                        />
                      </Stack>
                    </div>
                  )}
                </div>
              </Stack>
            ),
          },
          {
            id: 'advanced',
            label: t('traefikConfig.advanced'),
            icon: <Terminal size={15} />,
            content: (
              <Stack gap="5">
                <p className={styles.sectionHelp}>{t('traefikConfig.advancedHelp')}</p>

                <Stack gap="2">
                  <Checkbox
                    label={t('traefikConfig.apiInsecure')}
                    description={t('traefikConfig.apiInsecureHelp')}
                    icon={<Unlock size={16} className={styles.settingIcon} />}
                    disabled={!canManage}
                    checked={commandArgs.includes(FLAG_API_INSECURE)}
                    onChange={e =>
                      setCommandArgs(a => toggleFlag(a, FLAG_API_INSECURE, e.target.checked))
                    }
                  />
                  {commandArgs.includes(FLAG_API_INSECURE) && (
                    <Banner variant="warning" description={t('traefikConfig.apiInsecureWarning')} />
                  )}
                </Stack>

                <Stack gap="2">
                  <Input
                    id="traefik-image"
                    label={t('traefikConfig.image')}
                    type="text"
                    value={image}
                    disabled={!canManage}
                    onChange={e => setImage(e.target.value)}
                  />
                  {image !== initialImage && (
                    <Banner variant="warning" description={t('traefikConfig.imageChangeWarning')} />
                  )}
                </Stack>

                <Stack gap="2">
                  <div className={styles.labelWithHelp}>
                    <span className={styles.fieldLabel}>{t('traefikConfig.ports')}</span>
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
                          aria-label={t('traefikConfig.removePort')}
                        >
                          <Trash2 size={14} />
                        </button>
                      </div>
                    ))}
                  </Stack>
                  <Button
                    variant="secondary"
                    size="sm"
                    icon={<Plus size={14} />}
                    disabled={!canManage}
                    onClick={() => setPorts(p => [...p, ''])}
                    className={styles.addPortButton}
                  >
                    {t('traefikConfig.addPort')}
                  </Button>
                </Stack>

                <SelectInput
                  label={t('traefikConfig.restartPolicy')}
                  options={RESTART_POLICY_OPTIONS}
                  value={restartPolicy}
                  disabled={!canManage}
                  onChange={value => setRestartPolicy(value as RestartPolicy)}
                />

                <Stack gap="2">
                  <button
                    type="button"
                    className={styles.disclosureToggle}
                    onClick={() => setShowRawArgs(v => !v)}
                  >
                    {showRawArgs ? <ChevronUp size={15} /> : <ChevronDown size={15} />}
                    <span>{t('traefikConfig.rawArgsToggle')}</span>
                  </button>
                  {!showRawArgs && (
                    <CodeBlock
                      className={styles.previewBlock}
                      icon={<Terminal size={13} />}
                      header={t('traefikConfig.commandPreview')}
                      copyable
                      code={commandArgs.join(' \\\n  ')}
                    />
                  )}
                  {showRawArgs && (
                    <>
                      <p className={styles.sectionHelp}>{t('traefikConfig.rawArgsHelp')}</p>
                      <CommandArgsEditor
                        commandArgs={commandArgs}
                        onChange={setCommandArgs}
                        disabled={!canManage}
                      />
                    </>
                  )}
                </Stack>
              </Stack>
            ),
          },
        ]}
      />

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
          disabled={!canManage || !isDirty}
        >
          {t('traefikConfig.save')}
        </Button>
      </div>
    </Stack>
  );
}

function AccessTab({ sidecar, canManage }: { sidecar: SidecarDto; canManage: boolean }) {
  const { t } = useTranslation('sidecars');
  const { data: auth, isLoading } = useTraefikDashboardAuth();
  const updateAuth = useUpdateTraefikDashboardAuth();

  const [authEnabled, setAuthEnabled] = useState(false);
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [authError, setAuthError] = useState<string | undefined>(undefined);
  const [authSaved, setAuthSaved] = useState(false);
  const [initialized, setInitialized] = useState(false);

  if (auth && !initialized) {
    setAuthEnabled(auth.enabled);
    setUsername(auth.username ?? '');
    setInitialized(true);
  }

  const handleSaveAuth = async () => {
    setAuthError(undefined);
    setAuthSaved(false);
    try {
      await updateAuth.mutateAsync({ enabled: authEnabled, username, password });
      setPassword('');
      setAuthSaved(true);
    } catch (err) {
      setAuthError(err instanceof Error ? err.message : t('traefikConfig.saveError'));
    }
  };

  return (
    <Stack gap="5">
      <p className={styles.sectionHelp}>{t('traefikConfig.accessHelp')}</p>

      <Stack gap="3">
        <Row gap="2" align="center">
          <KeyRound size={15} />
          <span className={styles.fieldLabel}>{t('traefikConfig.basicAuth')}</span>
        </Row>
        <div className={styles.settingsList}>
          <div className={authEnabled ? styles.settingRow : styles.settingRowLast}>
            <Checkbox
              label={t('traefikConfig.basicAuthEnable')}
              description={t('traefikConfig.basicAuthEnableHelp')}
              disabled={!canManage || isLoading}
              checked={authEnabled}
              onChange={e => setAuthEnabled(e.target.checked)}
            />
          </div>
          {authEnabled && (
            <div className={styles.settingRowLast}>
              <Stack gap="3">
                <Input
                  id="traefik-auth-username"
                  label={t('traefikConfig.basicAuthUsername')}
                  value={username}
                  disabled={!canManage}
                  onChange={e => setUsername(e.target.value)}
                />
                <Input
                  id="traefik-auth-password"
                  label={t('traefikConfig.basicAuthPassword')}
                  type="password"
                  value={password}
                  disabled={!canManage}
                  placeholder={
                    auth?.enabled
                      ? t('traefikConfig.basicAuthPasswordKeepCurrent')
                      : t('traefikConfig.basicAuthPasswordPlaceholder')
                  }
                  onChange={e => setPassword(e.target.value)}
                />
              </Stack>
            </div>
          )}
        </div>
        {authError && <ErrorAlert message={authError} variant="block" />}
        {authSaved && <p className={styles.savedNote}>{t('traefikConfig.savedNote')}</p>}
        <Row justify="flex-end">
          <Button
            variant="primary"
            size="sm"
            onClick={handleSaveAuth}
            isLoading={updateAuth.isPending}
            disabled={!canManage || (authEnabled && !username.trim())}
          >
            {t('traefikConfig.save')}
          </Button>
        </Row>
      </Stack>

      <Stack gap="3">
        <DashboardDomainEditor sidecarId={sidecar.id} disabled={!canManage} />
      </Stack>
    </Stack>
  );
}
