import { tokenStorage } from '@/lib/tokenStorage';

const BASE = '/api';

export type Params = Record<string, string | number | boolean | null | undefined>;

function isApiResponse(
  body: unknown
): body is { success: boolean; data?: unknown; message?: string } {
  return typeof body === 'object' && body !== null && 'success' in body;
}

function isPagedResult(
  body: unknown
): body is { items: unknown[]; totalCount: number; pageNumber: number; pageSize: number } {
  return typeof body === 'object' && body !== null && 'items' in body && 'totalCount' in body;
}

let refreshPromise: Promise<boolean> | null = null;

async function tryRefresh(): Promise<boolean> {
  if (refreshPromise) return refreshPromise;

  refreshPromise = (async () => {
    const refreshToken = tokenStorage.getRefreshToken();
    if (!refreshToken) return false;

    try {
      const res = await fetch(`${BASE}/auth/refresh`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ token: refreshToken }),
      });
      if (!res.ok) return false;
      const body = await res.json();
      if (isApiResponse(body) && body.success && body.data) {
        const { accessToken, refreshToken: newRefreshToken } = body.data as {
          accessToken: string;
          refreshToken: string;
        };
        tokenStorage.setTokens(accessToken, newRefreshToken);
        return true;
      }
      return false;
    } catch {
      return false;
    }
  })().finally(() => {
    refreshPromise = null;
  });

  return refreshPromise;
}

async function request<T>(
  path: string,
  init?: RequestInit,
  params?: object,
  isRetry = false
): Promise<T> {
  const url = new URL(BASE + path, window.location.origin);

  if (params) {
    Object.entries(params).forEach(([k, v]) => {
      if (v != null) url.searchParams.set(k, String(v));
    });
  }

  const headers: Record<string, string> = {};
  if (init?.body !== undefined) {
    headers['Content-Type'] = 'application/json';
  }

  const accessToken = tokenStorage.getAccessToken();
  if (accessToken) {
    headers['Authorization'] = `Bearer ${accessToken}`;
  }

  const res = await fetch(url.toString(), {
    ...init,
    headers: { ...headers, ...((init?.headers as Record<string, string>) ?? {}) },
  });

  if (res.redirected && new URL(res.url).pathname.startsWith('/setup')) {
    window.location.href = '/setup';
    return new Promise(() => {});
  }

  if (res.status === 401 && !isRetry) {
    const refreshed = await tryRefresh();
    if (refreshed) {
      return request<T>(path, init, params, true);
    }
    tokenStorage.clear();
    window.location.href = '/login';
    return new Promise(() => {});
  }

  if (res.status === 204 || res.headers.get('content-length') === '0') {
    return null as T;
  }

  const body = await res.json();

  if (!res.ok) {
    const error = new Error(
      isApiResponse(body) ? body.message : `Request failed with status ${res.status}`
    );
    Object.assign(error, body);
    throw error;
  }

  if (isApiResponse(body)) {
    if (!body.success) {
      const error = new Error(body.message ?? `Request failed with status ${res.status}`);
      Object.assign(error, body);
      throw error;
    }
    return body.data as T;
  }

  if (isPagedResult(body)) {
    return body as T;
  }

  return body as T;
}

export const apiClient = {
  get: <T>(path: string, params?: object) => request<T>(path, { method: 'GET' }, params),
  post: <T>(path: string, body: unknown) =>
    request<T>(path, {
      method: 'POST',
      body: JSON.stringify(body ?? {}),
    }),
  put: <T>(path: string, body: unknown) =>
    request<T>(path, {
      method: 'PUT',
      body: JSON.stringify(body ?? {}),
    }),
  patch: <T>(path: string, body: unknown) =>
    request<T>(path, {
      method: 'PATCH',
      body: JSON.stringify(body ?? {}),
    }),
  delete: <T = void>(path: string) => request<T>(path, { method: 'DELETE' }),
};
