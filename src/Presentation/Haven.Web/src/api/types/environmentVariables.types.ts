export interface EnvironmentVariableDto {
  key: string;
  value: string;
  scope: string;
}

export interface TemplateSuggestionDto {
  namespace: string;
  key: string;
  description?: string | null;
}

export interface TemplateSuggestionsDto {
  namespaces: string[];
  variables: TemplateSuggestionDto[];
  filters: string[];
  /** When false, narrower scopes may define more keys, so an unknown key is not necessarily invalid. */
  isExhaustive: boolean;
}
