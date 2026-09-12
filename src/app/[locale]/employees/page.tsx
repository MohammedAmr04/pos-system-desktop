"use client"

import { useTranslations } from "next-intl"
import { EmployeesClient } from "./employees-client"
import { useAuth } from "@/components/common/auth-context"
import { PERMISSIONS } from "@/lib/constants"
import { AccessDenied } from "@/components/common/access-denied"

export default function EmployeesPage() {
  const t = useTranslations("Employees")
  const { hasPermission } = useAuth()
  const canView = hasPermission(PERMISSIONS.EMPLOYEES_VIEW)

  if (!canView) {
    return <AccessDenied />
  }

  return (
    <div className="flex-1 space-y-4 pt-6">
      <div className="flex items-center justify-between space-y-2">
        <h2 className="text-3xl font-bold tracking-tight">{t("title")}</h2>
      </div>
      <EmployeesClient />
    </div>
  )
}
