"use client"

import { useCallback, useEffect, useState } from "react"
import { useTranslations } from "next-intl"
import { toast } from "sonner"
import { CalendarClock, Loader2, Save, ShieldCheck } from "lucide-react"
import { useAuth } from "@/components/common/auth-context"
import { AccessDenied } from "@/components/common/access-denied"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { useApiError } from "@/lib/api-error"
import { PERMISSIONS } from "@/lib/constants"
import { checkLicenseStatus, saveLicenseConfiguration } from "@/api/license"
import type { LicenseStatus } from "@/types/domain/domain.types"

type LicenseType = "trial" | "monthly" | "annual" | "permanent"

function toDateInput(value?: string | null) {
  return value ? value.slice(0, 10) : ""
}

export default function LicenseSettingsPage() {
  const t = useTranslations("LicenseSettings")
  const resolveError = useApiError()
  const { hasPermission } = useAuth()
  const canManage = hasPermission(PERMISSIONS.LICENSE_MANAGE)
  const [status, setStatus] = useState<LicenseStatus | null>(null)
  const [licenseType, setLicenseType] = useState<LicenseType>("trial")
  const [trialDays, setTrialDays] = useState("14")
  const [startedAt, setStartedAt] = useState("")
  const [saving, setSaving] = useState(false)
  const [loading, setLoading] = useState(true)

  const load = useCallback(async () => {
    try {
      const result = await checkLicenseStatus()
      setStatus(result)
      setLicenseType((result.licenseType as LicenseType) || "trial")
      setTrialDays(String(result.trialDays || 14))
      setStartedAt(toDateInput(result.licenseStartedAt) || new Date().toISOString().slice(0, 10))
    } catch (error) {
      toast.error(resolveError(error) || t("loadFailed"))
    } finally {
      setLoading(false)
    }
  }, [resolveError, t])

  useEffect(() => {
    void (async () => {
      await load()
    })()
  }, [load])

  const handleSave = async () => {
    if (!canManage || !startedAt) return
    setSaving(true)
    try {
      const result = await saveLicenseConfiguration({
        licenseType,
        trialDays: Number(trialDays),
        licenseStartedAt: startedAt,
      })
      setStatus(result)
      toast.success(t("saved"))
    } catch (error) {
      toast.error(resolveError(error) || t("saveFailed"))
    } finally {
      setSaving(false)
    }
  }

  if (!canManage) return <AccessDenied />
  if (loading) {
    return <div className="flex flex-1 items-center justify-center"><Loader2 className="h-8 w-8 animate-spin" /></div>
  }

  return (
    <div className="flex-1 space-y-6 pt-6">
      <div>
        <h2 className="text-3xl font-bold tracking-tight">{t("title")}</h2>
        <p className="mt-1 text-muted-foreground">{t("description")}</p>
      </div>

      <div className="grid gap-6 lg:grid-cols-[1fr_320px]">
        <Card>
          <CardHeader>
            <CardTitle>{t("configurationTitle")}</CardTitle>
            <CardDescription>{t("configurationDescription")}</CardDescription>
          </CardHeader>
          <CardContent className="space-y-5">
            <div className="space-y-2">
              <label className="text-sm font-medium">{t("type")}</label>
              <Select value={licenseType} onValueChange={(value) => setLicenseType(value as LicenseType)}>
                <SelectTrigger className="w-full"><SelectValue /></SelectTrigger>
                <SelectContent>
                  <SelectItem value="trial">{t("trial")}</SelectItem>
                  <SelectItem value="monthly">{t("monthly")}</SelectItem>
                  <SelectItem value="annual">{t("annual")}</SelectItem>
                  <SelectItem value="permanent">{t("permanent")}</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <label htmlFor="trial-days" className="text-sm font-medium">{t("trialDays")}</label>
              <Input id="trial-days" type="number" min={1} max={3650} value={trialDays} onChange={(event) => setTrialDays(event.target.value)} dir="ltr" />
              <p className="text-xs text-muted-foreground">{t("trialDaysHint")}</p>
            </div>

            <div className="space-y-2">
              <label htmlFor="started-at" className="text-sm font-medium">{t("startedAt")}</label>
              <Input id="started-at" type="date" value={startedAt} onChange={(event) => setStartedAt(event.target.value)} dir="ltr" />
            </div>

            <Button onClick={handleSave} disabled={saving || !startedAt}>
              {saving ? <Loader2 className="h-4 w-4 animate-spin" /> : <Save className="h-4 w-4" />}
              {t("save")}
            </Button>
          </CardContent>
        </Card>

        <Card>
          <CardHeader><CardTitle className="flex items-center gap-2"><ShieldCheck className="h-5 w-5" />{t("currentTitle")}</CardTitle></CardHeader>
          <CardContent className="space-y-4 text-sm">
            <div className="flex items-center justify-between"><span>{t("status")}</span><strong>{status?.status === "locked" ? t("locked") : t("active")}</strong></div>
            <div className="flex items-center justify-between"><span>{t("machineId")}</span><span className="max-w-[170px] truncate font-mono text-xs" dir="ltr">{status?.machineId}</span></div>
            <div className="flex items-center justify-between"><span>{t("expiresAt")}</span><span className="flex items-center gap-1" dir="ltr"><CalendarClock className="h-4 w-4" />{toDateInput(status?.licenseExpiresAt) || t("never")}</span></div>
            {status?.remainingDays !== null && status?.remainingDays !== undefined && <div className="flex items-center justify-between"><span>{t("remainingDays")}</span><strong>{status.remainingDays}</strong></div>}
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
