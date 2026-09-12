import { clearStoredSession, getStoredToken } from "@/lib/auth-storage"

const API_BASE = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:3001'

export const AUTH_EXPIRED_EVENT = 'pos:auth-expired'

export class ApiError extends Error {
  readonly status: number

  constructor(status: number, message: string) {
    super(message)
    this.name = 'ApiError'
    this.status = status
  }
}

export async function request<T>(path: string, options?: RequestInit): Promise<T> {
  const token = getStoredToken()
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
    ...((options?.headers as Record<string, string>) ?? {}),
  }
  const res = await fetch(`${API_BASE}${path}`, { ...options, headers })
  const bodyText = await res.text()

  if (!res.ok) {
    let message = bodyText || `HTTP ${res.status}`
    try {
      const parsed = JSON.parse(bodyText) as { message?: string; error?: string }
      message = parsed.message ?? parsed.error ?? message
    } catch {
      // keep raw text as message
    }
    if (res.status === 401 && typeof window !== 'undefined') {
      clearStoredSession()
      window.dispatchEvent(new Event(AUTH_EXPIRED_EVENT))
    }
    throw new ApiError(res.status, message)
  }

  if (!bodyText) return null as T
  try {
    return JSON.parse(bodyText) as T
  } catch {
    throw new ApiError(res.status, `Unexpected non-JSON response (HTTP ${res.status})`)
  }
}

export function toQuery(params: Record<string, string | number | boolean | undefined | null>): string {
  const search = new URLSearchParams()
  for (const [key, value] of Object.entries(params)) {
    if (value === undefined || value === null || value === '') continue
    search.set(key, String(value))
  }
  const qs = search.toString()
  return qs ? `?${qs}` : ''
}
