"use client"

import { useEffect, useState } from "react"
import { api, PurchaseInvoice, PurchaseReturn } from "@/lib/api"
import { useAuth } from "@/components/common/auth-context"
import { PERMISSIONS } from "@/lib/constants"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import { Card, CardContent } from "@/components/ui/card"
import { useTranslations } from "next-intl"
import { toast } from "sonner"
import { Loader2, Search } from "lucide-react"
import { CreatePurchaseReturnDialog } from "./create-purchase-return-dialog"

const PAGE_SIZE = 20

interface Props {
  onOpenPurchase?: (purchaseId: string) => void
  initialPurchaseId?: string | null
}

export function PurchasesReturnsSection({ onOpenPurchase, initialPurchaseId }: Props) {
  const t = useTranslations("Returns")
  const tp = useTranslations("POS")
  const tc = useTranslations("Common")
  const { hasPermission } = useAuth()
  const canView = hasPermission(PERMISSIONS.INVOICES_VIEW)
  const canCreate = hasPermission(PERMISSIONS.PURCHASES_RETURN)

  const [returns, setReturns] = useState<PurchaseReturn[]>([])
  const [total, setTotal] = useState(0)
  const [page, setPage] = useState(1)
  const [reloadKey, setReloadKey] = useState(0)
  const [loading, setLoading] = useState(true)
  const [searchInput, setSearchInput] = useState("")
  const [candidates, setCandidates] = useState<PurchaseInvoice[]>([])
  const [searching, setSearching] = useState(false)
  const [dialogPurchase, setDialogPurchase] = useState<PurchaseInvoice | null>(null)

  useEffect(() => {
    if (!canView) return
    let cancelled = false
    api.purchaseReturns
      .listPaged(page, PAGE_SIZE)
      .then((res) => {
        if (cancelled) return
        setReturns(res.items)
        setTotal(res.total)
      })
      .catch(() => {
        if (cancelled) return
        setReturns([])
        setTotal(0)
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [page, reloadKey, canView])

  useEffect(() => {
    if (!canView || !canCreate) return
    if (!initialPurchaseId) return
    api.purchases.get(initialPurchaseId).then((inv) => {
      if ((inv.status ?? "posted") === "posted") setDialogPurchase(inv)
      else toast.error(t("onlyPosted"))
    }).catch(() => toast.error(t("loadFailed")))
  }, [initialPurchaseId, canView, canCreate])

  const handlePageChange = (p: number) => {
    setLoading(true)
    setPage(p)
  }

  const pageCount = Math.max(1, Math.ceil(total / PAGE_SIZE))

  const handleSearch = async () => {
    const q = searchInput.trim()
    if (!q) return
    setSearching(true)
    try {
      const res = await api.purchases.listPaged(1, 10, { q, status: "posted" })
      if (res.items.length === 0) {
        toast.error(t("noPostedPurchasesFound"))
        setCandidates([])
      } else if (res.items.length === 1) {
        openPurchase(res.items[0].id)
        setCandidates([])
      } else {
        setCandidates(res.items)
      }
    } catch {
      toast.error(t("noPostedPurchasesFound"))
    } finally {
      setSearching(false)
    }
  }

  const openPurchase = async (id: string) => {
    try {
      const inv = await api.purchases.get(id)
      if ((inv.status ?? "posted") !== "posted") {
        toast.error(t("onlyPosted"))
        return
      }
      setDialogPurchase(inv)
    } catch {
      toast.error(t("noPostedPurchasesFound"))
    }
  }

  const methodLabel = (m?: string | null) =>
    m === "credit" ? t("credit") : t("cash")

  const columns = [
    t("returnNumber"),
    tp("time"),
    t("purchaseNumber"),
    t("refundAmount"),
    t("method"),
    t("notes"),
  ]

  return (
    <div className="space-y-4">
      {canCreate && (
        <Card>
          <CardContent className="flex flex-wrap items-center gap-3 pt-4">
            <Input
              className="max-w-xs"
              placeholder={t("searchPurchasePlaceholder")}
              value={searchInput}
              onChange={(e) => setSearchInput(e.target.value)}
              onKeyDown={(e) => e.key === "Enter" && handleSearch()}
              inputMode="numeric"
            />
            <Button onClick={handleSearch} disabled={searching}>
              {searching ? <Loader2 className="ml-2 h-4 w-4 animate-spin" /> : <Search className="ml-2 h-4 w-4" />}
              {t("loadPurchase")}
            </Button>
          </CardContent>
        </Card>
      )}

      {candidates.length > 1 && (
        <Card>
          <CardContent className="pt-4">
            <p className="mb-2 text-sm font-medium">{t("pickPurchase")}</p>
            <div className="space-y-2">
              {candidates.map((inv) => (
                <button
                  key={inv.id}
                  type="button"
                  onClick={() => openPurchase(inv.id)}
                  className="flex w-full items-center justify-between rounded-md border px-3 py-2 text-sm hover:bg-accent"
                >
                  <span>#{inv.invoiceNumber}</span>
                  <span>{new Date(inv.createdAt).toLocaleDateString()}</span>
                  <span className="font-medium">{inv.total.toFixed(2)}</span>
                </button>
              ))}
            </div>
          </CardContent>
        </Card>
      )}

      <div className="rounded-md border bg-card">
        <Table>
          <TableHeader>
            <TableRow>
              {columns.map((c) => (
                <TableHead key={c}>{c}</TableHead>
              ))}
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading ? (
              <TableRow>
                <TableCell colSpan={columns.length} className="h-24 text-center">
                  <Loader2 className="mx-auto h-6 w-6 animate-spin" />
                </TableCell>
              </TableRow>
            ) : returns.length ? (
              returns.map((ret) => (
                <TableRow key={ret.id}>
                  <TableCell className="font-medium">#{ret.number}</TableCell>
                  <TableCell>{new Date(ret.createdAt).toLocaleString()}</TableCell>
                  <TableCell>#{ret.purchaseInvoiceNumber ?? "—"}</TableCell>
                  <TableCell className="font-medium">{ret.totalAmount.toFixed(2)}</TableCell>
                  <TableCell>{methodLabel(ret.paymentMethod)}</TableCell>
                  <TableCell className="text-muted-foreground">{ret.notes || "—"}</TableCell>
                </TableRow>
              ))
            ) : (
              <TableRow>
                <TableCell colSpan={columns.length} className="h-24 text-center">
                  {tc("noResults")}
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </div>

      <div className="flex items-center justify-between py-4">
        <span className="text-sm text-muted-foreground">
          {t("returnsCount", { count: total })}
        </span>
        <div className="flex items-center gap-2">
          <Button variant="outline" size="sm" onClick={() => handlePageChange(page - 1)} disabled={page <= 1}>
            {tc("previous")}
          </Button>
          <Button variant="outline" size="sm" onClick={() => handlePageChange(page + 1)} disabled={page >= pageCount}>
            {tc("next")}
          </Button>
        </div>
      </div>

      {dialogPurchase && (
        <CreatePurchaseReturnDialog
          key={dialogPurchase.id}
          open={!!dialogPurchase}
          purchase={dialogPurchase}
          onClose={() => setDialogPurchase(null)}
          onCreated={() => {
            setDialogPurchase(null)
            setLoading(true)
            setReloadKey((k) => k + 1)
          }}
        />
      )}
    </div>
  )
}
