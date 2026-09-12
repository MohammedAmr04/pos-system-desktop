"use client"

import { useTranslations } from "next-intl"
import { BrandsClient } from "./brands-client"
import { useAuth } from "@/components/common/auth-context"
import { PERMISSIONS, FEATURES } from "@/lib/constants"
import { AccessDenied } from "@/components/common/access-denied"

export default function BrandsPage() {
  const t = useTranslations("Brands")
  const { hasAccess } = useAuth()
  const canView = hasAccess(PERMISSIONS.BRANDS_VIEW, FEATURES.BRANDS)

  if (!canView) {
    return <AccessDenied />
  }

  return (
    <div className="flex-1 space-y-4 pt-6">
      <div className="flex items-center justify-between space-y-2">
        <h2 className="text-3xl font-bold tracking-tight">{t("title")}</h2>
      </div>
      <BrandsClient />
    </div>
  )
}
