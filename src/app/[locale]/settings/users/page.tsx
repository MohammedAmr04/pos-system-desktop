"use client"

import { useTranslations } from "next-intl"
import { UsersClient } from "./users-client"
import { useAuth } from "@/components/common/auth-context"
import { PERMISSIONS } from "@/lib/constants"
import { AccessDenied } from "@/components/common/access-denied"

export default function SettingsUsersPage() {
  const t = useTranslations("Users")
  const { hasPermission } = useAuth()
  const canManage = hasPermission(PERMISSIONS.USERS_MANAGE)

  if (!canManage) {
    return <AccessDenied />
  }

  return (
    <div className="flex-1 space-y-4 pt-6">
      <div className="flex items-center justify-between space-y-2">
        <h2 className="text-3xl font-bold tracking-tight">{t("title")}</h2>
      </div>
      <UsersClient />
    </div>
  )
}
