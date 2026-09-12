"use client"

import { useTranslations } from "next-intl"
import { CategoriesClient } from "./categories-client"
import { useAuth } from "@/components/common/auth-context"
import { PERMISSIONS, FEATURES } from "@/lib/constants"
import { AccessDenied } from "@/components/common/access-denied"

export default function CategoriesPage() {
  const t = useTranslations("Categories")
  const { hasAccess } = useAuth()
  const canView = hasAccess(PERMISSIONS.CATEGORIES_VIEW, FEATURES.CATEGORIES)

  if (!canView) {
    return <AccessDenied />
  }

  return (
    <div className="flex-1 space-y-4 pt-6">
      <div className="flex items-center justify-between space-y-2">
        <h2 className="text-3xl font-bold tracking-tight">{t("title")}</h2>
      </div>
      <CategoriesClient />
    </div>
  )
}
