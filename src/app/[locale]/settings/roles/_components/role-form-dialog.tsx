"use client"

import { useState } from "react"
import { useTranslations } from "next-intl"
import { useApiError } from "@/lib/api-error"
import { toast } from "sonner"
import { Loader2 } from "lucide-react"
import { RoleSummary } from "@/types/domain/domain.types"
import { createRole, updateRole } from "@/actions/roles.actions"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"

interface RoleFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  role: RoleSummary | null
  onSaved: () => void
}

export function RoleFormDialog({ open, onOpenChange, role, onSaved }: RoleFormDialogProps) {
  const t = useTranslations("Roles")
  const resolveError = useApiError()
  const [form, setForm] = useState(() => ({
    name: role?.name ?? "",
    description: role?.description ?? "",
  }))
  const [isSaving, setIsSaving] = useState(false)

  const handleSave = async () => {
    const name = form.name.trim()
    if (!name) return
    setIsSaving(true)
    try {
      if (role) {
        await updateRole(role.id, { name, description: form.description.trim() })
        toast.success(t("roleUpdated"))
      } else {
        await createRole({ name, description: form.description.trim() })
        toast.success(t("roleCreated"))
      }
      onOpenChange(false)
      onSaved()
    } catch (e) {
      toast.error(resolveError(e) || t("saveFailed"))
    } finally {
      setIsSaving(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[440px]">
        <DialogHeader>
          <DialogTitle>{role ? t("editRole") : t("newRole")}</DialogTitle>
          <DialogDescription>{t("roleFormHint")}</DialogDescription>
        </DialogHeader>
        <div className="py-4 space-y-4">
          <div className="space-y-2">
            <label className="text-sm font-medium">{t("roleName")} *</label>
            <Input
              value={form.name}
              onChange={(e) => setForm({ ...form, name: e.target.value })}
              placeholder={t("roleNamePlaceholder")}
              autoFocus
            />
          </div>
          <div className="space-y-2">
            <label className="text-sm font-medium">{t("roleDesc")}</label>
            <Input
              value={form.description}
              onChange={(e) => setForm({ ...form, description: e.target.value })}
              placeholder={t("roleDescPlaceholder")}
            />
          </div>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            {t("cancel")}
          </Button>
          <Button onClick={handleSave} disabled={!form.name.trim() || isSaving}>
            {isSaving ? <Loader2 className="h-4 w-4 animate-spin" /> : null}
            {t("save")}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
