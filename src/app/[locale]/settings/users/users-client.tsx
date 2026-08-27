"use client"

import { useState } from "react"
import { useQueryClient } from "@tanstack/react-query"
import { useTranslations } from "next-intl"
import { toast } from "sonner"
import { Pencil, Plus, UserCheck, UserX } from "lucide-react"

import { UserSummary } from "@/types/domain/domain.types"
import { adminKeys, useRoles, useUsers } from "@/hooks/use-admin"
import { updateUser } from "@/actions/users.actions"
import { useAuth } from "@/components/common/auth-context"
import { Button } from "@/components/ui/button"
import { TableColumn, TableBuilder } from "@/components/common/table-builder"
import { TooltipIconButton } from "@/components/common/tooltip-icon-button"
import { UserFormDialog } from "./_components/user-form-dialog"

export function UsersClient() {
  const t = useTranslations("Users")
  const queryClient = useQueryClient()
  const { refreshAccess } = useAuth()
  const [isFormOpen, setIsFormOpen] = useState(false)
  const [editingUser, setEditingUser] = useState<UserSummary | null>(null)

  const { data: users = [], isPending: usersLoading } = useUsers()
  const { data: roles = [] } = useRoles()

  const refresh = async () => {
    await queryClient.invalidateQueries({ queryKey: adminKeys.all() })
    await refreshAccess()
  }

  const openCreate = () => {
    setEditingUser(null)
    setIsFormOpen(true)
  }

  const openEdit = (user: UserSummary) => {
    setEditingUser(user)
    setIsFormOpen(true)
  }

  const handleToggleActive = async (user: UserSummary) => {
    try {
      await updateUser(user.id, { isActive: !user.isActive })
      toast.success(user.isActive ? t("userDeactivated") : t("userActivated"))
      await refresh()
    } catch (e) {
      toast.error((e as Error).message || t("saveFailed"))
    }
  }

  const columns: TableColumn<UserSummary>[] = [
    {
      key: "name",
      header: t("user"),
      cell: (user) => <span className="font-medium">{user.name}</span>,
    },
    {
      key: "username",
      header: t("username"),
      cell: (user) => <span className="text-muted-foreground">{user.username}</span>,
    },
    {
      key: "roles",
      header: t("roles"),
      cell: (user) =>
        user.roleIds
          .map((rid) => roles.find((r) => r.id === rid)?.name ?? rid)
          .join(", "),
    },
    {
      key: "status",
      header: t("status"),
      cell: (user) => (
        <span
          className={
            user.isActive
              ? "inline-flex items-center rounded-full bg-emerald-500/10 px-2 py-0.5 text-xs font-medium text-emerald-600"
              : "inline-flex items-center rounded-full bg-destructive/10 px-2 py-0.5 text-xs font-medium text-destructive"
          }
        >
          {user.isActive ? t("active") : t("inactive")}
        </span>
      ),
    },
    {
      key: "actions",
      header: t("actions"),
      headClassName: "w-28",
      cell: (user) => (
        <div className="flex items-center gap-1">
          <TooltipIconButton
            label={user.isActive ? t("userDeactivated") : t("userActivated")}
            variant="ghost"
            size="sm"
            onClick={() => handleToggleActive(user)}
          >
            {user.isActive ? <UserX className="h-4 w-4" /> : <UserCheck className="h-4 w-4" />}
          </TooltipIconButton>
          <TooltipIconButton
            label={t("editUser")}
            variant="ghost"
            size="sm"
            onClick={() => openEdit(user)}
          >
            <Pencil className="h-4 w-4" />
          </TooltipIconButton>
        </div>
      ),
    },
  ]

  return (
    <>
      <div className="flex items-center justify-between gap-4 mb-4">
        <p className="text-sm text-muted-foreground">{t("description")}</p>
        <Button onClick={openCreate}>
          <Plus className="mr-2 h-4 w-4" /> {t("newUser")}
        </Button>
      </div>

      <TableBuilder
        columns={columns}
        data={users}
        rowKey={(user) => user.id}
        loading={usersLoading}
        emptyMessage={t("noUsers")}
      />

      <UserFormDialog
        key={editingUser?.id ?? "new-user"}
        open={isFormOpen}
        onOpenChange={(open) => {
          setIsFormOpen(open)
          if (!open) setEditingUser(null)
        }}
        roles={roles}
        user={editingUser}
        onSaved={refresh}
      />
    </>
  )
}
