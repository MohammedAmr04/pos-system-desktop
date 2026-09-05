"use client"

import { useEffect, useState } from "react"
import { useTranslations } from "next-intl"
import { useApiError } from "@/lib/api-error"
import { toast } from "sonner"
import { Eye, Pencil, Upload, CircleX, Undo2 } from "lucide-react"
import { format, startOfWeek, endOfWeek, startOfMonth, endOfMonth } from "date-fns"
import { Invoice } from "@/types/domain/domain.types"
import { useInvoicesPage, useInvoice } from "@/hooks/use-invoices"
import { postInvoice, cancelInvoice } from "@/actions/invoices.lifecycle.actions"
import { useDebouncedCallback } from "@/hooks/use-debounced-callback"
import { useRouter } from "@/i18n/navigation"
import { useAuth } from "@/components/common/auth-context"
import { PERMISSIONS } from "@/lib/constants"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { DatePicker } from "@/components/ui/date-picker"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { TableColumn, TableBuilder } from "@/components/common/table-builder"
import { DataPagination } from "@/components/common/data-pagination"
import { TooltipIconButton } from "@/components/common/tooltip-icon-button"
import { InvoiceDetailsDialog } from "@/components/common/invoice-details-dialog"

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

type QuickFilter = "today" | "thisWeek" | "thisMonth" | "all"

