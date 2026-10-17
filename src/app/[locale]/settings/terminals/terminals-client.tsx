"use client"

import { useState } from "react"
import { useTranslations } from "next-intl"
import { useApiError } from "@/lib/api-error"
import { toast } from "sonner"
import { CircleCheck, CircleX, History, Pencil, Plus, Repeat } from "lucide-react"

import { Terminal } from "@/types/domain/domain.types"
import { useTerminalsPage } from "@/hooks/use-terminals"
import { useAllBranches } from "@/hooks/use-branches"
import { setTerminalActiveAction } from "@/actions/terminals.actions"
import { useAuth } from "@/components/common/auth-context"
import { PERMISSIONS } from "@/lib/constants"
import { Button } from "@/components/ui/button"
import { TableColumn, TableBuilder } from "@/components/common/table-builder"
import { DataPagination } from "@/components/common/data-pagination"
import { TooltipIconButton } from "@/components/common/tooltip-icon-button"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { TerminalFormDialog } from "./_components/terminal-form-dialog"
import { TerminalSwitchDialog } from "./_components/terminal-switch-dialog"
import { TerminalHistoryDialog } from "./_components/terminal-history-dialog"

const PAGE_SIZE = 20
const ALL = "__all__"

function formatDate(value: string | null) {
  return value ? new Date(value).toLocaleString(undefined, { dateStyle: "short", timeStyle: "short" }) : "—"
}

export function TerminalsClient() {
  const t = useTranslations("TerminalsAdmin")
  const resolveError = useApiError()
  const { hasPermission } = useAuth()
  const canManage = hasPermission(PERMISSIONS.BRANCHES_MANAGE)

  const branches = useAllBranches(false)
  const branchName = (id: string) => branches.data?.find((branch) => branch.id === id)?.name ?? id

  const [page, setPage] = useState(1)
  const [branchFilter, setBranchFilter] = useState("")
  const [isFormOpen, setIsFormOpen] = useState(false)
  const [editing, setEditing] = useState<Terminal | null>(null)
  const [switching, setSwitching] = useState<Terminal | null>(null)
  const [historyOf, setHistoryOf] = useState<Terminal | null>(null)
  const [formSession, setFormSession] = useState(0)

  const { data, isPending } = useTerminalsPage(page, PAGE_SIZE, { branchId: branchFilter || undefined })
  const items = data?.items ?? []
  const total = data?.total ?? 0

  const openCreate = () => {
    setEditing(null)
    setFormSession((s) => s + 1)
    setIsFormOpen(true)
  }

  const openEdit = (terminal: Terminal) => {
    setEditing(terminal)
    setFormSession((s) => s + 1)
    setIsFormOpen(true)
  }

  const handleToggleActive = async (terminal: Terminal) => {
    try {
      await setTerminalActiveAction(terminal.id, !terminal.isActive)
      toast.success(terminal.isActive ? t("deactivated") : t("activated"))
    } catch (e) {
      toast.error(resolveError(e) || t("saveFailed"))
    }
  }

  const columns: TableColumn<Terminal>[] = [
    {
      key: "name",
      header: t("name"),
      cell: (terminal) => <span className="font-medium">{terminal.name}</span>,
    },
    {
      key: "branch",
      header: t("branch"),
      cell: (terminal) => <span>{branchName(terminal.branchId)}</span>,
    },
    {
      key: "status",
      header: t("status"),
      cell: (terminal) => (
        <span
          className={
            terminal.isActive
              ? "inline-flex items-center rounded-full bg-emerald-500/10 px-2 py-0.5 text-xs font-medium text-emerald-600"
              : "inline-flex items-center rounded-full bg-destructive/10 px-2 py-0.5 text-xs font-medium text-destructive"
          }
        >
          {terminal.isActive ? t("active") : t("inactive")}
        </span>
      ),
    },
    {
      key: "lastSeen",
      header: t("lastSeen"),
      cell: (terminal) => <span className="text-muted-foreground" dir="ltr">{formatDate(terminal.lastSeenAt)}</span>,
    },
    {
      key: "actions",
      header: t("actions"),
      headClassName: "w-44",
      cell: (terminal) => (
        <div className="flex items-center gap-1">
          <TooltipIconButton label={t("historyTitle")} onClick={() => setHistoryOf(terminal)}>
            <History className="h-4 w-4" />
          </TooltipIconButton>
          {canManage && (
            <>
              <TooltipIconButton label={t("switchBranch")} onClick={() => setSwitching(terminal)}>
                <Repeat className="h-4 w-4" />
              </TooltipIconButton>
              <TooltipIconButton
                label={terminal.isActive ? t("deactivate") : t("activate")}
                onClick={() => handleToggleActive(terminal)}
              >
                {terminal.isActive ? <CircleX className="h-4 w-4" /> : <CircleCheck className="h-4 w-4" />}
              </TooltipIconButton>
              <TooltipIconButton label={t("editTerminal")} onClick={() => openEdit(terminal)}>
                <Pencil className="h-4 w-4" />
              </TooltipIconButton>
            </>
          )}
        </div>
      ),
    },
  ]

  return (
    <>
      <div className="flex items-center justify-between gap-4 mb-4">
        <p className="text-sm text-muted-foreground">{t("description")}</p>
        {canManage && (
          <Button onClick={openCreate}>
            <Plus className="mr-2 h-4 w-4" /> {t("newTerminal")}
          </Button>
        )}
      </div>

      <div className="mb-4 max-w-xs">
        <Select value={branchFilter || ALL} onValueChange={(value) => { setBranchFilter(!value || value === ALL ? "" : value); setPage(1) }}>
          <SelectTrigger className="w-full">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value={ALL}>{t("allBranches")}</SelectItem>
            {(branches.data ?? []).map((branch) => (
              <SelectItem key={branch.id} value={branch.id}>
                {branch.name}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      <TableBuilder
        columns={columns}
        data={items}
        rowKey={(terminal) => terminal.id}
        loading={isPending}
        emptyMessage={t("empty")}
      />

      <DataPagination page={page} pageSize={PAGE_SIZE} total={total} onPageChange={setPage} />

      <TerminalFormDialog
        key={formSession}
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        terminal={editing}
        defaultBranchId={branchFilter}
      />

      <TerminalSwitchDialog
        terminal={switching}
        onOpenChange={(open) => !open && setSwitching(null)}
      />

      <TerminalHistoryDialog
        terminal={historyOf}
        onOpenChange={(open) => !open && setHistoryOf(null)}
      />
    </>
  )
}
