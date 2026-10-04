export type ActionRisk = 'Safe' | 'RequireConfirmation';
export type ActionShell = 'Bash' | 'Sh';
export type ActionKind = 'exec' | 'http';

export interface ExecActionConfig {
  $type: 'exec';
  command: string[];
  workingDir?: string | null;
  user?: string | null;
  shell?: ActionShell | null;
}

export interface HttpActionConfig {
  $type: 'http';
  method: string;
  url: string;
  headers: Record<string, string>;
  body?: string | null;
  successStatusCodes?: number[] | null;
}

export type ActionConfig = ExecActionConfig | HttpActionConfig;

export interface CustomActionDto {
  id: string;
  serviceId: string;
  actionName: string;
  alias: string;
  actionDescription: string;
  /** Kebab-case lucide icon name, e.g. "rotate-cw". */
  icon: string;
  config: ActionConfig;
  requiredPermissions: string[];
  risk: ActionRisk;
  /** .NET TimeSpan string, e.g. "00:00:30". */
  timeout: string;
}

export interface CreateCustomActionInput {
  actionName: string;
  alias: string;
  actionDescription: string;
  icon: string;
  config: ActionConfig;
  requiredPermissions: string[];
  risk: ActionRisk;
  timeout: string;
}

export type UpdateCustomActionInput = Partial<CreateCustomActionInput>;
