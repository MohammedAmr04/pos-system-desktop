"use client"

import { useAuth } from "@/features/auth/auth-context"
import { LoginScreen } from "@/components/common/login-screen"

export function AuthGate({ children }: { children: React.ReactNode }) {
  const { isReady, isAuthenticated } = useAuth()

  if (!isReady) {
    return (
      <div className="flex h-screen items-center justify-center bg-background">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" />
      </div>
    )
  }

  if (!isAuthenticated) {
    return <LoginScreen />
  }

  return <>{children}</>
}
