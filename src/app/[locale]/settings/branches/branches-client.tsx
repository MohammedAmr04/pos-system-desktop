"use client"

import { useEffect, useState } from "react"
import { useTranslations } from "next-intl"
import { useApiError } from "@/lib/api-error"
import { toast } from "sonner"
import { CircleCheck, CircleX, KeyRound, Pencil, Plus, Trash } from "lucide-react"

import { Branch } from "@/types/domain/domain.types"
import { useBranchesPage } from "@/hooks/use-branches"
import { deleteBranchAction, setBranchActiveAction } from "@/actions/branches.actions"
import { useDebouncedCallback } from "@/hooks/use-debounced-callback"
import { useAuth } from "@/components/common/auth-context"
import { PERMISSIONS } from "@/lib/constants"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { TableColumn, TableBuilder } from "@/components/common/table-builder"
import { DataPagination } from "@/components/common/data-pagination"
import { TooltipIconButton } from "@/components/common/tooltip-icon-button"
import { BranchFormDialog } from "./_components/branch-form-dialog"
import { BranchSecretDialog } from "./_components/branch-secret-dialog"
import { BranchRuntimeCard } from "./_components/branch-runtime-card"

const PAGE_SIZE = 20

export function BranchesClient() {
  const t = useTranslations("BranchesAdmin")
  const tc = useTranslations("Common")
  const resolveError = useApiError()
  const { hasPermission } = useAuth()
  const canManage = hasPermission(PERMISSIONS.BRANCHES_MANAGE)

  const [page, setPage] = useState(1)
  const [query, setQuery] = useState("")
  const [searchInput, setSearchInput] = useState("")
  const [isFormOpen, setIsFormOpen] = useState(false)
  const [editingBranch, setEditingBranch] = useState<Branch | null>(null)
  const [secretBranch, setSecretBranch] = useState<Branch | null>(null)
  const [formSession, setFormSession] = useState(0)

  const debouncedQueryChange = useDebouncedCallback((value: string) => {
    setQuery(value)
    setPage(1)
  }, 300)

  useEffect(() => {
    const value = searchInput.trim()
    if (value === query) return
    debouncedQueryChange(value)
  }, [debouncedQueryChange, query, searchInput])

  const { data, isPending } = useBranchesPage(page, PAGE_SIZE, { q: query })
  const items = data?.items ?? []
  const total = data?.total ?? 0

  const openCreate = () => {
    setEditingBranch(null)
    setFormSession((s) => s + 1)
    setIsFormOpen(true)
  }

  const openEdit = (branch: Branch) => {
    setEditingBranch(branch)
    setFormSession((s) => s + 1)
    setIsFormOpen(true)
  }

  const handleToggleActive = async (branch: Branch) => {
    try {
      await setBranchActiveAction(branch.id, !branch.isActive)
      toast.success(branch.isActive ? t("deactivated") : t("activated"))
    } catch (e) {
      toast.error(resolveError(e) || t("saveFailed"))
    }
  }

  const handleDelete = async (branch: Branch) => {
    if (!confirm(t("deleteConfirmDescription"))) return
    try {
      await deleteBranchAction(branch.id)
      toast.success(t("deleted"))
    } catch (e) {
      toast.error(resolveError(e) || t("saveFailed"))
    }
  }

  const columns: TableColumn<Branch>[] = [
    {
      key: "name",
      header: t("name"),
      cell: (branch) => <span className="font-medium">{branch.name}</span>,
    },
    {
      key: "code",
      header: t("code"),
      cell: (branch) => <span dir="ltr">{branch.code}</span>,
    },
    {
      key: "address",
      header: t("address"),
      cell: (branch) => <span className="text-muted-foreground">{branch.address || "—"}</span>,
    },
    {
      key: "status",
      header: t("status"),
      cell: (branch) => (
        <span
          className={
            branch.isActive
              ? "inline-flex items-center rounded-full bg-emerald-500/10 px-2 py-0.5 text-xs font-medium text-emerald-600"
              : "inline-flex items-center rounded-full bg-destructive/10 px-2 py-0.5 text-xs font-medium text-destructive"
          }
        >
          {branch.isActive ? t("active") : t("inactive")}
        </span>
      ),
    },
    {
      key: "actions",
      header: t("actions"),
      headClassName: "w-40",
      cell: (branch) =>
        canManage && (
          <div className="flex items-center gap-1">
            <TooltipIconButton
              label={branch.isActive ? t("deactivate") : t("activate")}
              onClick={() => handleToggleActive(branch)}
            >
              {branch.isActive ? <CircleX className="h-4 w-4" /> : <CircleCheck className="h-4 w-4" />}
            </TooltipIconButton>
            <TooltipIconButton label={t("editBranch")} onClick={() => openEdit(branch)}>
              <Pencil className="h-4 w-4" />
            </TooltipIconButton>
            <TooltipIconButton label={t("setSecret")} onClick={() => setSecretBranch(branch)}>
              <KeyRound className="h-4 w-4" />
            </TooltipIconButton>
            <TooltipIconButton label={t("confirmDelete")} onClick={() => handleDelete(branch)}>
              <Trash className="h-4 w-4 text-destructive" />
            </TooltipIconButton>
          </div>
        ),
    },
  ]

  return (
    <div className="space-y-6">
      <div>
        <div className="flex items-center justify-between gap-4 mb-4">
          <p className="text-sm text-muted-foreground">{t("description")}</p>
          {canManage && (
            <Button onClick={openCreate}>
              <Plus className="mr-2 h-4 w-4" /> {t("newBranch")}
            </Button>
          )}
        </div>

        <Input
          placeholder={tc("search")}
          value={searchInput}
          onChange={(e) => setSearchInput(e.target.value)}
          className="max-w-sm mb-4"
        />

        <TableBuilder
          columns={columns}
          data={items}
          rowKey={(branch) => branch.id}
          loading={isPending}
          emptyMessage={query ? t("noResults") : t("empty")}
        />

        <DataPagination page={page} pageSize={PAGE_SIZE} total={total} onPageChange={setPage} />
      </div>

      {canManage && <BranchRuntimeCard />}

      <BranchFormDialog
        key={formSession}
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        branch={editingBranch}
      />

      <BranchSecretDialog
        branch={secretBranch}
        onOpenChange={(open) => !open && setSecretBranch(null)}
      />
    </div>
  )
}
