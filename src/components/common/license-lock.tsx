"use client"

import { useEffect, useState, useCallback } from "react"
import { checkLicense, unlockLicense, LicenseStatus } from "@/features/license/actions"
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
import { ShieldAlert, Clock, Lock } from "lucide-react"

export function LicenseGate({ children }: { children: React.ReactNode }) {
  const t = useTranslations("License")
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
    const result = await unlockLicense(code.trim())
    if (result.success) {
      setLicense({ status: "ok" })
      setError("")
      setCode("")
    } else {
      setError(t("invalidCode"))
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

  if (license?.status === "locked") {
    return (
      <div className="flex h-screen items-center justify-center bg-muted/30 p-8">
        <div className="max-w-md w-full space-y-8 text-center">
          <Lock className="mx-auto h-16 w-16 text-amber-500" />
          <h1 className="text-2xl font-bold">{t("trialExpired")}</h1>
          <p className="text-muted-foreground text-sm">{t("trialDescription")}</p>

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
