"use client"

import { useTranslations } from "next-intl"
import { InvoicesClient } from "./invoices-client"
import { useAuth } from "@/components/common/auth-context"
import { PERMISSIONS } from "@/lib/constants"
import { AccessDenied } from "@/components/common/access-denied"

export default function InvoicesPage() {
  const t = useTranslations("Invoices")
  const { hasPermission } = useAuth()
  const canView = hasPermission(PERMISSIONS.INVOICES_VIEW)

  if (!canView) {
    return <AccessDenied />
  }

  return (
    <div className="flex-1 space-y-4 pt-6">
      <div className="flex items-center justify-between space-y-2">
        <h2 className="text-3xl font-bold tracking-tight">{t("title")}</h2>
      </div>
      <InvoicesClient />
    </div>
  )
}
