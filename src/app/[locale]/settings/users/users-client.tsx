"use client"

import { RoleSummary, UserSummary, api } from "@/lib/api"
import { useState } from "react"
import { Button } from "@/components/ui/button"
import { Loader2, Plus, Pencil, UserCheck, UserX } from "lucide-react"
import { useTranslations } from "next-intl"
import { toast } from "sonner"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Input } from "@/components/ui/input"
import { Checkbox } from "@/components/ui/checkbox"
import { Switch } from "@/components/ui/switch"
import { useAuth } from "@/features/auth/auth-context"

interface UsersClientProps {
  users: UserSummary[]
  roles: RoleSummary[]
  loading: boolean
  onRefresh: () => void
}

interface UserFormState {
  id?: string
  name: string
  username: string
  password: string
  isActive: boolean
  roleIds: string[]
}

export function UsersClient({ users, roles, loading, onRefresh }: UsersClientProps) {
  const t = useTranslations("Users")
  const { refreshAccess } = useAuth()
  const [isFormOpen, setIsFormOpen] = useState(false)
  const [form, setForm] = useState<UserFormState>({ name: "", username: "", password: "", isActive: true, roleIds: [] })
  const [isSaving, setIsSaving] = useState(false)

  const openCreate = () => {
    setForm({ name: "", username: "", password: "", isActive: true, roleIds: [] })
    setIsFormOpen(true)
  }

  const openEdit = (user: UserSummary) => {
    setForm({ id: user.id, name: user.name, username: user.username, password: "", isActive: user.isActive, roleIds: [...user.roleIds] })
    setIsFormOpen(true)
  }

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
    if (!form.id && form.password.length < 4) return
    setIsSaving(true)
    try {
      if (form.id) {
        await api.users.update(form.id, {
          name,
          username,
          isActive: form.isActive,
          roleIds: form.roleIds,
          ...(form.password ? { password: form.password } : {}),
        })
        toast.success(t("userUpdated"))
      } else {
        await api.users.create({ name, username, password: form.password, roleIds: form.roleIds })
        toast.success(t("userCreated"))
      }
      setIsFormOpen(false)
      await refreshAccess()
      onRefresh()
    } catch (e) {
      toast.error((e as Error).message || t("saveFailed"))
    } finally {
      setIsSaving(false)
    }
  }

  const handleToggleActive = async (user: UserSummary) => {
    try {
      await api.users.update(user.id, { isActive: !user.isActive })
      toast.success(user.isActive ? t("userDeactivated") : t("userActivated"))
      await refreshAccess()
      onRefresh()
    } catch (e) {
      toast.error((e as Error).message || t("saveFailed"))
    }
  }

  return (
    <>
      <div className="flex items-center justify-between gap-4 mb-4">
        <p className="text-sm text-muted-foreground">{t("description")}</p>
        <Button onClick={openCreate}>
          <Plus className="mr-2 h-4 w-4" /> {t("newUser")}
        </Button>
      </div>

      <div className="rounded-md border bg-card">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>{t("user")}</TableHead>
              <TableHead>{t("username")}</TableHead>
              <TableHead>{t("roles")}</TableHead>
              <TableHead>{t("status")}</TableHead>
              <TableHead>{t("actions")}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading ? (
              <TableRow>
                <TableCell colSpan={5} className="h-24 text-center">
                  <Loader2 className="mx-auto h-6 w-6 animate-spin" />
                </TableCell>
              </TableRow>
            ) : users.length ? (
              users.map((user) => (
                <TableRow key={user.id}>
                  <TableCell className="font-medium">{user.name}</TableCell>
                  <TableCell className="text-muted-foreground">{user.username}</TableCell>
                  <TableCell>
                    {user.roleIds
                      .map((rid) => roles.find((r) => r.id === rid)?.name ?? rid)
                      .join(", ")}
                  </TableCell>
                  <TableCell>
                    <span
                      className={
                        user.isActive
                          ? "inline-flex items-center rounded-full bg-emerald-500/10 px-2 py-0.5 text-xs font-medium text-emerald-600"
                          : "inline-flex items-center rounded-full bg-destructive/10 px-2 py-0.5 text-xs font-medium text-destructive"
                      }
                    >
                      {user.isActive ? t("active") : t("inactive")}
                    </span>
                  </TableCell>
                  <TableCell>
                    <div className="flex items-center gap-1">
                      <Button variant="ghost" size="sm" onClick={() => handleToggleActive(user)}>
                        {user.isActive ? <UserX className="h-4 w-4" /> : <UserCheck className="h-4 w-4" />}
                      </Button>
                      <Button variant="ghost" size="sm" onClick={() => openEdit(user)}>
                        <Pencil className="h-4 w-4" />
                      </Button>
                    </div>
                  </TableCell>
                </TableRow>
              ))
            ) : (
              <TableRow>
                <TableCell colSpan={5} className="h-24 text-center">
                  {t("noUsers")}
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </div>

      <Dialog open={isFormOpen} onOpenChange={setIsFormOpen}>
        <DialogContent className="sm:max-w-[480px]">
          <DialogHeader>
            <DialogTitle>{form.id ? t("editUser") : t("newUser")}</DialogTitle>
            <DialogDescription>{form.id ? t("editHint") : t("createHint")}</DialogDescription>
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
                autoFocus={!form.id}
              />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">
                {t("password")} {form.id ? `(${t("passwordOptional")})` : "*"}
              </label>
              <Input
                type="password"
                maxLength={64}
                value={form.password}
                onChange={(e) => setForm({ ...form, password: e.target.value })}
                placeholder={t("passwordPlaceholder")}
              />
            </div>
            {form.id && (
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
            <Button variant="outline" onClick={() => setIsFormOpen(false)}>
              {t("cancel")}
            </Button>
            <Button
              onClick={handleSave}
              disabled={
                !form.name.trim() ||
                !form.username.trim() ||
                !form.roleIds.length ||
                (!form.id && form.password.length < 4) ||
                (form.id && form.password.length > 0 && form.password.length < 4) ||
                isSaving
              }
            >
              {isSaving ? <Loader2 className="h-4 w-4 animate-spin" /> : null}
              {t("save")}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  )
}
