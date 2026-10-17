"use client"

import { useTranslations } from "next-intl"
import { TerminalsClient } from "./terminals-client"
import { useAuth } from "@/components/common/auth-context"
import { PERMISSIONS } from "@/lib/constants"
import { AccessDenied } from "@/components/common/access-denied"

export default function TerminalsPage() {
  const t = useTranslations("TerminalsAdmin")
  const { hasPermission } = useAuth()

  if (!hasPermission(PERMISSIONS.BRANCHES_VIEW)) {
    return <AccessDenied />
  }

  return (
    <div className="flex-1 space-y-4 pt-6">
      <h2 className="text-3xl font-bold tracking-tight">{t("title")}</h2>
      <TerminalsClient />
    </div>
  )
}
