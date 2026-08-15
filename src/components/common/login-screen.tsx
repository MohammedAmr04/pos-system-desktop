"use client"

import { useCallback, useState } from "react"
import { useTranslations } from "next-intl"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Lock, LogIn } from "lucide-react"
import { useAuth } from "@/features/auth/auth-context"

interface LoginScreenProps {
  description?: string
  onSuccess?: () => void
}

export function LoginScreen({ description, onSuccess }: LoginScreenProps) {
  const t = useTranslations("Auth")
  const { login } = useAuth()
  const [username, setUsername] = useState("")
  const [password, setPassword] = useState("")
  const [error, setError] = useState("")
  const [loading, setLoading] = useState(false)

  const handleSubmit = useCallback(async () => {
    const value = username.trim()
    if (!value || !password || loading) return
    setLoading(true)
    setError("")
    try {
      await login(value, password)
      setUsername("")
      setPassword("")
      onSuccess?.()
    } catch {
      setError(t("invalidCredentials"))
      setPassword("")
    } finally {
      setLoading(false)
    }
  }, [username, password, loading, login, onSuccess, t])

  return (
    <div className="flex h-screen items-center justify-center bg-muted/30 p-8">
      <div className="w-full max-w-md space-y-8 text-center">
        <div className="space-y-2">
          <Lock className="mx-auto h-14 w-14 text-primary" />
          <h1 className="text-2xl font-bold">{t("title")}</h1>
          <p className="text-sm text-muted-foreground">{description || t("description")}</p>
        </div>

        <div className="space-y-4">
          <Input
            className="h-14 text-center text-lg"
            placeholder={t("usernamePlaceholder")}
            value={username}
            onChange={(e) => {
              setUsername(e.target.value)
              setError("")
            }}
            autoFocus
          />
          <Input
            type="password"
            maxLength={64}
            className="h-14 text-center text-lg tracking-widest"
            placeholder={t("passwordPlaceholder")}
            value={password}
            onChange={(e) => {
              setPassword(e.target.value)
              setError("")
            }}
            onKeyDown={(e) => {
              if (e.key === "Enter") handleSubmit()
            }}
          />
          {error && <p className="text-sm text-destructive font-medium">{error}</p>}
          <Button className="w-full h-12 text-lg" onClick={handleSubmit} disabled={loading || !username.trim() || !password}>
            <LogIn className="mr-2 h-5 w-5" />
            {loading ? t("loggingIn") : t("login")}
          </Button>
        </div>
      </div>
    </div>
  )
}
