/* Search */
export interface FuzzySearchResult {
  entityType: string;
  id: string;
  label: string;
  similarity: number;
  metadata?: Record<string, string>;
}

/* Response Wrappers */
export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  message?: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

/* Users */
export interface UserDto {
  id: string;
  name: string;
  email: string;
  isAdmin: boolean;
  requirePasswordChange: boolean;
}

export interface CreateUserInput {
  email: string;
  isAdmin: boolean;
  permissions: string[];
}
