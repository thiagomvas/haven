export interface JobInfoDto {
  name: string;
  key: string;
  nextRunTime?: string | null;
  lastRunTime?: string | null;
}
