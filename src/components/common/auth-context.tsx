"use client"

import { createContext, useCallback, useContext, useEffect, useMemo, useState } from "react"
import { AUTH_EXPIRED_EVENT } from "@/lib/api"
import { login as loginAction } from "@/actions/auth.actions"
import { getMe } from "@/api/auth"
import {
  AuthSession,
  loadStoredSession,
  saveSession,
  clearStoredSession,
  getStoredToken,
} from "@/lib/auth-storage"

interface AuthContextValue {
  session: AuthSession | null
  isReady: boolean
  isAuthenticated: boolean
  login: (username: string, password: string) => Promise<AuthSession>
  logout: () => void
  refreshAccess: () => Promise<AuthSession | null>
  hasPermission: (permissionKey: string) => boolean
  hasFeature: (featureKey: string) => boolean
  hasAccess: (permissionKey: string, featureKey?: string) => boolean
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [initial] = useState(() => loadStoredSession())
  const [session, setSession] = useState<AuthSession | null>(initial)
  const [isReady, setIsReady] = useState(initial === null)

  useEffect(() => {
    const handleExpired = () => setSession(null)
    window.addEventListener(AUTH_EXPIRED_EVENT, handleExpired)
    return () => window.removeEventListener(AUTH_EXPIRED_EVENT, handleExpired)
  }, [])

  useEffect(() => {
    if (!initial) return

    let cancelled = false
    // Re-validate the stored token against the backend; drop it when invalid.
    getMe()
      .then((bundle) => {
        if (cancelled) return
        const fresh: AuthSession = {
          token: initial.token,
          user: bundle.user,
          roles: bundle.roles,
          permissions: bundle.permissions,
          features: bundle.features,
        }
        setSession(fresh)
        saveSession(fresh)
      })
      .catch(() => {
        if (cancelled) return
        clearStoredSession()
        setSession(null)
      })
      .finally(() => {
        if (!cancelled) setIsReady(true)
      })

    return () => {
      cancelled = true
    }
  }, [initial])

  const login = useCallback(async (username: string, password: string) => {
    const res = await loginAction(username, password)
    const next: AuthSession = {
      token: res.token,
      user: res.access.user,
      roles: res.access.roles,
      permissions: res.access.permissions,
      features: res.access.features,
    }
    setSession(next)
    saveSession(next)
    return next
  }, [])

  const logout = useCallback(() => {
    clearStoredSession()
    setSession(null)
  }, [])

  const refreshAccess = useCallback(async () => {
    const token = getStoredToken()
    if (!token) return null
    try {
      const bundle = await getMe()
      const fresh: AuthSession = {
        token,
        user: bundle.user,
        roles: bundle.roles,
        permissions: bundle.permissions,
        features: bundle.features,
      }
      setSession(fresh)
      saveSession(fresh)
      return fresh
    } catch {
      return null
    }
  }, [])

  const hasPermission = useCallback(
    (permissionKey: string) => session?.permissions?.includes(permissionKey) ?? false,
    [session]
  )

  const hasFeature = useCallback(
    (featureKey: string) => session?.features?.includes(featureKey) ?? false,
    [session]
  )

  const hasAccess = useCallback(
    (permissionKey: string, featureKey?: string) => {
      if (!hasPermission(permissionKey)) return false
      if (featureKey && !hasFeature(featureKey)) return false
      return true
    },
    [hasPermission, hasFeature]
  )

  const value = useMemo<AuthContextValue>(
    () => ({
      session,
      isReady,
      isAuthenticated: session !== null,
      login,
      logout,
      refreshAccess,
      hasPermission,
      hasFeature,
      hasAccess,
    }),
    [session, isReady, login, logout, refreshAccess, hasPermission, hasFeature, hasAccess]
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error("useAuth must be used within an AuthProvider")
  return ctx
}
