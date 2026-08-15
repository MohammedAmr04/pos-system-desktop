"use client"

import { PermissionInfo, RoleSummary, api } from "@/lib/api"
import { useState } from "react"
import { Button } from "@/components/ui/button"
import { Loader2, Plus, Pencil, Trash, ShieldCheck } from "lucide-react"
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
import { PERMISSION_LABELS, RESOURCE_LABELS } from "@/lib/constants"
import { useAuth } from "@/features/auth/auth-context"

interface RolesClientProps {
  roles: RoleSummary[]
  permissions: PermissionInfo[]
  loading: boolean
  onRefresh: () => void
}

const ADMIN_ROLE_ID = "role-admin"

interface RoleFormState {
  id?: string
  name: string
  description: string
}

interface PermissionsState {
  roleId: string
  roleName: string
  selected: string[]
}

function groupPermissions(permissions: PermissionInfo[]) {
  const groups = new Map<string, PermissionInfo[]>()
  for (const p of permissions) {
    const list = groups.get(p.resource) ?? []
    list.push(p)
    groups.set(p.resource, list)
  }
  return Array.from(groups.entries())
}

export function RolesClient({ roles, permissions, loading, onRefresh }: RolesClientProps) {
  const t = useTranslations("Roles")
  const { refreshAccess } = useAuth()
  const [isFormOpen, setIsFormOpen] = useState(false)
  const [form, setForm] = useState<RoleFormState>({ name: "", description: "" })
  const [isSaving, setIsSaving] = useState(false)
  const [permsDialog, setPermsDialog] = useState<PermissionsState | null>(null)
  const [permsSaving, setPermsSaving] = useState(false)
  const [deleteRole, setDeleteRole] = useState<RoleSummary | null>(null)
  const [isDeleting, setIsDeleting] = useState(false)

  const groups = groupPermissions(permissions)

  const openCreate = () => {
    setForm({ name: "", description: "" })
    setIsFormOpen(true)
  }

  const openEdit = (role: RoleSummary) => {
    setForm({ id: role.id, name: role.name, description: role.description ?? "" })
    setIsFormOpen(true)
  }

  const handleSaveRole = async () => {
    const name = form.name.trim()
    if (!name) return
    setIsSaving(true)
    try {
      if (form.id) {
        await api.roles.update(form.id, { name, description: form.description.trim() })
        toast.success(t("roleUpdated"))
      } else {
        await api.roles.create({ name, description: form.description.trim() })
        toast.success(t("roleCreated"))
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

  const openPermissions = async (role: RoleSummary) => {
    try {
      const res = await api.roles.getPermissions(role.id)
      setPermsDialog({ roleId: role.id, roleName: role.name, selected: res.permissionIds })
    } catch {
      toast.error(t("loadFailed"))
    }
  }

  const togglePermission = (key: string) => {
    setPermsDialog((prev) => {
      if (!prev) return prev
      const selected = prev.selected.includes(key)
        ? prev.selected.filter((k) => k !== key)
        : [...prev.selected, key]
      return { ...prev, selected }
    })
  }

  const handleSavePermissions = async () => {
    if (!permsDialog) return
    setPermsSaving(true)
    try {
      await api.roles.setPermissions(permsDialog.roleId, permsDialog.selected)
      toast.success(t("permissionsUpdated"))
      setPermsDialog(null)
      await refreshAccess()
      onRefresh()
    } catch (e) {
      toast.error((e as Error).message || t("saveFailed"))
    } finally {
      setPermsSaving(false)
    }
  }

  const handleDelete = async () => {
    if (!deleteRole) return
    setIsDeleting(true)
    try {
      await api.roles.remove(deleteRole.id)
      toast.success(t("roleDeleted"))
      setDeleteRole(null)
      await refreshAccess()
      onRefresh()
    } catch (e) {
      toast.error((e as Error).message || t("deleteFailed"))
    } finally {
      setIsDeleting(false)
    }
  }

  return (
    <>
      <div className="flex items-center justify-between gap-4 mb-4">
        <p className="text-sm text-muted-foreground">{t("description")}</p>
        <Button onClick={openCreate}>
          <Plus className="mr-2 h-4 w-4" /> {t("newRole")}
        </Button>
      </div>

      <div className="rounded-md border bg-card">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>{t("role")}</TableHead>
              <TableHead>{t("roleDesc")}</TableHead>
              <TableHead>{t("users")}</TableHead>
              <TableHead>{t("permissions")}</TableHead>
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
            ) : roles.length ? (
              roles.map((role) => {
                const isAdmin = role.id === ADMIN_ROLE_ID
                return (
                  <TableRow key={role.id}>
                    <TableCell>
                      <div className="flex items-center gap-2">
                        <span className="font-medium">{role.name}</span>
                        {role.isSystem && (
                          <ShieldCheck className="h-4 w-4 text-muted-foreground" />
                        )}
                      </div>
                    </TableCell>
                    <TableCell className="text-muted-foreground">{role.description}</TableCell>
                    <TableCell>{role.userCount}</TableCell>
                    <TableCell>{role.permissionCount}</TableCell>
                    <TableCell>
                      <div className="flex items-center gap-1">
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => openPermissions(role)}
                          disabled={isAdmin}
                          title={isAdmin ? t("adminLocked") : undefined}
                        >
                          <ShieldCheck className="h-4 w-4" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => openEdit(role)}
                          disabled={role.isSystem}
                          title={role.isSystem ? t("systemLocked") : undefined}
                        >
                          <Pencil className="h-4 w-4" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => setDeleteRole(role)}
                          disabled={role.isSystem || role.userCount > 0}
                          title={role.isSystem || role.userCount > 0 ? t("deleteLocked") : undefined}
                        >
                          <Trash className="h-4 w-4 text-destructive" />
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                )
              })
            ) : (
              <TableRow>
                <TableCell colSpan={5} className="h-24 text-center">
                  {t("noRoles")}
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </div>

      <Dialog open={isFormOpen} onOpenChange={setIsFormOpen}>
        <DialogContent className="sm:max-w-[440px]">
          <DialogHeader>
            <DialogTitle>{form.id ? t("editRole") : t("newRole")}</DialogTitle>
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
            <Button variant="outline" onClick={() => setIsFormOpen(false)}>
              {t("cancel")}
            </Button>
            <Button onClick={handleSaveRole} disabled={!form.name.trim() || isSaving}>
              {isSaving ? <Loader2 className="h-4 w-4 animate-spin" /> : null}
              {t("save")}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog
        open={permsDialog !== null}
        onOpenChange={(open) => {
          if (!open) setPermsDialog(null)
        }}
      >
        <DialogContent className="sm:max-w-[560px]">
          <DialogHeader>
            <DialogTitle>{t("assignPermissionsTitle", { role: permsDialog?.roleName ?? "" })}</DialogTitle>
            <DialogDescription>{t("assignPermissionsHint")}</DialogDescription>
          </DialogHeader>
          <div className="max-h-[55vh] overflow-y-auto py-4 space-y-4">
            {groups.map(([resource, perms]) => (
              <div key={resource}>
                <h4 className="mb-2 text-sm font-semibold">{RESOURCE_LABELS[resource] ?? resource}</h4>
                <div className="space-y-1.5">
                  {perms.map((p) => (
                    <label
                      key={p.key}
                      className="flex cursor-pointer items-center gap-2 rounded-lg border px-3 py-2 text-sm transition-colors hover:bg-muted"
                    >
                      <Checkbox
                        checked={permsDialog?.selected.includes(p.key) ?? false}
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
            <Button variant="outline" onClick={() => setPermsDialog(null)}>
              {t("cancel")}
            </Button>
            <Button onClick={handleSavePermissions} disabled={permsSaving}>
              {permsSaving ? <Loader2 className="h-4 w-4 animate-spin" /> : null}
              {t("save")}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={deleteRole !== null} onOpenChange={(open) => { if (!open) setDeleteRole(null) }}>
        <DialogContent className="sm:max-w-[400px]">
          <DialogHeader>
            <DialogTitle>{t("deleteTitle")}</DialogTitle>
            <DialogDescription>
              {t("deleteConfirm", { role: deleteRole?.name ?? "" })}
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setDeleteRole(null)}>
              {t("cancel")}
            </Button>
            <Button variant="destructive" onClick={handleDelete} disabled={isDeleting}>
              {isDeleting ? <Loader2 className="h-4 w-4 animate-spin" /> : null}
              {t("delete")}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  )
}
