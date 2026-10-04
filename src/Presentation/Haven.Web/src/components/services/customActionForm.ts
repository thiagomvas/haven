import {
  ActionConfig,
  ActionKind,
  ActionRisk,
  ActionShell,
  CreateCustomActionInput,
  CustomActionDto,
} from '@/api/types';
import { secondsToTimeSpan, timeSpanToSeconds } from '@/lib/timespan';

export interface CustomActionFormState {
  actionName: string;
  alias: string;
  actionDescription: string;
  icon: string;
  risk: ActionRisk;
  timeoutSeconds: string;
  requiredPermissions: string;
  kind: ActionKind;
  execCommand: string[];
  execWorkingDir: string;
  execUser: string;
  execShell: ActionShell | '';
  httpMethod: string;
  httpUrl: string;
  httpHeaders: string;
  httpBody: string;
  httpSuccessCodes: string;
}

export const EMPTY_FORM: CustomActionFormState = {
  actionName: '',
  alias: '',
  actionDescription: '',
  icon: 'zap',
  risk: 'Safe',
  timeoutSeconds: '30',
  requiredPermissions: '',
  kind: 'exec',
  execCommand: [''],
  execWorkingDir: '',
  execUser: '',
  execShell: '',
  httpMethod: 'POST',
  httpUrl: '',
  httpHeaders: '',
  httpBody: '',
  httpSuccessCodes: '',
};

const parseHeaders = (text: string): Record<string, string> =>
  Object.fromEntries(
    text
      .split('\n')
      .map(line => line.trim())
      .filter(Boolean)
      .map(line => {
        const idx = line.indexOf(':');
        return idx < 0 ? [line, ''] : [line.slice(0, idx).trim(), line.slice(idx + 1).trim()];
      })
      .filter(([key]) => key)
  );

const formatHeaders = (headers: Record<string, string>): string =>
  Object.entries(headers)
    .map(([k, v]) => `${k}: ${v}`)
    .join('\n');

const splitList = (text: string): string[] =>
  text
    .split(',')
    .map(s => s.trim())
    .filter(Boolean);

export function actionToForm(action: CustomActionDto): CustomActionFormState {
  const base: CustomActionFormState = {
    ...EMPTY_FORM,
    actionName: action.actionName,
    alias: action.alias,
    actionDescription: action.actionDescription,
    icon: action.icon,
    risk: action.risk,
    timeoutSeconds: String(timeSpanToSeconds(action.timeout)),
    requiredPermissions: action.requiredPermissions.join(', '),
    kind: action.config.$type,
  };

  if (action.config.$type === 'exec') {
    return {
      ...base,
      execCommand: action.config.command.length ? action.config.command : [''],
      execWorkingDir: action.config.workingDir ?? '',
      execUser: action.config.user ?? '',
      execShell: action.config.shell ?? '',
    };
  }

  return {
    ...base,
    httpMethod: action.config.method,
    httpUrl: action.config.url,
    httpHeaders: formatHeaders(action.config.headers ?? {}),
    httpBody: action.config.body ?? '',
    httpSuccessCodes: (action.config.successStatusCodes ?? []).join(', '),
  };
}

export function formToConfig(form: CustomActionFormState): ActionConfig {
  if (form.kind === 'exec') {
    return {
      $type: 'exec',
      command: form.execCommand.map(a => a.trim()).filter(Boolean),
      workingDir: form.execWorkingDir.trim() || null,
      user: form.execUser.trim() || null,
      shell: form.execShell || null,
    };
  }

  const codes = splitList(form.httpSuccessCodes)
    .map(c => parseInt(c, 10))
    .filter(n => !isNaN(n));

  return {
    $type: 'http',
    method: form.httpMethod,
    url: form.httpUrl.trim(),
    headers: parseHeaders(form.httpHeaders),
    body: form.httpBody || null,
    successStatusCodes: codes.length ? codes : null,
  };
}

export function formToInput(form: CustomActionFormState): CreateCustomActionInput {
  return {
    actionName: form.actionName.trim(),
    alias: form.alias.trim(),
    actionDescription: form.actionDescription.trim(),
    icon: form.icon.trim(),
    config: formToConfig(form),
    requiredPermissions: splitList(form.requiredPermissions),
    risk: form.risk,
    timeout: secondsToTimeSpan(parseInt(form.timeoutSeconds, 10) || 30),
  };
}

export function isFormValid(form: CustomActionFormState): boolean {
  if (!form.actionName.trim() || !form.alias.trim() || !form.icon.trim()) return false;
  if (!(parseInt(form.timeoutSeconds, 10) > 0)) return false;
  if (form.kind === 'exec') return form.execCommand.some(a => a.trim());
  return /^https?:\/\/\S+$/i.test(form.httpUrl.trim());
}
