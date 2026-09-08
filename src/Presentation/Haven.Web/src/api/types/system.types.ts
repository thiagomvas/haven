export interface DatabaseBuildInfoDto {
  provider: string;
  version: string;
  path: string;
}
export interface DockerEngineBuildInfoDto {
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

export interface LatestVersionDto {
  currentVersion: string;
  latestVersion: string;
  isUpdateAvailable: boolean;
  name: string | null;
  htmlUrl: string | null;
  body: string | null;
  prerelease: boolean;
  publishedAt: string | null;
}
