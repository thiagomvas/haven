import { apiClient } from './client';
import { FuzzySearchResult } from './types';

export const fuzzySearchApi = {
  search: (query: string, count = 10) =>
    apiClient.get<FuzzySearchResult[]>('/fuzzy', { query, count }),
};
