"use client"

import { useCallback, useMemo, useState } from "react"
import { useTranslations } from "next-intl"
import { useApiError } from "@/lib/api-error"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Lock, Save } from "lucide-react"
import { changePassword } from "@/actions/auth.actions"

interface PasswordChangeScreenProps {
  onSuccess: () => void
}

function validatePassword(password: string): string | null {
  if (password.length < 8) {
    return "passwordMinLength"
  }
  if (!/[a-z]/.test(password)) {
    return "passwordLowercase"
  }
  if (!/[A-Z]/.test(password)) {
    return "passwordUppercase"
  }
  if (!/[0-9]/.test(password)) {
    return "passwordDigit"
  }
  if (!/[^a-zA-Z0-9]/.test(password)) {
    return "passwordSymbol"
  }
  return null
}

export function PasswordChangeScreen({ onSuccess }: PasswordChangeScreenProps) {
  const t = useTranslations("Auth")
  const resolveError = useApiError()
  const [currentPassword, setCurrentPassword] = useState("")
  const [newPassword, setNewPassword] = useState("")
  const [confirmPassword, setConfirmPassword] = useState("")
  const [error, setError] = useState("")
  const [loading, setLoading] = useState(false)

  const validationError = useMemo(() => {
    if (!currentPassword) {
      return "currentPasswordRequired"
    }
    if (!newPassword) {
      return "newPasswordRequired"
    }
    const passwordError = validatePassword(newPassword)
    if (passwordError) {
      return passwordError
    }
    if (newPassword !== confirmPassword) {
      return "passwordsDoNotMatch"
    }
    return null
  }, [currentPassword, newPassword, confirmPassword])

  const canSubmit = Boolean(currentPassword && newPassword && confirmPassword) && !validationError

  const handleSubmit = useCallback(async () => {
    if (loading || !canSubmit) {
      return
    }
    if (validationError) {
      setError(t(validationError))
      return
    }
    setLoading(true)
    setError("")
    try {
      await changePassword(currentPassword, newPassword)
      setCurrentPassword("")
      setNewPassword("")
      setConfirmPassword("")
      onSuccess()
    } catch (e) {
      setError(resolveError(e) || t("changePasswordFailed"))
    } finally {
      setLoading(false)
    }
  }, [loading, canSubmit, validationError, currentPassword, newPassword, onSuccess, t, resolveError])

  return (
    <div className="flex h-screen items-center justify-center bg-muted/30 p-8">
      <div className="w-full max-w-md space-y-8 text-center">
        <div className="space-y-2">
          <Lock className="mx-auto h-14 w-14 text-primary" />
          <h1 className="text-2xl font-bold">{t("changePasswordTitle")}</h1>
          <p className="text-sm text-muted-foreground">{t("changePasswordDescription")}</p>
        </div>

        <div className="space-y-4">
          <Input
            type="password"
            className="h-14 text-center text-lg tracking-widest"
            placeholder={t("currentPasswordPlaceholder")}
            value={currentPassword}
            onChange={(e) => {
              setCurrentPassword(e.target.value)
              setError("")
            }}
            autoFocus
          />
          <Input
            type="password"
            className="h-14 text-center text-lg tracking-widest"
            placeholder={t("newPasswordPlaceholder")}
            value={newPassword}
            onChange={(e) => {
              setNewPassword(e.target.value)
              setError("")
            }}
          />
          <Input
            type="password"
            className="h-14 text-center text-lg tracking-widest"
            placeholder={t("confirmPasswordPlaceholder")}
            value={confirmPassword}
            onChange={(e) => {
              setConfirmPassword(e.target.value)
              setError("")
            }}
            onKeyDown={(e) => {
              if (e.key === "Enter") {
                handleSubmit()
              }
            }}
          />
          {error && <p className="text-sm text-destructive font-medium">{error}</p>}
          <Button
            className="w-full h-12 text-lg"
            onClick={handleSubmit}
            disabled={loading || !canSubmit}
          >
            <Save className="mr-2 h-5 w-5" />
            {loading ? t("saving") : t("changePassword")}
          </Button>
        </div>
      </div>
    </div>
  )
}
