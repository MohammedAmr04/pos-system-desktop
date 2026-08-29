"use client"

import { useState } from "react"
import { useSearchParams } from "next/navigation"
import { useTranslations } from "next-intl"
import { ArrowRight, Eye, Loader2 } from "lucide-react"
import { useShiftReport, useShiftInvoices } from "@/hooks/use-shifts"
import { useInvoice } from "@/hooks/use-invoices"
import { useRouter } from "@/i18n/navigation"
import { useAuth } from "@/components/common/auth-context"
import { PERMISSIONS } from "@/lib/constants"
import { AccessDenied } from "@/components/common/access-denied"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { TableColumn, TableBuilder } from "@/components/common/table-builder"
import { DataPagination } from "@/components/common/data-pagination"
import { TooltipIconButton } from "@/components/common/tooltip-icon-button"
import { InvoiceDetailsDialog } from "@/components/common/invoice-details-dialog"
import { Invoice } from "@/types/domain/domain.types"

const PAGE_SIZE = 20

function StatusBadge({ status, t }: { status?: string; t: (key: string) => string }) {
  const key = status === 'draft' ? 'draft' : status === 'cancelled' ? 'cancelled' : 'posted'
  const cls =
    key === 'posted'
      ? "bg-emerald-500/10 text-emerald-600"
      : key === 'cancelled'
        ? "bg-destructive/10 text-destructive"
        : "bg-amber-500/10 text-amber-600"
  return (
    <span className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ${cls}`}>
      {t(key)}
    </span>
  )
}

function ShiftStatusBadge({ status, t }: { status: string; t: (key: string) => string }) {
  const open = status === "open"
  return (
    <span
      className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ${
        open ? "bg-emerald-500/10 text-emerald-600" : "bg-slate-500/10 text-slate-600"
      }`}
    >
      {t(open ? "open" : "closed")}
    </span>
  )
}

