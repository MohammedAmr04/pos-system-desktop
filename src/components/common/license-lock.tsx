"use client"

import { useEffect, useState, useCallback } from "react"
import { checkLicense, unlockLicense, LicenseStatus } from "@/actions/license.actions"
import { useTranslations } from "next-intl"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { ShieldAlert, Lock } from "lucide-react"
import { useAuth } from "@/components/common/auth-context"
import { LoginScreen } from "@/components/common/login-screen"

export function LicenseGate({ children }: { children: React.ReactNode }) {
  const t = useTranslations("License")
  const { isAuthenticated } = useAuth()
  const [license, setLicense] = useState<LicenseStatus | null>(null)
  const [loading, setLoading] = useState(true)
  const [code, setCode] = useState("")
  const [error, setError] = useState("")

  useEffect(() => {
    checkLicense()
      .then(setLicense)
      .catch(() => setLicense({ status: "ok" }))
      .finally(() => setLoading(false))
  }, [])

  const handleUnlock = useCallback(async () => {
    setError("")
    const result = await unlockLicense(code.trim())
    if (result.success) {
      setLicense({ status: "ok" })
      setCode("")
    } else {
      setError(result.message?.includes("Permission") || result.message?.includes("Authentication")
        ? t("permissionDenied")
        : t("invalidCode"))
    }
  }, [code, t])

  if (loading) {
    return (
      <div className="flex h-screen items-center justify-center bg-background">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" />
      </div>
    )
  }

  if (license?.status === "tampered") {
    return (
      <div className="flex h-screen items-center justify-center bg-destructive/5 p-8">
        <div className="max-w-md text-center space-y-6">
          <ShieldAlert className="mx-auto h-16 w-16 text-destructive" />
          <h1 className="text-2xl font-bold text-destructive">{t("tamperDetected")}</h1>
          <p className="text-muted-foreground">{t("tamperDescription")}</p>
        </div>
      </div>
    )
  }

  if (license?.status === "locked" || license?.status === "first_boot") {
    // Unlock requires an authenticated user with license.manage, so ask for a
    // PIN login first when no session exists yet.
    if (!isAuthenticated) {
      return <LoginScreen description={t("loginRequired")} />
    }

    return (
      <div className="flex h-screen items-center justify-center bg-muted/30 p-8">
        <div className="max-w-md w-full space-y-8 text-center">
          <Lock className="mx-auto h-16 w-16 text-amber-500" />
          <h1 className="text-2xl font-bold">
            {license.status === "first_boot" ? t("activationRequired") : t("trialExpired")}
          </h1>
          <p className="text-muted-foreground text-sm">
            {license.status === "first_boot" ? t("activationDescription") : t("trialDescription")}
          </p>

          <Dialog open={true} onOpenChange={() => {}}>
            <DialogContent
              className="sm:max-w-[380px]"
              showCloseButton={false}
            >
              <DialogHeader>
                <DialogTitle>{t("enterCode")}</DialogTitle>
                <DialogDescription>{t("codeHint")}</DialogDescription>
              </DialogHeader>
              <div className="space-y-4 py-4">
                <Input
                  type="password"
                  inputMode="numeric"
                  maxLength={4}
                  placeholder="****"
                  className="text-center text-2xl tracking-widest h-14"
                  value={code}
                  onChange={(e) => {
                    const v = e.target.value.replace(/\D/g, "").slice(0, 4)
                    setCode(v)
                    setError("")
                  }}
                  onKeyDown={(e) => {
                    if (e.key === "Enter") handleUnlock()
                  }}
                  autoFocus
                />
                {error && (
                  <p className="text-sm text-destructive font-medium">{error}</p>
                )}
                <Button className="w-full h-12 text-lg" onClick={handleUnlock}>
                  {t("submit")}
                </Button>
              </div>
            </DialogContent>
          </Dialog>
        </div>
      </div>
    )
  }

  return <>{children}</>
}
