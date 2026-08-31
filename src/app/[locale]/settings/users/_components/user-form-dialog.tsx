"use client"

import { useState } from "react"
import { useTranslations } from "next-intl"
import { useApiError } from "@/lib/api-error"
import { toast } from "sonner"
import { Loader2 } from "lucide-react"
import { RoleSummary, UserSummary } from "@/types/domain/domain.types"
import { createUser, updateUser } from "@/actions/users.actions"
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
import { Checkbox } from "@/components/ui/checkbox"
import { Switch } from "@/components/ui/switch"

interface UserFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  roles: RoleSummary[]
  user: UserSummary | null
  onSaved: () => void
}

interface UserFormState {
  name: string
  username: string
  password: string
  isActive: boolean
  roleIds: string[]
}

export function UserFormDialog({ open, onOpenChange, roles, user, onSaved }: UserFormDialogProps) {
  const t = useTranslations("Users")
  const resolveError = useApiError()
  const [form, setForm] = useState<UserFormState>(() =>
    user
      ? { name: user.name, username: user.username, password: "", isActive: user.isActive, roleIds: [...user.roleIds] }
      : { name: "", username: "", password: "", isActive: true, roleIds: [] }
  )
  const [isSaving, setIsSaving] = useState(false)

  const toggleRole = (roleId: string) => {
    setForm((prev) => ({
      ...prev,
      roleIds: prev.roleIds.includes(roleId)
        ? prev.roleIds.filter((id) => id !== roleId)
        : [...prev.roleIds, roleId],
    }))
  }

  const handleSave = async () => {
    const name = form.name.trim()
    const username = form.username.trim()
    if (!name || !username || !form.roleIds.length) return
    if (!user && form.password.length < 4) return
    setIsSaving(true)
    try {
      if (user) {
        await updateUser(user.id, {
          name,
          username,
          isActive: form.isActive,
          roleIds: form.roleIds,
          ...(form.password ? { password: form.password } : {}),
        })
        toast.success(t("userUpdated"))
      } else {
        await createUser({ name, username, password: form.password, roleIds: form.roleIds })
        toast.success(t("userCreated"))
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
      <DialogContent className="sm:max-w-[480px]">
        <DialogHeader>
          <DialogTitle>{user ? t("editUser") : t("newUser")}</DialogTitle>
          <DialogDescription>{user ? t("editHint") : t("createHint")}</DialogDescription>
        </DialogHeader>
        <div className="py-4 space-y-4">
          <div className="space-y-2">
            <label className="text-sm font-medium">{t("userName")} *</label>
            <Input
              value={form.name}
              onChange={(e) => setForm({ ...form, name: e.target.value })}
              placeholder={t("userNamePlaceholder")}
            />
          </div>
          <div className="space-y-2">
            <label className="text-sm font-medium">{t("username")} *</label>
            <Input
              value={form.username}
              onChange={(e) => setForm({ ...form, username: e.target.value })}
              placeholder={t("usernamePlaceholder")}
              autoFocus={!user}
            />
          </div>
          <div className="space-y-2">
            <label className="text-sm font-medium">
              {t("password")} {user ? `(${t("passwordOptional")})` : "*"}
            </label>
            <Input
              type="password"
              maxLength={64}
              value={form.password}
              onChange={(e) => setForm({ ...form, password: e.target.value })}
              placeholder={t("passwordPlaceholder")}
            />
          </div>
          {user && (
            <div className="flex items-center justify-between rounded-lg border px-3 py-2">
              <span className="text-sm font-medium">{t("active")}</span>
              <Switch
                checked={form.isActive}
                onCheckedChange={(checked) => setForm({ ...form, isActive: checked })}
              />
            </div>
          )}
          <div className="space-y-2">
            <label className="text-sm font-medium">{t("roles")} *</label>
            <div className="space-y-1.5">
              {roles.map((role) => (
                <label
                  key={role.id}
                  className="flex cursor-pointer items-center gap-2 rounded-lg border px-3 py-2 text-sm transition-colors hover:bg-muted"
                >
                  <Checkbox
                    checked={form.roleIds.includes(role.id)}
                    onCheckedChange={() => toggleRole(role.id)}
                  />
                  <span>{role.name}</span>
                </label>
              ))}
            </div>
          </div>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            {t("cancel")}
          </Button>
          <Button
            onClick={handleSave}
            disabled={
              !form.name.trim() ||
              !form.username.trim() ||
              !form.roleIds.length ||
              (!user && form.password.length < 4) ||
              (user && form.password.length > 0 && form.password.length < 4) ||
              isSaving
            }
          >
            {isSaving ? <Loader2 className="h-4 w-4 animate-spin" /> : null}
            {t("save")}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
