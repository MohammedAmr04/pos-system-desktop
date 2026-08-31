"use client"

import { useMemo, useState } from "react"
import { useQueryClient } from "@tanstack/react-query"
import { useTranslations } from "next-intl"
import { useApiError } from "@/lib/api-error"
import { toast } from "sonner"
import { Loader2, Lock, ShieldCheck } from "lucide-react"
import { setRolePermissions } from "@/actions/roles.actions"
import { adminKeys, usePermissions, useRolePermissions, useRoles } from "@/hooks/use-admin"
import { useAuth } from "@/components/common/auth-context"
import { Button } from "@/components/ui/button"
import { Checkbox } from "@/components/ui/checkbox"
import { PERMISSION_LABELS, RESOURCE_LABELS } from "@/lib/constants"
import { cn } from "@/lib/utils"

const ADMIN_ROLE_ID = "role-admin"

export function PermissionsClient() {
  const t = useTranslations("Permissions")
  const resolveError = useApiError()
  const queryClient = useQueryClient()
  const { refreshAccess } = useAuth()
  const [selectedRoleId, setSelectedRoleId] = useState<string | null>(null)
  // Local edits for the selected role; null = mirror the server data.
  const [edited, setEdited] = useState<{ roleId: string; perms: string[] } | null>(null)
  const [saving, setSaving] = useState(false)

  const { data: roles = [], isPending: rolesLoading } = useRoles()
  const { data: permissions = [] } = usePermissions()

  const selectedRole = roles.find((r) => r.id === selectedRoleId) ?? null
  const isAdmin = selectedRoleId === ADMIN_ROLE_ID

  const { data: currentPerms, isPending: loadingPerms } = useRolePermissions(
    selectedRoleId ?? "",
    !!selectedRoleId && !isAdmin
  )

  const selected = useMemo(() => {
    if (!selectedRoleId || isAdmin) return []
    if (edited && edited.roleId === selectedRoleId) return edited.perms
    return currentPerms?.permissionIds ?? []
  }, [selectedRoleId, isAdmin, edited, currentPerms])

  const groups = new Map<string, typeof permissions>()
  for (const p of permissions) {
    const list = groups.get(p.resource) ?? []
    list.push(p)
    groups.set(p.resource, list)
  }

  const selectRole = (id: string) => {
    setSelectedRoleId(id)
    setEdited(null)
  }

  const toggle = (key: string) => {
    if (!selectedRoleId || isAdmin) return
    const next = selected.includes(key)
      ? selected.filter((k) => k !== key)
      : [...selected, key]
    setEdited({ roleId: selectedRoleId, perms: next })
  }

  const handleSave = async () => {
    if (!selectedRoleId || isAdmin) return
    setSaving(true)
    try {
      await setRolePermissions(selectedRoleId, selected)
      await refreshAccess()
      toast.success(t("permissionsUpdated"))
      await queryClient.invalidateQueries({ queryKey: adminKeys.all() })
    } catch (e) {
      toast.error(resolveError(e) || t("saveFailed"))
    } finally {
      setSaving(false)
    }
  }

  if (rolesLoading) {
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
                Array.from(groups.entries()).map(([resource, perms]) => (
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
