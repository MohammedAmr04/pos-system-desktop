"use client"

import { useCallback, useEffect, useState } from "react"
import { useTranslations } from "next-intl"
import { UsersClient } from "./users-client"
import { api, RoleSummary, UserSummary } from "@/lib/api"
import { useAuth } from "@/features/auth/auth-context"
import { PERMISSIONS } from "@/lib/constants"
import { AccessDenied } from "@/components/common/access-denied"

export default function SettingsUsersPage() {
  const t = useTranslations("Users")
  const { hasPermission } = useAuth()
  const canManage = hasPermission(PERMISSIONS.USERS_MANAGE)
  const [users, setUsers] = useState<UserSummary[]>([])
  const [roles, setRoles] = useState<RoleSummary[]>([])
  const [loading, setLoading] = useState(true)
  const [requestId, setRequestId] = useState(0)

  useEffect(() => {
    if (!canManage) return
    let cancelled = false
    Promise.all([api.users.list(), api.roles.list()])
      .then(([usersRes, rolesRes]) => {
        if (cancelled) return
        setUsers(usersRes)
        setRoles(rolesRes)
      })
      .catch(() => {
        if (cancelled) return
        setUsers([])
        setRoles([])
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
      <UsersClient
        users={users}
        roles={roles}
        loading={loading}
        onRefresh={refresh}
      />
    </div>
  )
}
