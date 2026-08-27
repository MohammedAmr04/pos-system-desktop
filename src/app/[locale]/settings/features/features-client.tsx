"use client"

import { useState } from "react"
import { useQueryClient } from "@tanstack/react-query"
import { useTranslations } from "next-intl"
import { toast } from "sonner"
import { Loader2, Save } from "lucide-react"
import { TenantFeature } from "@/types/domain/domain.types"
import { setTenantFeatures } from "@/actions/tenant.actions"
import { adminKeys, useTenantFeatures } from "@/hooks/use-admin"
import { useAuth } from "@/components/common/auth-context"
import { PERMISSIONS, FEATURE_LABELS } from "@/lib/constants"
import { Button } from "@/components/ui/button"
import { Switch } from "@/components/ui/switch"
import { Card, CardContent } from "@/components/ui/card"

export function FeaturesClient() {
  const t = useTranslations("Features")
  const queryClient = useQueryClient()
  const { hasPermission, refreshAccess } = useAuth()
  const canUpdate = hasPermission(PERMISSIONS.SETTINGS_UPDATE)
  const [draft, setDraft] = useState<Record<string, boolean> | null>(null)
  const [saving, setSaving] = useState(false)

  const { data, isPending: loading } = useTenantFeatures()
  const features: TenantFeature[] = data?.features ?? []

  const current = draft ?? Object.fromEntries(features.map((f) => [f.key, f.enabled]))

  const toggle = (key: string, enabled: boolean) => {
    setDraft({ ...current, [key]: enabled })
  }

  const handleSave = async () => {
    if (!draft) return
    setSaving(true)
    try {
      await setTenantFeatures(
        Object.entries(draft).map(([key, enabled]) => ({ key, enabled }))
      )
      toast.success(t("saved"))
      setDraft(null)
      await refreshAccess()
      await queryClient.invalidateQueries({ queryKey: adminKeys.all() })
    } catch (e) {
      toast.error((e as Error).message || t("saveFailed"))
    } finally {
      setSaving(false)
    }
  }

  if (loading) {
    return (
      <div className="flex justify-center py-12">
        <Loader2 className="h-8 w-8 animate-spin" />
      </div>
    )
  }

  return (
    <>
      <div className="grid gap-4 md:grid-cols-2">
        {features.map((feature) => {
          const label = FEATURE_LABELS[feature.key]
          return (
            <Card key={feature.key}>
              <CardContent className="flex items-start justify-between gap-4 p-4">
                <div className="min-w-0">
                  <h3 className="text-sm font-semibold">{label?.name ?? feature.key}</h3>
                  {label?.description && (
                    <p className="mt-1 text-xs text-muted-foreground">{label.description}</p>
                  )}
                </div>
                <Switch
                  checked={current[feature.key] ?? false}
                  onCheckedChange={(checked) => toggle(feature.key, checked)}
                  disabled={!canUpdate}
                />
              </CardContent>
            </Card>
          )
        })}
      </div>

      {canUpdate && draft && (
        <div className="flex justify-end">
          <Button onClick={handleSave} disabled={saving}>
            {saving ? <Loader2 className="h-4 w-4 animate-spin" /> : <Save className="h-4 w-4" />}
            {t("save")}
          </Button>
        </div>
      )}
    </>
  )
}
