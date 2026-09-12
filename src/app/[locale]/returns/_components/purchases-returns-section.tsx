"use client"

import { useCallback, useEffect, useRef, useState } from "react"
import { useQueryClient } from "@tanstack/react-query"
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
import { Eye, Loader2, Search } from "lucide-react"
import { PurchaseInvoice, PurchaseReturn } from "@/types/domain/domain.types"
import { purchaseReturnsKeys, usePurchaseReturnsPage } from "@/hooks/use-returns"
import { getPurchase, listPurchasesPaged } from "@/api/purchases"
import { purchasesKeys } from "@/hooks/use-purchases"
import { TooltipIconButton } from "@/components/common/tooltip-icon-button"
import { CreatePurchaseReturnDialog } from "./create-purchase-return-dialog"
import { PurchaseReturnDetailsDialog } from "./purchase-return-details-dialog"

const PAGE_SIZE = 20

interface Props {
  initialPurchaseId?: string | null
}

export function PurchasesReturnsSection({ initialPurchaseId }: Props) {
  const t = useTranslations("Returns")
  const tp = useTranslations("POS")
  const tc = useTranslations("Common")
  const queryClient = useQueryClient()

  const [page, setPage] = useState(1)
  const [searchInput, setSearchInput] = useState("")
  const [searching, setSearching] = useState(false)
  const [candidates, setCandidates] = useState<PurchaseInvoice[] | null>(null)
  const [dialogPurchase, setDialogPurchase] = useState<PurchaseInvoice | null>(null)
  const [detailsRet, setDetailsRet] = useState<PurchaseReturn | null>(null)

  const { data, isPending: loading } = usePurchaseReturnsPage(page, PAGE_SIZE)
  const returns: PurchaseReturn[] = data?.items ?? []
  const total = data?.total ?? 0

  const refresh = () => queryClient.invalidateQueries({ queryKey: purchaseReturnsKeys.all })

  const openPurchase = useCallback(
    async (id: string) => {
      try {
        const inv = await queryClient.fetchQuery({
          queryKey: purchasesKeys.detail(id),
          queryFn: () => getPurchase(id),
        })
        if ((inv.status ?? "posted") !== "posted") {
          toast.error(t("onlyPosted"))
          return
        }
        setDialogPurchase(inv)
      } catch {
        toast.error(t("loadFailed"))
      }
    },
    [queryClient, t]
  )

  // Deep link: open the return dialog for a specific purchase.
  const deepLinkRef = useRef<string | null>(initialPurchaseId ?? null)
  useEffect(() => {
    const id = deepLinkRef.current
    if (!id) return
    void openPurchase(id)
  }, [openPurchase])

  // Candidate search over posted purchases.
  const handleSearch = async () => {
    const q = searchInput.trim()
    if (!q || searching) return
    setSearching(true)
    try {
      const res = await queryClient.fetchQuery({
        queryKey: purchasesKeys.paged(1, 10, { q, status: "posted" }),
        queryFn: () => listPurchasesPaged(1, 10, { q, status: "posted" }),
      })
      const items = res.items ?? []
      if (items.length === 0) {
        toast.error(t("noPostedPurchasesFound"))
        setCandidates(null)
      } else if (items.length === 1) {
        setCandidates(null)
        await openPurchase(items[0].id)
      } else {
        setCandidates(items)
      }
    } catch {
      toast.error(t("loadFailed"))
    } finally {
      setSearching(false)
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
    "",
  ]

  return (
    <div className="space-y-4">
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

      {candidates && candidates.length > 1 && (
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
                  <TableCell>
                    <TooltipIconButton label={t("viewReturn")} onClick={() => setDetailsRet(ret)}>
                      <Eye className="h-4 w-4" />
                    </TooltipIconButton>
                  </TableCell>
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
          <Button variant="outline" size="sm" onClick={() => setPage(page - 1)} disabled={page <= 1}>
            {tc("previous")}
          </Button>
          <Button variant="outline" size="sm" onClick={() => setPage(page + 1)} disabled={page * PAGE_SIZE >= total}>
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
            void refresh()
          }}
        />
      )}

      <PurchaseReturnDetailsDialog
        ret={detailsRet}
        open={!!detailsRet}
        onOpenChange={(open) => {
          if (!open) setDetailsRet(null)
        }}
      />
    </div>
  )
}
