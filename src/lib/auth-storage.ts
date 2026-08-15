export interface AuthSession {
  token: string
  user: {
    id: string
    name: string
    isActive: boolean
    tenantId: string
  }
  roles: string[]
  permissions: string[]
  features: string[]
}

const STORAGE_KEY = 'pos.auth.v1'

export function loadStoredSession(): AuthSession | null {
  if (typeof window === 'undefined') return null
  try {
    const raw = window.localStorage.getItem(STORAGE_KEY)
    if (!raw) return null
    const parsed = JSON.parse(raw) as AuthSession
    if (!parsed?.token || !parsed?.user?.id) return null
    return parsed
  } catch {
    return null
  }
}

export function getStoredToken(): string | null {
  return loadStoredSession()?.token ?? null
}

export function saveSession(session: AuthSession): void {
  if (typeof window === 'undefined') return
  window.localStorage.setItem(STORAGE_KEY, JSON.stringify(session))
}

export function clearStoredSession(): void {
  if (typeof window === 'undefined') return
  window.localStorage.removeItem(STORAGE_KEY)
}
