export type NotificationScope = 'Global' | 'Project' | 'Environment' | 'Service';
export interface NotificationRuleContext {
  scope: NotificationScope;
  scopeId: string;
}
export interface NotificationRuleSummaryItemDto {
  name: string;
  i18NKey: string;
  ruleCount: number;
  isOverridden: boolean;
  globalRuleCount: number;
}
export interface NotificationRuleEventConfigDto {
  eventType: string;
  channelIds: string[];
}
export interface SetNotificationRulesInput {
  channelIds: string[];
}
/* Notification Channels */

export type NotificationChannel = 'Webhook' | 'Discord' | 'Ntfy' | 'Smtp';
export interface WebhookNotificationConfig {
  url: string;
  headers: Record<string, string>;
}

export interface DiscordNotificationConfig {
  webhookUrl: string;
  embed: boolean;
}

export interface NtfyNotificationConfig {
  host: string;
  queue: string;
  enableSSL: boolean;
}

export interface SmtpNotificationConfig {
  host: string;
  port: number;
  username: string;
  password: string;
  fromEmail: string;
  fromName: string;
  enableSsl: boolean;
  toEmails: string[];
}
export interface NotificationChannelConfigDto {
  id: string;
  name: string;
  channel: NotificationChannel;
  config: string;
  enabled: boolean;
  isSystemDefault: boolean;
  rulesCount: number;
}
export interface CreateNotificationChannelConfigInput {
  name: string;
  channel: NotificationChannel;
  configJson: string;
  enabled: boolean;
}
export interface UpdateNotificationChannelConfigInput {
  name: string;
  configJson: string;
  enabled: boolean;
}
export interface GetNotificationChannelConfigsParams {
  pageNumber?: number;
  pageSize?: number;
}
export type NotificationDeliveryStatus = 'Pending' | 'Delivered' | 'Failed';
export interface NotificationAttemptDto {
  id: string;
  channelConfigId: string;
  channelConfigName: string;
  channel: NotificationChannel;
  eventType: string;
  status: NotificationDeliveryStatus;
  errorMessage: string | null;
  attemptedAt: string | null;
  eventPayload: string;
  payload: string | null;
  response: string | null;
}
export interface GetNotificationAttemptsParams {
  channelConfigId?: string;
  pageNumber?: number;
  pageSize?: number;
}