export function ShiftInvoicesClient() {
  const t = useTranslations("Shifts")
  const ti = useTranslations("Invoices")
  const tp = useTranslations("Payments")
  const tc = useTranslations("Common")
  const router = useRouter()
  const { hasPermission } = useAuth()
  const canView = hasPermission(PERMISSIONS.SHIFTS_VIEW)

  const searchParams = useSearchParams()
  const shiftId = searchParams.get("id") ?? ""

  const [page, setPage] = useState(1)
  const [selectedId, setSelectedId] = useState<string | null>(null)

  const { data: report, isPending: reportPending } = useShiftReport(shiftId, !!shiftId)
  const { data: paged, isPending: invoicesPending } = useShiftInvoices(shiftId, page, PAGE_SIZE, !!shiftId)
  const { data: fullInvoice } = useInvoice(selectedId ?? "", !!selectedId)

  const invoices = paged?.items ?? []
  const total = paged?.total ?? 0
  const shift = report?.shift ?? null

  if (!canView) return <AccessDenied />

  const money = (v: number | null | undefined) => (v ?? 0).toFixed(2)
  const fmtDate = (v: string | null | undefined) =>
    v ? new Date(v).toLocaleString() : "—"

  const columns: TableColumn<Invoice>[] = [
    {
      key: "invoiceNumber",
      header: ti("invoiceNumber"),
      cell: (inv) => <span className="font-medium">#{inv.invoiceNumber}</span>,
    },
    {
      key: "createdAt",
      header: ti("time"),
      cell: (inv) => <span dir="ltr" className="text-muted-foreground">{new Date(inv.createdAt).toLocaleTimeString()}</span>,
    },
    {
      key: "status",
      header: ti("status"),
      cell: (inv) => {
        const rs = inv.returnStatus
        return (
          <div className="flex items-center gap-1">
            <StatusBadge status={inv.status} t={ti} />
            {(rs === 'partial' || rs === 'full') && (
              <span className="inline-flex items-center rounded-full bg-orange-500/10 px-2 py-0.5 text-xs font-medium text-orange-600">
                {ti(rs === 'full' ? "fullyReturned" : "partiallyReturned")}
              </span>
            )}
          </div>
        )
      },
    },
    {
      key: "client",
      header: ti("client"),
      cell: (inv) => inv.client?.name || ti("walkIn"),
    },
    {
      key: "paymentMethod",
      header: ti("payment"),
      cell: (inv) => {
        const m = inv.paymentMethod
        const label =
          m === 'credit' ? ti("credit")
            : m === 'card' ? tp("methodCard")
              : m === 'bank_transfer' ? tp("methodBankTransfer")
                : tp("methodCash")
        const cls = m === 'credit' ? "bg-sky-500/10 text-sky-600" : "bg-slate-500/10 text-slate-600"
        return (
          <span className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ${cls}`}>
            {label}
          </span>
        )
      },
    },
    {
      key: "discount",
      header: ti("discount"),
      cell: (inv) => `${inv.discount.toFixed(2)}${inv.discountType === 'percentage' ? '%' : ''}`,
    },
    {
      key: "totalAmount",
      header: ti("total"),
      cell: (inv) => inv.totalAmount.toFixed(2),
    },
    {
      key: "actions",
      header: ti("actions"),
      headClassName: "w-16",
      cell: (inv) => (
        <div className="flex items-center gap-1">
          <TooltipIconButton label={ti("details")} onClick={() => setSelectedId(inv.id)}>
            <Eye className="h-4 w-4" />
          </TooltipIconButton>
        </div>
      ),
    },
  ]

  return (
    <div className="flex-1 space-y-4 pt-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex items-center gap-2">
          <TooltipIconButton label={t("invoicesBack")} onClick={() => router.push("/shifts")}>
            <ArrowRight className="h-4 w-4" />
          </TooltipIconButton>
          <h2 className="text-3xl font-bold tracking-tight">{t("invoicesOfShift", { number: shift?.number ?? 0 })}</h2>
        </div>
        {shift && <ShiftStatusBadge status={shift.status} t={t} />}
      </div>

      {reportPending || !report ? (
        <div className="py-8 text-center"><Loader2 className="mx-auto h-6 w-6 animate-spin" /></div>
      ) : (
        <>
          <div className="flex flex-wrap gap-x-8 gap-y-1 text-sm text-muted-foreground">
            <div className="flex items-center gap-1"><span className="text-foreground">{t("openedBy")}:</span><span>{report.shift.openedBy}</span></div>
            <div className="flex items-center gap-1"><span className="text-foreground">{t("openedAt")}:</span><span dir="ltr">{fmtDate(report.shift.openedAt)}</span></div>
            <div className="flex items-center gap-1"><span className="text-foreground">{t("closedAt")}:</span><span dir="ltr">{fmtDate(report.shift.closedAt)}</span></div>
          </div>

          <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">{t("reportTitle", { number: report.shift.number })}</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="grid grid-cols-2 gap-x-6 gap-y-1 rounded-md border p-3 text-sm md:grid-cols-3">
                <div className="flex justify-between"><span>{t("openingCash")}</span><span>{money(report.openingCash)}</span></div>
                <div className="flex justify-between"><span>{t("cashSales")}</span><span>+{money(report.cashSales)}</span></div>
                <div className="flex justify-between"><span>{t("saleRefunds")}</span><span>−{money(report.saleRefunds)}</span></div>
                <div className="flex justify-between"><span>{t("otherCashIn")}</span><span>+{money(report.otherCashIn)}</span></div>
                <div className="flex justify-between"><span>{t("supplierPaymentsOut")}</span><span>−{money(report.supplierPaymentsOut)}</span></div>
                <div className="flex justify-between"><span>{t("supplierRefundsIn")}</span><span>+{money(report.supplierRefundsIn)}</span></div>
                <div className="col-span-2 flex justify-between border-t pt-1 font-bold md:col-span-3">
                  <span>{t("expectedCash")}</span><span>{money(report.expectedCash)}</span>
                </div>
                {report.shift.countedCash != null && (
                  <>
                    <div className="flex justify-between"><span>{t("countedCash")}</span><span>{money(report.shift.countedCash)}</span></div>
                    <div className={`flex justify-between font-bold ${(report.shift.difference ?? 0) >= 0 ? "text-emerald-600" : "text-destructive"}`}>
                      <span>{t("difference")}</span><span>{money(report.shift.difference)}</span>
                    </div>
                  </>
                )}
              </div>
            </CardContent>
          </Card>
        </>
      )}

      <TableBuilder
        columns={columns}
        data={invoices}
        rowKey={(inv) => inv.id}
        loading={invoicesPending}
        emptyMessage={tc("noResults")}
      />

      <DataPagination page={page} pageSize={PAGE_SIZE} total={total} onPageChange={setPage} />

      <InvoiceDetailsDialog
        open={!!selectedId && !!fullInvoice}
        onOpenChange={(open) => {
          if (!open) setSelectedId(null)
        }}
        invoice={fullInvoice ?? null}
      />
    </div>
  )
}