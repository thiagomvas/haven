import '@xterm/xterm/css/xterm.css';

import { FitAddon } from '@xterm/addon-fit';
import { Terminal } from '@xterm/xterm';
import { Square, TerminalSquare } from 'lucide-react';
import { useContext, useEffect, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';

import { Row, Stack } from '@/components/layout';
import { Button } from '@/components/ui/Button';
import { Card } from '@/components/ui/Card';
import { ErrorAlert } from '@/components/ui/ErrorAlert';
import { Label } from '@/components/ui/Label';
import { SelectInput } from '@/components/ui/SelectInput';
import { ThemeContext } from '@/context/ThemeContext';
import { ShellType, useContainerShell } from '@/lib/signalr/useContainerShell';
import styles from '@/styles/components/services/ShellTab.module.css';

interface ShellTabProps {
  projectId: string;
  environmentId: string;
  serviceId: string;
}

const SHELL_OPTIONS = [
  { value: 'Bash', label: 'bash' },
  { value: 'Sh', label: 'sh' },
];

function readXtermTheme() {
  const style = getComputedStyle(document.documentElement);
  const v = (name: string) => style.getPropertyValue(name).trim();

  return {
    background: v('--color-surface'),
    foreground: v('--color-text-primary'),
    cursor: v('--color-primary'),
    cursorAccent: v('--color-surface'),
    selectionBackground: v('--color-surface-active'),
    black: v('--color-gray-900'),
    red: v('--color-danger'),
    green: v('--color-success'),
    yellow: v('--color-warning'),
    blue: v('--color-deploying'),
    magenta: v('--color-deployment-pending'),
    cyan: v('--color-teal-400'),
    white: v('--color-gray-200'),
    brightBlack: v('--color-gray-600'),
    brightRed: v('--color-danger-light'),
    brightGreen: v('--color-success-light'),
    brightYellow: v('--color-warning-light'),
    brightBlue: v('--color-deploying'),
    brightMagenta: v('--color-deployment-pending'),
    brightCyan: v('--color-teal-200'),
    brightWhite: v('--color-text-primary'),
  };
}

function ShellTerminal({
  projectId,
  environmentId,
  serviceId,
  shellType,
  onExit,
}: ShellTabProps & { shellType: ShellType; onExit: () => void }) {
  const { t } = useTranslation('services');
  const containerRef = useRef<HTMLDivElement>(null);
  const termRef = useRef<Terminal | null>(null);
  const fitAddonRef = useRef<FitAddon | null>(null);
  const { theme } = useContext(ThemeContext)!;

  const { status, error, sendInput } = useContainerShell({
    projectId,
    environmentId,
    serviceId,
    shellType,
    enabled: true,
    onOutput: data => termRef.current?.write(data),
  });

  // Terminal is created once; SignalR I/O and theme wiring reach it via refs so this effect
  // doesn't need to recreate the terminal (which would drop scrollback) on every status/theme change.
  useEffect(() => {
    if (!containerRef.current) return;

    const term = new Terminal({
      fontFamily: "'Monaco', 'Menlo', 'Ubuntu Mono', monospace",
      fontSize: 13,
      cursorBlink: true,
      theme: readXtermTheme(),
    });
    const fitAddon = new FitAddon();
    term.loadAddon(fitAddon);
    term.open(containerRef.current);
    fitAddon.fit();

    termRef.current = term;
    fitAddonRef.current = fitAddon;

    const dataDisposable = term.onData(input => sendInput(new TextEncoder().encode(input)));

    const resizeObserver = new ResizeObserver(() => fitAddon.fit());
    resizeObserver.observe(containerRef.current);

    return () => {
      dataDisposable.dispose();
      resizeObserver.disconnect();
      term.dispose();
      termRef.current = null;
      fitAddonRef.current = null;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  useEffect(() => {
    if (termRef.current) termRef.current.options.theme = readXtermTheme();
  }, [theme]);

  useEffect(() => {
    if (status === 'connected') termRef.current?.focus();
  }, [status]);

  return (
    <Stack gap="3" className={styles.terminalPane}>
      <Row gap="3" align="center" justify="space-between">
        <Row gap="2" align="center">
          <Label variant="secondary" size="sm">
            {status === 'connecting' && t('shell.connecting')}
            {status === 'connected' &&
              t('shell.connected', { shell: shellType === 'Bash' ? 'bash' : 'sh' })}
            {status === 'closed' && t('shell.closed')}
            {status === 'error' && t('shell.connectError')}
          </Label>
        </Row>
        <Button variant="secondary" size="sm" icon={<Square size={14} />} onClick={onExit}>
          {t('shell.closeButton')}
        </Button>
      </Row>

      {error && <ErrorAlert message={error} />}

      <div ref={containerRef} className={styles.terminal} />
    </Stack>
  );
}

export function ShellTab({ projectId, environmentId, serviceId }: ShellTabProps) {
  const { t } = useTranslation('services');
  const [session, setSession] = useState<{ key: number; shellType: ShellType } | null>(null);
  const [pendingShellType, setPendingShellType] = useState<ShellType>('Bash');

  if (!session) {
    return (
      <Card padding="var(--space-6)">
        <Stack gap="4" align="center" className={styles.startPane}>
          <TerminalSquare size={28} className={styles.startIcon} />
          <Label variant="secondary">{t('shell.intro')}</Label>
          <Row gap="3" align="center">
            <SelectInput
              options={SHELL_OPTIONS}
              value={pendingShellType}
              onChange={value => setPendingShellType(value as ShellType)}
            />
            <Button
              variant="primary"
              icon={<TerminalSquare size={16} />}
              onClick={() => setSession({ key: Date.now(), shellType: pendingShellType })}
            >
              {t('shell.startButton')}
            </Button>
          </Row>
        </Stack>
      </Card>
    );
  }

  return (
    <Card padding="var(--space-4)" className={styles.card}>
      <ShellTerminal
        // Remounts the terminal (fresh xterm instance + hub session) per "Start shell" click.
        key={session.key}
        projectId={projectId}
        environmentId={environmentId}
        serviceId={serviceId}
        shellType={session.shellType}
        onExit={() => setSession(null)}
      />
    </Card>
  );
}
