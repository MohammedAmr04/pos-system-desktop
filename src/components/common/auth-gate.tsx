"use client"

import { useAuth } from "@/components/common/auth-context"
import { LoginScreen } from "@/components/common/login-screen"
import { PasswordChangeScreen } from "@/components/common/password-change-screen"

export function AuthGate({ children }: { children: React.ReactNode }) {
  const { isReady, isAuthenticated, mustChangePassword, completePasswordChange } = useAuth()

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

  if (mustChangePassword) {
    return <PasswordChangeScreen onSuccess={completePasswordChange} />
  }

  return <>{children}</>
}
