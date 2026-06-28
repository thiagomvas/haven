
export interface DatabaseBuildInfoDto {
  provider: string;
  version: string;
  path: string;
}export interface DockerEngineBuildInfoDto {
  isConnected: boolean;
  version: string | null;
}
export interface BuildInfoDto {
  version: string;
  commitSha: string;
  buildDate: string;
  buildSystem: string;
  dotNetVersion: string;
  database: DatabaseBuildInfoDto;
  dockerEngine: DockerEngineBuildInfoDto;
}

