"use client"

import { PurchaseInvoice, api } from "@/lib/api"
import { useEffect, useState } from "react"
import { useDebouncedCallback } from "@/hooks/use-debounced-callback"
import { useAuth } from "@/components/common/auth-context"
import { PERMISSIONS } from "@/lib/constants"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Loader2, Plus, Eye, Pencil, CircleX, Upload, Undo2 } from "lucide-react"
import { useTranslations } from "next-intl"
import { useRouter } from "@/i18n/navigation"
import { toast } from "sonner"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import { PurchaseDetailsDialog } from "./_components/purchase-details-dialog"

const selectClass =
  "flex h-9 w-[160px] min-w-0 rounded-lg border border-input bg-transparent px-2.5 py-1 text-base transition-colors outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 md:text-sm dark:bg-input/30"

interface PurchasesClientProps {
  items: PurchaseInvoice[]
  total: number
  postedTotal: number
  page: number
  pageSize: number
  loading: boolean
  query: string
  status: string
  onQueryChange: (query: string) => void
  onStatusChange: (status: string) => void
  onPageChange: (page: number) => void
  onRefresh?: () => void
}

export function PurchasesClient({
  items,
  total,
  postedTotal,
  page,
  pageSize,
  loading,
  query,
  status,
  onQueryChange,
  onStatusChange,
  onPageChange,
  onRefresh,
}: PurchasesClientProps) {
  const t = useTranslations("Purchases")
  const tc = useTranslations("Common")
  const router = useRouter()
  const { hasPermission } = useAuth()
  const canCreate = hasPermission(PERMISSIONS.PURCHASES_CREATE)
  const canUpdate = hasPermission(PERMISSIONS.PURCHASES_UPDATE)
  const canCreateReturns = hasPermission(PERMISSIONS.PURCHASES_RETURN)
  const [searchInput, setSearchInput] = useState(query)
  const debouncedQueryChange = useDebouncedCallback(onQueryChange, 300)
  const [detailsFor, setDetailsFor] = useState<PurchaseInvoice | null>(null)

  useEffect(() => {
    const value = searchInput.trim()
    if (value === query) return
    debouncedQueryChange(value)
  }, [debouncedQueryChange, query, searchInput])

  const pageCount = Math.max(1, Math.ceil(total / pageSize))

  const handlePost = async (invoice: PurchaseInvoice) => {
    if (!window.confirm(t("confirmPost"))) return
    try {
      await api.purchases.post(invoice.id)
      toast.success(t("posted"))
      if (onRefresh) onRefresh()
    } catch (e) {
      toast.error((e as Error).message || t("saveFailed"))
    }
  }

  const handleCancelInvoice = async (invoice: PurchaseInvoice) => {
    if (!window.confirm(t("confirmCancel"))) return
    try {
      await api.purchases.cancel(invoice.id)
      toast.success(t("cancelledDone"))
      if (onRefresh) onRefresh()
    } catch (e) {
      toast.error((e as Error).message || t("saveFailed"))
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
        <select className={selectClass} value={status} onChange={(e) => onStatusChange(e.target.value)} aria-label={t("status")}>
          <option value="all">{t("statusAll")}</option>
          <option value="draft">{t("draft")}</option>
          <option value="posted">{t("posted")}</option>
          <option value="cancelled">{t("cancelled")}</option>
        </select>
        <span className="text-sm text-muted-foreground whitespace-nowrap">
          {`${postedTotal.toFixed(2)}`}
        </span>
      </div>

      <div className="rounded-md border bg-card">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>{t("invoiceNumber")}</TableHead>
              <TableHead>{t("supplier")}</TableHead>
              <TableHead>{t("date")}</TableHead>
              <TableHead>{t("paymentMethod")}</TableHead>
              <TableHead>{t("total")}</TableHead>
              <TableHead>{t("status")}</TableHead>
              <TableHead>{t("actions")}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading ? (
              <TableRow>
                <TableCell colSpan={7} className="h-24 text-center">
                  <Loader2 className="mx-auto h-6 w-6 animate-spin" />
                </TableCell>
              </TableRow>
            ) : items.length ? (
              items.map((invoice) => (
                <TableRow key={invoice.id}>
                  <TableCell className="font-medium">#{invoice.invoiceNumber}</TableCell>
                  <TableCell>{invoice.supplier?.name ?? "—"}</TableCell>
                  <TableCell dir="ltr" className="text-muted-foreground">
                    {new Date(invoice.date).toLocaleDateString()}
                  </TableCell>
                  <TableCell>{invoice.paymentMethod === "credit" ? t("credit") : t("cash")}</TableCell>
                  <TableCell className="font-medium">{invoice.total.toFixed(2)}</TableCell>
                  <TableCell>
                    <div className="flex items-center gap-1">
                      {statusBadge(invoice.status)}
                      {(invoice.returnStatus === 'partial' || invoice.returnStatus === 'full') && (
                        <span className="inline-flex items-center rounded-full bg-orange-500/10 px-2 py-0.5 text-xs font-medium text-orange-600">
                          {t(invoice.returnStatus === 'full' ? "fullyReturned" : "partiallyReturned")}
                        </span>
                      )}
                    </div>
                  </TableCell>
                  <TableCell>
                    <div className="flex items-center gap-1">
                      <Button variant="ghost" size="icon" aria-label={t("details")} title={t("details")} onClick={() => setDetailsFor(invoice)}>
                        <Eye className="h-4 w-4" />
                      </Button>
                      {canUpdate && invoice.status === "draft" && (
                        <Button variant="ghost" size="icon" aria-label={t("post")} title={t("post")} onClick={() => handlePost(invoice)}>
                          <Upload className="h-4 w-4" />
                        </Button>
                      )}
                      {canCreate && invoice.status !== "cancelled" && (
                        <Button
                          variant="ghost"
                          size="icon"
                          aria-label={t("editPurchase")}
                          onClick={() => router.push(`/purchases/editor?id=${invoice.id}`)}
                        >
                          <Pencil className="h-4 w-4" />
                        </Button>
                      )}
                      {canUpdate && invoice.status === "posted" && (
                        <Button variant="ghost" size="icon" aria-label={t("cancelInvoice")} title={t("cancelInvoice")} onClick={() => handleCancelInvoice(invoice)}>
                          <CircleX className="h-4 w-4" />
                        </Button>
                      )}
                      {invoice.status === 'posted' && canCreateReturns && (
                        <Button
                          variant="ghost"
                          size="icon"
                          aria-label={t("makeReturn")}
                          title={t("makeReturn")}
                          onClick={() => router.push(`/returns/?tab=purchase&purchase=${invoice.id}`)}
                        >
                          <Undo2 className="h-4 w-4" />
                        </Button>
                      )}
                    </div>
                  </TableCell>
                </TableRow>
              ))
            ) : (
              <TableRow>
                <TableCell colSpan={7} className="h-24 text-center">
                  {query ? t("noResults") : t("empty")}
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </div>

      <div className="flex items-center justify-between py-4">
        <span className="text-sm text-muted-foreground">{`${total}`}</span>
        <div className="flex items-center gap-2">
          <Button variant="outline" size="sm" onClick={() => onPageChange(page - 1)} disabled={page <= 1}>
            {tc("previous")}
          </Button>
          <span className="text-sm text-muted-foreground whitespace-nowrap">{`${page} / ${pageCount}`}</span>
          <Button variant="outline" size="sm" onClick={() => onPageChange(page + 1)} disabled={page >= pageCount}>
            {tc("next")}
          </Button>
        </div>
      </div>

      <PurchaseDetailsDialog
        purchase={detailsFor}
        open={!!detailsFor}
        onOpenChange={(open) => {
          if (!open) setDetailsFor(null)
        }}
      />
    </>
  )
}
