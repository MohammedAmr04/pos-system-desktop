"use client"

import { useCallback, useEffect, useState } from "react"
import { useTranslations } from "next-intl"
import { RolesClient } from "./roles-client"
import { api, PermissionInfo, RoleSummary } from "@/lib/api"
import { useAuth } from "@/features/auth/auth-context"
import { PERMISSIONS } from "@/lib/constants"
import { AccessDenied } from "@/components/common/access-denied"

export default function SettingsRolesPage() {
  const t = useTranslations("Roles")
  const { hasPermission } = useAuth()
  const canManage = hasPermission(PERMISSIONS.ROLES_MANAGE)
  const [roles, setRoles] = useState<RoleSummary[]>([])
  const [permissions, setPermissions] = useState<PermissionInfo[]>([])
  const [loading, setLoading] = useState(true)
  const [requestId, setRequestId] = useState(0)

  useEffect(() => {
    if (!canManage) return
    let cancelled = false
    Promise.all([api.roles.list(), api.permissions.list()])
      .then(([rolesRes, permissionsRes]) => {
        if (cancelled) return
        setRoles(rolesRes)
        setPermissions(permissionsRes)
      })
      .catch(() => {
        if (cancelled) return
        setRoles([])
        setPermissions([])
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [canManage, requestId])

  const refresh = useCallback(() => {
    setLoading(true)
    setRequestId((id) => id + 1)
  }, [])

  if (!canManage) {
    return <AccessDenied />
  }

  return (
    <div className="flex-1 space-y-4 pt-6">
      <div className="flex items-center justify-between space-y-2">
        <h2 className="text-3xl font-bold tracking-tight">{t("title")}</h2>
      </div>
      <RolesClient
        roles={roles}
        permissions={permissions}
        loading={loading}
        onRefresh={refresh}
      />
    </div>
  )
}