export function InvoicesClient() {
  const t = useTranslations("Invoices")
  const tc = useTranslations("Common")
  const tp = useTranslations("Payments")
  const resolveError = useApiError()
  const router = useRouter()
  const { hasPermission } = useAuth()
  const canCreateReturns = hasPermission(PERMISSIONS.INVOICES_RETURN)

  const [page, setPage] = useState(1)
  const [searchInput, setSearchInput] = useState("")
  const [query, setQuery] = useState("")
  const [fromDate, setFromDate] = useState<Date | undefined>(undefined)
  const [toDate, setToDate] = useState<Date | undefined>(undefined)
  const [allRange, setAllRange] = useState(false)
  const [status, setStatus] = useState("all")

  const debouncedQueryChange = useDebouncedCallback((value: string) => {
    setQuery(value)
    setPage(1)
  }, 300)

  useEffect(() => {
    const value = searchInput.trim()
    if (value === query) return
    debouncedQueryChange(value)
  }, [debouncedQueryChange, query, searchInput])

  const filter = {
    from: fromDate ? format(fromDate, "yyyy-MM-dd") : undefined,
    to: toDate ? format(toDate, "yyyy-MM-dd") : undefined,
    q: query.trim() || undefined,
    range: allRange ? "all" as const : undefined,
    status,
  }

  const { data, isPending } = useInvoicesPage(page, PAGE_SIZE, filter)
  const items = data?.items ?? []
  const total = data?.total ?? 0
  const totals = data?.totals ?? { revenue: 0, discounts: 0 }

  const [selectedId, setSelectedId] = useState<string | null>(null)
  const { data: fullInvoice } = useInvoice(selectedId ?? "", !!selectedId)

  const handlePost = async (invoice: Invoice) => {
    if (!window.confirm(t("confirmPost"))) return
    try {
      await postInvoice(invoice.id)
      toast.success(t("postedDone"))
    } catch (e) {
      toast.error(resolveError(e) || t("saveFailed"))
    }
  }

  const handleCancelInvoice = async (invoice: Invoice) => {
    if (!window.confirm(t("confirmCancel"))) return
    try {
      await cancelInvoice(invoice.id)
      toast.success(t("cancelledDone"))
    } catch (e) {
      toast.error(resolveError(e) || t("saveFailed"))
    }
  }

  const columns: TableColumn<Invoice>[] = [
    {
      key: "invoiceNumber",
      header: t("invoiceNumber"),
      cell: (inv) => inv.invoiceNumber,
    },
    {
      key: "createdAt",
      header: t("time"),
      cell: (inv) => new Date(inv.createdAt).toLocaleTimeString(),
    },
    {
      key: "status",
      header: t("status"),
      cell: (inv) => {
        const rs = inv.returnStatus
        return (
          <div className="flex items-center gap-1">
            <StatusBadge status={inv.status} t={t} />
            {(rs === 'partial' || rs === 'full') && (
              <span className="inline-flex items-center rounded-full bg-orange-500/10 px-2 py-0.5 text-xs font-medium text-orange-600">
                {t(rs === 'full' ? "fullyReturned" : "partiallyReturned")}
              </span>
            )}
          </div>
        )
      },
    },
    {
      key: "client",
      header: <span className="block text-center">{t("client")}</span>,
      className: "text-center",
      cell: (inv) => inv.client?.name || t("walkIn"),
    },
    {
      key: "paymentMethod",
      header: <span className="block text-center">{t("payment")}</span>,
      className: "text-center",
      cell: (inv) => {
        const m = inv.paymentMethod
        const label =
          m === 'credit' ? t("credit")
            : m === 'card' ? tp("methodCard")
              : m === 'bank_transfer' ? tp("methodBankTransfer")
                : tp("methodCash")
        const cls =
          m === 'credit'
            ? "bg-sky-500/10 text-sky-600"
            : "bg-slate-500/10 text-slate-600"
        return (
          <span className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ${cls}`}>
            {label}
          </span>
        )
      },
    },
    {
      key: "totalAmount",
      header: t("total"),
      cell: (inv) => inv.totalAmount.toFixed(2),
    },
    {
      key: "discount",
      header: t("discount"),
      cell: (inv) => {
        const type = inv.discountType === 'percentage' ? '%' : ''
        return `${inv.discount.toFixed(2)}${type}`
      },
    },
    {
      key: "actions",
      header: t("actions"),
      headClassName: "w-44",
      cell: (inv) => {
        const s = inv.status ?? 'posted'
        return (
          <div className="flex items-center gap-1">
            <TooltipIconButton label={t("details")} onClick={() => setSelectedId(inv.id)}>
              <Eye className="h-4 w-4" />
            </TooltipIconButton>
            {s === 'draft' && (
              <>
                <TooltipIconButton
                  label={t("resumeDraft")}
                  onClick={() => router.push(`/pos/?draft=${inv.id}`)}
                >
                  <Pencil className="h-4 w-4" />
                </TooltipIconButton>
                <TooltipIconButton label={t("post")} onClick={() => handlePost(inv)}>
                  <Upload className="h-4 w-4" />
                </TooltipIconButton>
              </>
            )}
            {s === 'posted' && canCreateReturns && (
              <TooltipIconButton
                label={t("makeReturn")}
                onClick={() => router.push(`/returns/?invoice=${inv.id}`)}
              >
                <Undo2 className="h-4 w-4" />
              </TooltipIconButton>
            )}
            {s === 'posted' && (
              <TooltipIconButton label={tp("cancel")} onClick={() => handleCancelInvoice(inv)}>
                <CircleX className="h-4 w-4" />
              </TooltipIconButton>
            )}
          </div>
        )
      },
    },
  ]

  const handleDateChange = (from?: Date, to?: Date, range?: "all") => {
    setFromDate(from)
    setToDate(to)
    setAllRange(range === "all")
    setPage(1)
  }

  const quickFilter = (preset: QuickFilter) => {
    const now = new Date()
    switch (preset) {
      case "today":
        handleDateChange(now, undefined)
        break
      case "thisWeek": {
        const weekStart = startOfWeek(now, { weekStartsOn: 6 })
        const weekEnd = endOfWeek(now, { weekStartsOn: 6 })
        handleDateChange(weekStart, weekEnd)
        break
      }
      case "thisMonth": {
        const monthStart = startOfMonth(now)
        const monthEnd = endOfMonth(now)
        handleDateChange(monthStart, monthEnd)
        break
      }
      case "all":
        handleDateChange(undefined, undefined, "all")
        break
    }
  }

  return (
    <>
      <div className="flex flex-wrap items-center gap-3 mb-4">
        <Input
          placeholder={t("searchByNumber")}
          value={searchInput}
          onChange={(e) => setSearchInput(e.target.value)}
          className="max-w-sm"
        />
        <DatePicker
          value={fromDate}
          onChange={(date) => handleDateChange(date, toDate)}
          placeholder={t("fromDate")}
        />
        <span className="text-muted-foreground text-sm">-</span>
        <DatePicker
          value={toDate}
          onChange={(date) => handleDateChange(fromDate, date)}
          placeholder={t("toDate")}
        />
        <Select
          value={status}
          onValueChange={(v) => {
            if (v == null) return
            setStatus(v)
            setPage(1)
          }}
          items={{
            all: t("statusAll"),
            draft: t("draft"),
            posted: t("posted"),
            cancelled: t("cancelled"),
          }}
        >
          <SelectTrigger aria-label={t("status")} className="w-[140px]">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">{t("statusAll")}</SelectItem>
            <SelectItem value="draft">{t("draft")}</SelectItem>
            <SelectItem value="posted">{t("posted")}</SelectItem>
            <SelectItem value="cancelled">{t("cancelled")}</SelectItem>
          </SelectContent>
        </Select>
        <div className="flex gap-2 mr-auto">
          <Button variant="outline" size="sm" onClick={() => quickFilter("today")}>
            {t("today")}
          </Button>
          <Button variant="outline" size="sm" onClick={() => quickFilter("thisWeek")}>
            {t("thisWeek")}
          </Button>
          <Button variant="outline" size="sm" onClick={() => quickFilter("thisMonth")}>
            {t("thisMonth")}
          </Button>
          <Button variant="outline" size="sm" onClick={() => quickFilter("all")}>
            {t("all")}
          </Button>
        </div>
      </div>

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4 mb-4">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">{t("totalRevenue")}</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{totals.revenue.toFixed(2)}</div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">{t("totalDiscounts")}</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{totals.discounts.toFixed(2)}</div>
          </CardContent>
        </Card>
      </div>

      <TableBuilder
        columns={columns}
        data={items}
        rowKey={(inv) => inv.id}
        loading={isPending}
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
    </>
  )
}
