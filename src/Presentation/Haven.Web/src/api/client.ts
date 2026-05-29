const BASE = '/api'

type Params = Record<string, string | number | boolean | null | undefined>

function isApiResponse(body: unknown): body is { success: boolean; data?: unknown; message?: string } {
  return typeof body === 'object' && body !== null && 'success' in body
}

function isPagedResult(body: unknown): body is { items: unknown[]; totalCount: number; pageNumber: number; pageSize: number } {
  return typeof body === 'object' && body !== null && 'items' in body && 'totalCount' in body
}

async function request<T>(
  path: string,
  init?: RequestInit,
  params?: Params,
): Promise<T> {
  const url = new URL(BASE + path, window.location.origin)

  if (params) {
    Object.entries(params).forEach(([k, v]) => {
      if (v != null) url.searchParams.set(k, String(v))
    })
  }

  const headers: HeadersInit = {}
  if (init?.body !== undefined) {
    headers['Content-Type'] = 'application/json'
  }

  const res = await fetch(url.toString(), {
    headers,
    ...init,
  })

  if (res.redirected && new URL(res.url).pathname.startsWith('/setup')) {
    window.location.href = '/setup'
    return new Promise(() => {})
  }

  if (res.status === 204 || res.headers.get('content-length') === '0') {
    return null as T
  }

  const body = await res.json()

  if (!res.ok) {
    const error = new Error(
      isApiResponse(body) ? body.message : `Request failed with status ${res.status}`
    )
    Object.assign(error, body)
    throw error
  }

  if (isApiResponse(body)) {
    if (!body.success) {
      const error = new Error(body.message ?? `Request failed with status ${res.status}`)
      Object.assign(error, body)
      throw error
    }
    return body.data as T
  }

  if (isPagedResult(body)) {
    return body as T
  }

  return body as T
}

export const apiClient = {
  get: <T>(path: string, params?: Params) =>
    request<T>(path, { method: 'GET' }, params),
  post: <T>(path: string, body: unknown) =>
    request<T>(path, {
      method: 'POST',
      body: JSON.stringify(body ?? {}),
    }),
  patch: <T>(path: string, body: unknown) =>
    request<T>(path, {
      method: 'PATCH',
      body: JSON.stringify(body ?? {}),
    }),
  delete: <T = void>(path: string) =>
    request<T>(path, { method: 'DELETE' }),
}
