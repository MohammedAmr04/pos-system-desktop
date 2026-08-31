"use client"

import { useEffect, useState } from "react"
import { useQueryClient } from "@tanstack/react-query"
import { useDebouncedCallback } from "@/hooks/use-debounced-callback"
import { useTranslations } from "next-intl"
import { useApiError } from "@/lib/api-error"
import { toast } from "sonner"
import { Eye, Pencil, CircleX, Upload, Undo2, Plus } from "lucide-react"

import { PurchaseInvoice } from "@/types/domain/domain.types"
import { purchasesKeys, usePurchase, usePurchasesPage } from "@/hooks/use-purchases"
import { postPurchase, cancelPurchase } from "@/actions/purchases.actions"
import { useRouter } from "@/i18n/navigation"
import { useAuth } from "@/components/common/auth-context"
import { PERMISSIONS } from "@/lib/constants"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
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
import { PurchaseDetailsDialog } from "./_components/purchase-details-dialog"

const PAGE_SIZE = 20

export function PurchasesClient() {
  const t = useTranslations("Purchases")
  const tc = useTranslations("Common")
  const resolveError = useApiError()
  const router = useRouter()
  const queryClient = useQueryClient()
  const { hasPermission } = useAuth()
  const canCreate = hasPermission(PERMISSIONS.PURCHASES_CREATE)
  const canUpdate = hasPermission(PERMISSIONS.PURCHASES_UPDATE)
  const canCreateReturns = hasPermission(PERMISSIONS.PURCHASES_RETURN)

  const [page, setPage] = useState(1)
  const [searchInput, setSearchInput] = useState("")
  const [query, setQuery] = useState("")
  const [status, setStatus] = useState("all")
  const [detailsId, setDetailsId] = useState<string | null>(null)
  const { data: fullPurchase } = usePurchase(detailsId ?? "", !!detailsId)

  const debouncedQueryChange = useDebouncedCallback((value: string) => {
    setQuery(value)
    setPage(1)
  }, 300)

  useEffect(() => {
    const value = searchInput.trim()
    if (value === query) return
    debouncedQueryChange(value)
  }, [debouncedQueryChange, query, searchInput])

  const filter = { status, q: query.trim() || undefined }

  const { data, isPending } = usePurchasesPage(page, PAGE_SIZE, filter)
  const items = data?.items ?? []
  const total = data?.total ?? 0
  const postedTotal = data?.postedTotal ?? 0

  const refresh = () => queryClient.invalidateQueries({ queryKey: purchasesKeys.all })

  const handlePost = async (invoice: PurchaseInvoice) => {
    if (!window.confirm(t("confirmPost"))) return
    try {
      await postPurchase(invoice.id)
      toast.success(t("posted"))
      await refresh()
    } catch (e) {
      toast.error(resolveError(e) || t("saveFailed"))
    }
  }

  const handleCancelInvoice = async (invoice: PurchaseInvoice) => {
    if (!window.confirm(t("confirmCancel"))) return
    try {
      await cancelPurchase(invoice.id)
      toast.success(t("cancelledDone"))
      await refresh()
    } catch (e) {
      toast.error(resolveError(e) || t("saveFailed"))
    }
  }

  const statusBadge = (s: string) => {
    const cls =
      s === "posted"
        ? "bg-emerald-500/10 text-emerald-600"
        : s === "cancelled"
          ? "bg-destructive/10 text-destructive"
          : "bg-amber-500/10 text-amber-600"
    const label = s === "posted" ? t("posted") : s === "cancelled" ? t("cancelled") : t("draft")
    return (
      <span className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ${cls}`}>
        {label}
      </span>
    )
  }

  const columns: TableColumn<PurchaseInvoice>[] = [
    {
      key: "invoiceNumber",
      header: t("invoiceNumber"),
      cell: (inv) => <span className="font-medium">#{inv.invoiceNumber}</span>,
    },
    {
      key: "supplier",
      header: t("supplier"),
      cell: (inv) => inv.supplier?.name ?? "—",
    },
    {
      key: "date",
      header: t("date"),
      cell: (inv) => (
        <span dir="ltr" className="text-muted-foreground">
          {new Date(inv.date).toLocaleDateString()}
        </span>
      ),
    },
    {
      key: "paymentMethod",
      header: t("paymentMethod"),
      cell: (inv) => (inv.paymentMethod === "credit" ? t("credit") : t("cash")),
    },
    {
      key: "total",
      header: t("total"),
      cell: (inv) => <span className="font-medium">{inv.total.toFixed(2)}</span>,
    },
    {
      key: "status",
      header: t("status"),
      cell: (inv) => (
        <div className="flex items-center justify-center gap-1">
          {statusBadge(inv.status)}
          {(inv.returnStatus === 'partial' || inv.returnStatus === 'full') && (
            <span className="inline-flex items-center rounded-full bg-orange-500/10 px-2 py-0.5 text-xs font-medium text-orange-600">
              {t(inv.returnStatus === 'full' ? "fullyReturned" : "partiallyReturned")}
            </span>
          )}
        </div>
      ),
    },
    {
      key: "actions",
      header: t("actions"),
      headClassName: "w-44",
      cell: (inv) => (
        <div className="flex items-center gap-1">
          <TooltipIconButton
            label={t("details")}
            variant="ghost"
            size="icon"
            onClick={() => setDetailsId(inv.id)}
          >
            <Eye className="h-4 w-4" />
          </TooltipIconButton>
          {canUpdate && inv.status === "draft" && (
            <TooltipIconButton label={t("post")} variant="ghost" size="icon" onClick={() => handlePost(inv)}>
              <Upload className="h-4 w-4" />
            </TooltipIconButton>
          )}
          {canCreate && inv.status !== "cancelled" && (
            <TooltipIconButton
              label={t("editPurchase")}
              variant="ghost"
              size="icon"
              onClick={() => router.push(`/purchases/editor?id=${inv.id}`)}
            >
              <Pencil className="h-4 w-4" />
            </TooltipIconButton>
          )}
          {canUpdate && inv.status === "posted" && (
            <TooltipIconButton label={t("cancelInvoice")} variant="ghost" size="icon" onClick={() => handleCancelInvoice(inv)}>
              <CircleX className="h-4 w-4" />
            </TooltipIconButton>
          )}
          {inv.status === 'posted' && canCreateReturns && (
            <TooltipIconButton
              label={t("makeReturn")}
              variant="ghost"
              size="icon"
              onClick={() => router.push(`/returns/?tab=purchase&purchase=${inv.id}`)}
            >
              <Undo2 className="h-4 w-4" />
            </TooltipIconButton>
          )}
        </div>
      ),
    },
  ]

  return (
    <>
      <div className="flex items-center justify-between gap-4 mb-4">
        <p className="text-sm text-muted-foreground">{t("description")}</p>
        {canCreate && (
          <Button onClick={() => router.push("/purchases/editor")}>
            <Plus className="mr-2 h-4 w-4" /> {t("newPurchase")}
          </Button>
        )}
      </div>

      <div className="flex items-center gap-3 mb-4">
        <Input
          placeholder={`${tc("search")} #`}
          value={searchInput}
          onChange={(e) => setSearchInput(e.target.value)}
          className="max-w-sm"
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
          <SelectTrigger aria-label={t("status")} className="w-[160px]">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">{t("statusAll")}</SelectItem>
            <SelectItem value="draft">{t("draft")}</SelectItem>
            <SelectItem value="posted">{t("posted")}</SelectItem>
            <SelectItem value="cancelled">{t("cancelled")}</SelectItem>
          </SelectContent>
        </Select>
        <span className="text-sm text-muted-foreground whitespace-nowrap">
          {`${postedTotal.toFixed(2)}`}
        </span>
      </div>

      <TableBuilder
        columns={columns}
        data={items}
        rowKey={(inv) => inv.id}
        loading={isPending}
        emptyMessage={query ? t("noResults") : t("empty")}
      />

      <DataPagination page={page} pageSize={PAGE_SIZE} total={total} onPageChange={setPage} />

      <PurchaseDetailsDialog
        purchase={fullPurchase ?? null}
        open={!!detailsId && !!fullPurchase}
        onOpenChange={(open) => {
          if (!open) setDetailsId(null)
        }}
      />
    </>
  )
}
