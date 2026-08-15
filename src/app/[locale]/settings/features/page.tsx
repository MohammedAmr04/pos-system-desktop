"use client"

import { useCallback, useEffect, useState } from "react"
import { useTranslations } from "next-intl"
import { FeaturesClient } from "./features-client"
import { api, TenantFeature } from "@/lib/api"
import { useAuth } from "@/features/auth/auth-context"
import { PERMISSIONS } from "@/lib/constants"
import { AccessDenied } from "@/components/common/access-denied"

export default function SettingsFeaturesPage() {
  const t = useTranslations("Features")
  const { hasPermission } = useAuth()
  const canView = hasPermission(PERMISSIONS.SETTINGS_VIEW)
  const [features, setFeatures] = useState<TenantFeature[]>([])
  const [loading, setLoading] = useState(true)
  const [requestId, setRequestId] = useState(0)

  useEffect(() => {
    if (!canView) return
    let cancelled = false
    api.tenant
      .features()
      .then((res) => {
        if (!cancelled) setFeatures(res.features)
      })
      .catch(() => {
        if (!cancelled) setFeatures([])
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [canView, requestId])

  const refresh = useCallback(() => {
    setRequestId((id) => id + 1)
  }, [])

  if (!canView) {
    return <AccessDenied />
  }

  return (
    <div className="flex-1 space-y-4 pt-6">
      <div className="flex items-center justify-between space-y-2">
        <h2 className="text-3xl font-bold tracking-tight">{t("title")}</h2>
      </div>
      <FeaturesClient
        features={features}
        loading={loading}
        onRefresh={refresh}
      />
    </div>
  )
}
