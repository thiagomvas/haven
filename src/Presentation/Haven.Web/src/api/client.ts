const BASE = '/api'

type Params = Record<string, string | number | boolean | null | undefined>

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
  if (init?.body) {
    headers['Content-Type'] = 'application/json'
  }

  const res = await fetch(url.toString(), {
    headers,
    ...init,
  })

  const body = await res.json()

  if (!res.ok || !body.success) {
    const error = new Error(body.message ?? `Request failed with status ${res.status}`)
    // Attach the full response body to the error for structured error handling
    Object.assign(error, body)
    throw error
  }

  return body.data as T
}

export const apiClient = {
  get: <T>(path: string, params?: Params) =>
    request<T>(path, { method: 'GET' }, params),
  post: <T>(path: string, body: unknown) =>
    request<T>(path, {
      method: 'POST',
      body: body !== null ? JSON.stringify(body) : undefined,
    }),
  patch: <T>(path: string, body: unknown) =>
    request<T>(path, {
      method: 'PATCH',
      body: body !== null ? JSON.stringify(body) : undefined,
    }),
  delete: <T = void>(path: string) =>
    request<T>(path, { method: 'DELETE' }),
}
