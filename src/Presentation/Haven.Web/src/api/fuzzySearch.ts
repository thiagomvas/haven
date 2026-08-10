import { apiClient } from './client';
import { FuzzySearchResult } from './types';

export const fuzzySearchApi = {
  search: (query: string, count = 10, scopes?: readonly string[]) =>
    apiClient.get<FuzzySearchResult[]>('/fuzzy', { query, count, scopes }),
};
