"use client"

import { PermissionInfo, RoleSummary, api } from "@/lib/api"
import { useEffect, useState } from "react"
import { Button } from "@/components/ui/button"
import { Loader2, Lock, ShieldCheck } from "lucide-react"
import { useTranslations } from "next-intl"
import { toast } from "sonner"
import { Checkbox } from "@/components/ui/checkbox"
import { PERMISSION_LABELS, RESOURCE_LABELS } from "@/lib/constants"
import { useAuth } from "@/features/auth/auth-context"
import { cn } from "@/lib/utils"

interface PermissionsClientProps {
  roles: RoleSummary[]
  permissions: PermissionInfo[]
  loading: boolean
  onRefresh: () => void
}

const ADMIN_ROLE_ID = "role-admin"

function groupPermissions(permissions: PermissionInfo[]) {
  const groups = new Map<string, PermissionInfo[]>()
  for (const p of permissions) {
    const list = groups.get(p.resource) ?? []
    list.push(p)
    groups.set(p.resource, list)
  }
  return Array.from(groups.entries())
}

export function PermissionsClient({ roles, permissions, loading, onRefresh }: PermissionsClientProps) {
  const t = useTranslations("Permissions")
  const { refreshAccess } = useAuth()
  const [selectedRoleId, setSelectedRoleId] = useState<string | null>(null)
  const [selected, setSelected] = useState<string[]>([])
  const [loadingPerms, setLoadingPerms] = useState(false)
  const [saving, setSaving] = useState(false)

  const groups = groupPermissions(permissions)
  const selectedRole = roles.find((r) => r.id === selectedRoleId) ?? null
  const isAdmin = selectedRoleId === ADMIN_ROLE_ID

  useEffect(() => {
    if (!selectedRoleId || isAdmin) return
    let cancelled = false
    api.roles
      .getPermissions(selectedRoleId)
      .then((res) => {
        if (cancelled) return
        setSelected(res.permissionIds)
      })
      .catch(() => {
        if (!cancelled) toast.error(t("loadFailed"))
      })
      .finally(() => {
        if (!cancelled) setLoadingPerms(false)
      })
    return () => {
      cancelled = true
    }
  }, [selectedRoleId, isAdmin, t])

  const selectRole = (id: string) => {
    setSelectedRoleId(id)
    setSelected([])
    setLoadingPerms(id !== ADMIN_ROLE_ID)
  }

  const toggle = (key: string) => {
    setSelected((prev) =>
      prev.includes(key) ? prev.filter((k) => k !== key) : [...prev, key]
    )
  }

  const handleSave = async () => {
    if (!selectedRoleId || isAdmin) return
    setSaving(true)
    try {
      await api.roles.setPermissions(selectedRoleId, selected)
      await refreshAccess()
      toast.success(t("permissionsUpdated"))
      onRefresh()
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
    <div className="grid gap-4 lg:grid-cols-[240px_1fr]">
      <div className="rounded-md border bg-card p-2">
        <p className="px-2 pb-2 pt-1 text-xs font-medium text-muted-foreground">{t("selectRole")}</p>
        <div className="space-y-1">
          {roles.map((role) => {
            const active = role.id === selectedRoleId
            return (
              <button
                key={role.id}
                onClick={() => selectRole(role.id)}
                className={cn(
                  "flex w-full items-center gap-2 rounded-lg px-3 py-2 text-sm transition-colors",
                  active ? "bg-muted font-medium text-primary" : "hover:bg-muted/60"
                )}
              >
                <ShieldCheck className="h-4 w-4 shrink-0" />
                <span className="truncate">{role.name}</span>
              </button>
            )
          })}
        </div>
      </div>

      <div className="rounded-md border bg-card">
        {!selectedRole ? (
          <div className="p-8 text-center text-sm text-muted-foreground">
            {t("selectRoleHint")}
          </div>
        ) : (
          <div className="p-4 space-y-4">
            <div className="flex items-center justify-between gap-4">
              <div>
                <h3 className="text-lg font-semibold">{selectedRole.name}</h3>
                <p className="text-sm text-muted-foreground">
                  {t("permissionCount", { count: selected.length })}
                </p>
              </div>
              {isAdmin ? (
                <span className="inline-flex items-center gap-1.5 rounded-full bg-muted px-3 py-1 text-xs font-medium text-muted-foreground">
                  <Lock className="h-3.5 w-3.5" />
                  {t("adminLocked")}
                </span>
              ) : (
                <Button onClick={handleSave} disabled={saving || loadingPerms}>
                  {saving ? <Loader2 className="h-4 w-4 animate-spin" /> : null}
                  {t("save")}
                </Button>
              )}
            </div>

            <div className="max-h-[65vh] space-y-4 overflow-y-auto pr-1">
              {loadingPerms ? (
                <div className="flex justify-center py-10">
                  <Loader2 className="h-6 w-6 animate-spin" />
                </div>
              ) : (
                groups.map(([resource, perms]) => (
                  <div key={resource} className={cn(isAdmin && "pointer-events-none opacity-60")}>
                    <h4 className="mb-2 text-sm font-semibold">
                      {RESOURCE_LABELS[resource] ?? resource}
                    </h4>
                    <div className="space-y-1.5">
                      {perms.map((p) => (
                        <label
                          key={p.key}
                          className="flex cursor-pointer items-center gap-2 rounded-lg border px-3 py-2 text-sm transition-colors hover:bg-muted"
                        >
                          <Checkbox
                            checked={selected.includes(p.key)}
                            onCheckedChange={() => toggle(p.key)}
                            disabled={isAdmin}
                          />
                          <span>{PERMISSION_LABELS[p.key] ?? p.name}</span>
                        </label>
                      ))}
                    </div>
                  </div>
                ))
              )}
            </div>
          </div>
        )}
      </div>
    </div>
  )
}
