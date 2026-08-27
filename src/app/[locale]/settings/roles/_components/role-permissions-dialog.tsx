"use client"

import { useState } from "react"
import { useTranslations } from "next-intl"
import { toast } from "sonner"
import { Loader2 } from "lucide-react"
import { RoleSummary } from "@/types/domain/domain.types"
import { setRolePermissions } from "@/actions/roles.actions"
import { usePermissions, useRolePermissions } from "@/hooks/use-admin"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { Checkbox } from "@/components/ui/checkbox"
import { PERMISSION_LABELS, RESOURCE_LABELS } from "@/lib/constants"

interface RolePermissionsDialogProps {
  role: RoleSummary | null
  onOpenChange: (open: boolean) => void
  onSaved: () => void
}

export function RolePermissionsDialog({ role, onOpenChange, onSaved }: RolePermissionsDialogProps) {
  const t = useTranslations("Roles")
  const [selected, setSelected] = useState<string[]>([])
  const [initializedFor, setInitializedFor] = useState<string | null>(null)
  const [permsSaving, setPermsSaving] = useState(false)

  const { data: permissions = [] } = usePermissions()
  const { data: current } = useRolePermissions(role?.id ?? "", !!role)

  // Canonical React "reset state when props change" pattern.
  if (role && initializedFor !== role.id && current) {
    setInitializedFor(role.id)
    setSelected([...current.permissionIds])
  }

  const groups = new Map<string, typeof permissions>()
  for (const p of permissions) {
    const list = groups.get(p.resource) ?? []
    list.push(p)
    groups.set(p.resource, list)
  }

  const togglePermission = (key: string) => {
    setSelected((prev) =>
      prev.includes(key)
        ? prev.filter((k) => k !== key)
        : [...prev, key]
    )
  }

  const handleSave = async () => {
    if (!role) return
    setPermsSaving(true)
    try {
      await setRolePermissions(role.id, selected)
      toast.success(t("permissionsUpdated"))
      onOpenChange(false)
      onSaved()
    } catch (e) {
      toast.error((e as Error).message || t("saveFailed"))
    } finally {
      setPermsSaving(false)
    }
  }

  return (
    <Dialog
      open={role !== null}
      onOpenChange={(open) => {
        if (!open) onOpenChange(false)
      }}
    >
      <DialogContent className="sm:max-w-[560px]">
        <DialogHeader>
          <DialogTitle>{t("assignPermissionsTitle", { role: role?.name ?? "" })}</DialogTitle>
          <DialogDescription>{t("assignPermissionsHint")}</DialogDescription>
        </DialogHeader>
        <div className="max-h-[55vh] overflow-y-auto py-4 space-y-4">
          {Array.from(groups.entries()).map(([resource, perms]) => (
            <div key={resource}>
              <h4 className="mb-2 text-sm font-semibold">{RESOURCE_LABELS[resource] ?? resource}</h4>
              <div className="space-y-1.5">
                {perms.map((p) => (
                  <label
                    key={p.key}
                    className="flex cursor-pointer items-center gap-2 rounded-lg border px-3 py-2 text-sm transition-colors hover:bg-muted"
                  >
                    <Checkbox
                      checked={selected.includes(p.key)}
                      onCheckedChange={() => togglePermission(p.key)}
                    />
                    <span>{PERMISSION_LABELS[p.key] ?? p.name}</span>
                  </label>
                ))}
              </div>
            </div>
          ))}
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            {t("cancel")}
          </Button>
          <Button onClick={handleSave} disabled={permsSaving}>
            {permsSaving ? <Loader2 className="h-4 w-4 animate-spin" /> : null}
            {t("save")}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
