"use client"

import { useState } from "react"
import { useQueryClient } from "@tanstack/react-query"
import { useTranslations } from "next-intl"
import { Shift } from "@/types/domain/domain.types"
import { shiftsKeys, useActiveShift, useShiftsPage } from "@/hooks/use-shifts"
import { useAuth } from "@/components/common/auth-context"
import { PERMISSIONS } from "@/lib/constants"
import { AccessDenied } from "@/components/common/access-denied"
import { Button } from "@/components/ui/button"
import { TableColumn, TableBuilder } from "@/components/common/table-builder"
import { DataPagination } from "@/components/common/data-pagination"
import { TooltipIconButton } from "@/components/common/tooltip-icon-button"
import { Eye, PlayCircle, StopCircle } from "lucide-react"
import { ShiftOpenDialog } from "./_components/shift-open-dialog"
import { ShiftCloseDialog } from "./_components/shift-close-dialog"
import { ShiftReportDialog } from "./_components/shift-report-dialog"

const PAGE_SIZE = 20

export function ShiftsClient() {
  const t = useTranslations("Shifts")
  const tc = useTranslations("Common")
  const queryClient = useQueryClient()
  const { hasPermission } = useAuth()
  const canView = hasPermission(PERMISSIONS.SHIFTS_VIEW)
  const canOpen = hasPermission(PERMISSIONS.SHIFTS_OPEN)
  const canClose = hasPermission(PERMISSIONS.SHIFTS_CLOSE)

  const [page, setPage] = useState(1)
  const [openDialog, setOpenDialog] = useState(false)
  const [closeTarget, setCloseTarget] = useState<Shift | null>(null)
  const [reportFor, setReportFor] = useState<Shift | null>(null)

  const { data: paged, isPending } = useShiftsPage(page, PAGE_SIZE)
  const shifts = paged?.items ?? []
  const total = paged?.total ?? 0
  const { data: activeShift = null } = useActiveShift()

  if (!canView) return <AccessDenied />

  const refresh = () => queryClient.invalidateQueries({ queryKey: shiftsKeys.all })

  const money = (v: number | null | undefined) => (v ?? 0).toFixed(2)

  const statusBadge = (s: string) =>
    s === "open" ? (
      <span className="inline-flex items-center rounded-full bg-emerald-500/10 px-2 py-0.5 text-xs font-medium text-emerald-600">
        {t("open")}
      </span>
    ) : (
      <span className="inline-flex items-center rounded-full bg-slate-500/10 px-2 py-0.5 text-xs font-medium text-slate-600">
        {t("closed")}
      </span>
    )

  const columns: TableColumn<Shift>[] = [
    {
      key: "number",
      header: "#",
      cell: (s) => <span className="font-medium">#{s.number}</span>,
    },
    {
      key: "openedBy",
      header: t("openedBy"),
      cell: (s) => s.openedBy,
    },
    {
      key: "openedAt",
      header: t("openedAt"),
      cell: (s) => (
        <span dir="ltr" className="text-muted-foreground">{new Date(s.openedAt).toLocaleString()}</span>
      ),
    },
    {
      key: "closedAt",
      header: t("closedAt"),
      cell: (s) => (
        <span dir="ltr" className="text-muted-foreground">
          {s.closedAt ? new Date(s.closedAt).toLocaleString() : "—"}
        </span>
      ),
    },
    {
      key: "openingCash",
      header: t("openingCash"),
      cell: (s) => money(s.openingCash),
    },
    {
      key: "expectedCash",
      header: t("expectedCash"),
      cell: (s) => money(s.expectedCash),
    },
    {
      key: "countedCash",
      header: t("countedCash"),
      cell: (s) => (s.countedCash == null ? "—" : money(s.countedCash)),
    },
    {
      key: "difference",
      header: t("difference"),
      cell: (s) =>
        s.difference == null ? (
          "—"
        ) : (
          <span className={s.difference === 0 ? "" : s.difference > 0 ? "text-emerald-600" : "text-destructive"}>
            {money(s.difference)}
          </span>
        ),
    },
    {
      key: "status",
      header: t("status"),
      cell: (s) => statusBadge(s.status),
    },
    {
      key: "actions",
      header: t("actions"),
      headClassName: "w-24",
      cell: (s) => (
        <div className="flex items-center gap-1">
          <TooltipIconButton label={t("details")} onClick={() => setReportFor(s)}>
            <Eye className="h-4 w-4" />
          </TooltipIconButton>
          {s.status === "open" && canClose && (
            <TooltipIconButton label={t("closeShift")} onClick={() => setCloseTarget(s)}>
              <StopCircle className="h-4 w-4" />
            </TooltipIconButton>
          )}
        </div>
      ),
    },
  ]

  return (
    <div className="flex-1 space-y-4 pt-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h2 className="text-3xl font-bold tracking-tight">{t("title")}</h2>
        {canOpen && !activeShift && (
          <Button onClick={() => setOpenDialog(true)}>
            <PlayCircle className="ml-2 h-4 w-4" />
            {t("openShift")}
          </Button>
        )}
        {activeShift && (
          <div className="flex items-center gap-2 rounded-lg border border-emerald-500/30 bg-emerald-500/10 px-3 py-1.5 text-sm font-medium text-emerald-700">
            <span className="h-2 w-2 animate-pulse rounded-full bg-emerald-500" />
            {t("activeSince", { number: activeShift.number })}
            {canClose && (
              <Button variant="outline" size="sm" onClick={() => setCloseTarget(activeShift)}>
                <StopCircle className="ml-1 h-4 w-4" />
                {t("closeShift")}
              </Button>
            )}
          </div>
        )}
      </div>

      <TableBuilder
        columns={columns}
        data={shifts}
        rowKey={(s) => s.id}
        loading={isPending}
        emptyMessage={tc("noResults")}
      />

      <DataPagination page={page} pageSize={PAGE_SIZE} total={total} onPageChange={setPage} />

      <ShiftOpenDialog
        open={openDialog}
        onOpenChange={setOpenDialog}
        onOpened={refresh}
      />

      <ShiftCloseDialog
        shift={closeTarget}
        onOpenChange={(open) => {
          if (!open) setCloseTarget(null)
        }}
        onClosed={refresh}
      />

      <ShiftReportDialog
        shift={reportFor}
        onOpenChange={(open) => {
          if (!open) setReportFor(null)
        }}
      />
    </div>
  )
}
