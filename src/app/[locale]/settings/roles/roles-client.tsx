"use client"

import { useState } from "react"
import { useQueryClient } from "@tanstack/react-query"
import { useTranslations } from "next-intl"
import { useApiError } from "@/lib/api-error"
import { toast } from "sonner"
import { Pencil, Plus, ShieldCheck, Trash, Loader2 } from "lucide-react"

import { RoleSummary } from "@/types/domain/domain.types"
import { adminKeys, usePermissions, useRoles } from "@/hooks/use-admin"
import { deleteRole } from "@/actions/roles.actions"
import { useAuth } from "@/components/common/auth-context"
import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { TableColumn, TableBuilder } from "@/components/common/table-builder"
import { TooltipIconButton } from "@/components/common/tooltip-icon-button"
import { RoleFormDialog } from "./_components/role-form-dialog"
import { RolePermissionsDialog } from "./_components/role-permissions-dialog"

const ADMIN_ROLE_ID = "role-admin"

export function RolesClient() {
  const t = useTranslations("Roles")
  const resolveError = useApiError()
  const queryClient = useQueryClient()
  const { refreshAccess } = useAuth()
  const [isFormOpen, setIsFormOpen] = useState(false)
  const [editingRole, setEditingRole] = useState<RoleSummary | null>(null)
  const [permsRole, setPermsRole] = useState<RoleSummary | null>(null)
  const [roleToDelete, setRoleToDelete] = useState<RoleSummary | null>(null)
  const [isDeleting, setIsDeleting] = useState(false)

  const { data: roles = [], isPending } = useRoles()
  usePermissions()

  const refresh = async () => {
    await queryClient.invalidateQueries({ queryKey: adminKeys.all() })
    await refreshAccess()
  }

  const openCreate = () => {
    setEditingRole(null)
    setIsFormOpen(true)
  }

  const openEdit = (role: RoleSummary) => {
    setEditingRole(role)
    setIsFormOpen(true)
  }

  const handleDelete = async () => {
    if (!roleToDelete) return
    setIsDeleting(true)
    try {
      await deleteRole(roleToDelete.id)
      toast.success(t("roleDeleted"))
      setRoleToDelete(null)
      await refresh()
    } catch (e) {
      toast.error(resolveError(e) || t("deleteFailed"))
    } finally {
      setIsDeleting(false)
    }
  }

  const columns: TableColumn<RoleSummary>[] = [
    {
      key: "name",
      header: t("role"),
      cell: (role) => (
        <div className="flex items-center gap-2">
          <span className="font-medium">{role.name}</span>
          {role.isSystem && <ShieldCheck className="h-4 w-4 text-muted-foreground" />}
        </div>
      ),
    },
    {
      key: "description",
      header: t("roleDesc"),
      cell: (role) => <span className="text-muted-foreground">{role.description}</span>,
    },
    {
      key: "userCount",
      header: t("users"),
      cell: (role) => role.userCount,
    },
    {
      key: "permissionCount",
      header: t("permissions"),
      cell: (role) => role.permissionCount,
    },
    {
      key: "actions",
      header: t("actions"),
      headClassName: "w-32",
      cell: (role) => {
        const isAdmin = role.id === ADMIN_ROLE_ID
        return (
          <div className="flex items-center gap-1">
            <TooltipIconButton
              label={t("permissions")}
              variant="ghost"
              size="sm"
              disabled={isAdmin}
              onClick={() => setPermsRole(role)}
            >
              <ShieldCheck className="h-4 w-4" />
            </TooltipIconButton>
            <TooltipIconButton
              label={role.isSystem ? t("systemLocked") : t("editRole")}
              variant="ghost"
              size="sm"
              disabled={role.isSystem}
              onClick={() => openEdit(role)}
            >
              <Pencil className="h-4 w-4" />
            </TooltipIconButton>
            <TooltipIconButton
              label={role.isSystem || role.userCount > 0 ? t("deleteLocked") : t("delete")}
              variant="ghost"
              size="sm"
              disabled={role.isSystem || role.userCount > 0}
              onClick={() => setRoleToDelete(role)}
            >
              <Trash className="h-4 w-4 text-destructive" />
            </TooltipIconButton>
          </div>
        )
      },
    },
  ]

  return (
    <>
      <div className="flex items-center justify-between gap-4 mb-4">
        <p className="text-sm text-muted-foreground">{t("description")}</p>
        <Button onClick={openCreate}>
          <Plus className="mr-2 h-4 w-4" /> {t("newRole")}
        </Button>
      </div>

      <TableBuilder
        columns={columns}
        data={roles}
        rowKey={(role) => role.id}
        loading={isPending}
        emptyMessage={t("noRoles")}
      />

      <RoleFormDialog
        key={editingRole?.id ?? "new-role"}
        open={isFormOpen}
        onOpenChange={(open) => {
          setIsFormOpen(open)
          if (!open) setEditingRole(null)
        }}
        role={editingRole}
        onSaved={refresh}
      />

      <RolePermissionsDialog
        role={permsRole}
        onOpenChange={(open) => {
          if (!open) setPermsRole(null)
        }}
        onSaved={refresh}
      />

      <Dialog open={roleToDelete !== null} onOpenChange={(open) => { if (!open) setRoleToDelete(null) }}>
        <DialogContent className="sm:max-w-[400px]">
          <DialogHeader>
            <DialogTitle>{t("deleteTitle")}</DialogTitle>
            <DialogDescription>
              {t("deleteConfirm", { role: roleToDelete?.name ?? "" })}
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setRoleToDelete(null)}>
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
