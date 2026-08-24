"use client"

import { useEffect, useState } from "react"
import { api, Invoice } from "@/lib/api"
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
import { CreateReturnDialog } from "./create-return-dialog"

const PAGE_SIZE = 20

interface Props {
  onOpenPurchase?: (purchaseId: string) => void
}

export function SalesReturnsSection({ onOpenPurchase }: Props) {
  const t = useTranslations("Returns")
  const ti = useTranslations("Invoices")
  const tc = useTranslations("Common")
  const { hasPermission } = useAuth()
  const canView = hasPermission(PERMISSIONS.INVOICES_VIEW)
  const canCreate = hasPermission(PERMISSIONS.INVOICES_RETURN)

  const [returns, setReturns] = useState<Awaited<ReturnType<typeof api.saleReturns.listPaged>>["items"]>([])
  const [total, setTotal] = useState(0)
  const [page, setPage] = useState(1)
  const [reloadKey, setReloadKey] = useState(0)
  const [loading, setLoading] = useState(true)
  const [searchInput, setSearchInput] = useState("")
  const [candidates, setCandidates] = useState<Invoice[] | null>(null)
  const [searching, setSearching] = useState(false)
  const [dialogInvoice, setDialogInvoice] = useState<Invoice | null>(null)

  useEffect(() => {
    if (!canView) return
    let cancelled = false
    api.saleReturns
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
      const res = await api.invoices.listPaged(1, 10, { q, status: "posted" })
      if (res.items.length === 0) {
        toast.error(t("noPostedInvoicesFound"))
        setCandidates(null)
      } else if (res.items.length === 1) {
        await openInvoice(res.items[0].id)
        setCandidates(null)
      } else {
        setCandidates(res.items)
      }
    } catch {
      toast.error(t("noPostedInvoicesFound"))
    } finally {
      setSearching(false)
    }
  }

  const openInvoice = async (id: string) => {
    try {
      const inv = await api.invoices.get(id)
      if ((inv.status ?? "posted") !== "posted") {
        toast.error(t("onlyPosted"))
        return
      }
      setDialogInvoice(inv)
    } catch {
      toast.error(t("noPostedInvoicesFound"))
    }
  }

  const methodLabel = (m?: string | null) =>
    m === "credit" ? t("credit") : t("cash")

  const columns = [
    t("returnNumber"),
    ti("time"),
    ti("invoiceNumber"),
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
              placeholder={t("searchInvoicePlaceholder")}
              value={searchInput}
              onChange={(e) => setSearchInput(e.target.value)}
              onKeyDown={(e) => e.key === "Enter" && handleSearch()}
              inputMode="numeric"
            />
            <Button onClick={handleSearch} disabled={searching}>
              {searching ? <Loader2 className="ml-2 h-4 w-4 animate-spin" /> : <Search className="ml-2 h-4 w-4" />}
              {t("loadInvoice")}
            </Button>
          </CardContent>
        </Card>
      )}

      {candidates && candidates.length > 1 && (
        <Card>
          <CardContent className="pt-4">
            <p className="mb-2 text-sm font-medium">{t("pickInvoice")}</p>
            <div className="space-y-2">
              {candidates.map((inv) => (
                <button
                  key={inv.id}
                  type="button"
                  onClick={() => openInvoice(inv.id)}
                  className="flex w-full items-center justify-between rounded-md border px-3 py-2 text-sm hover:bg-accent"
                >
                  <span>#{inv.invoiceNumber}</span>
                  <span>{new Date(inv.createdAt).toLocaleDateString()}</span>
                  <span className="font-medium">{inv.totalAmount.toFixed(2)}</span>
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
                  <TableCell>#{ret.invoiceNumber ?? "—"}</TableCell>
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

      {dialogInvoice && (
        <CreateReturnDialog
          key={dialogInvoice.id}
          open={!!dialogInvoice}
          invoice={dialogInvoice}
          onClose={() => setDialogInvoice(null)}
          onCreated={() => {
            setDialogInvoice(null)
            setLoading(true)
            setReloadKey((k) => k + 1)
          }}
        />
      )}
    </div>
  )
}
