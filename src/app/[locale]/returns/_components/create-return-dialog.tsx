"use client"

import { useMemo, useState } from "react"
import { useQueryClient } from "@tanstack/react-query"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { useTranslations } from "next-intl"
import { useApiError } from "@/lib/api-error"
import { toast } from "sonner"
import { Loader2 } from "lucide-react"
import { Invoice, SaleReturn } from "@/types/domain/domain.types"
import { saleReturnsKeys, useSaleReturnsPage } from "@/hooks/use-returns"
import { createSaleReturn } from "@/actions/sale-returns.actions"

interface CreateReturnDialogProps {
  open: boolean
  invoice: Invoice
  onClose: () => void
  onCreated?: (saleReturn: SaleReturn) => void
}

export function CreateReturnDialog({ open, invoice, onClose, onCreated }: CreateReturnDialogProps) {
  const t = useTranslations("Returns")
  const ti = useTranslations("Invoices")
  const tp = useTranslations("POS")
  const resolveError = useApiError()
  const queryClient = useQueryClient()

  const [qtys, setQtys] = useState<Record<string, number>>({})
  const [notes, setNotes] = useState("")
  const [submitting, setSubmitting] = useState(false)

  const { data: past } = useSaleReturnsPage(1, 100, invoice.id)

  const details = invoice.invoiceDetail ?? invoice.InvoiceDetail ?? []

  const returnedByDetail = useMemo(() => {
    const map = new Map<string, number>()
    for (const ret of past?.items ?? []) {
      for (const d of ret.details ?? []) {
        map.set(d.invoiceDetailId, (map.get(d.invoiceDetailId) ?? 0) + d.quantity)
      }
    }
    return map
  }, [past])

  const lines = useMemo(
    () =>
      details.map((d) => ({
        detailId: d.id,
        name: d.product?.name ?? "—",
        unitName: d.unitName ?? "",
        unitPrice: d.unitPrice ?? d.salePrice,
        soldQty: d.quantity,
        returnedQty: returnedByDetail.get(d.id) ?? 0,
      })),
    [details, returnedByDetail]
  )

  const refundTotal = lines.reduce((sum, l) => sum + (qtys[l.detailId] ?? 0) * l.unitPrice, 0)

  const setQty = (detailId: string, value: number) => {
    const line = lines.find((l) => l.detailId === detailId)
    const max = line ? Math.max(line.soldQty - line.returnedQty, 0) : 0
    setQtys((prev) => ({ ...prev, [detailId]: Math.max(0, Math.min(value, max)) }))
  }

  const handleSubmit = async () => {
    const items = lines
      .filter((l) => (qtys[l.detailId] ?? 0) > 0)
      .map((l) => ({ invoiceDetailId: l.detailId, quantity: qtys[l.detailId] }))
    if (items.length === 0) {
      toast.error(t("noItemsSelected"))
      return
    }
    setSubmitting(true)
    try {
      const created = await createSaleReturn(invoice.id, { items, notes: notes.trim() || undefined })
      toast.success(t("created", { number: created.number }))
      await queryClient.invalidateQueries({ queryKey: saleReturnsKeys.all })
      await queryClient.invalidateQueries({ queryKey: ["invoices"] })
      onCreated?.(created)
      onClose()
    } catch (e) {
      toast.error(resolveError(e) || t("createFailed"))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={(v) => !v && onClose()}>
      <DialogContent className="max-w-2xl">
        <DialogHeader>
          <DialogTitle>
            {t("dialogTitle", { number: invoice.invoiceNumber })}
          </DialogTitle>
          <DialogDescription>{t("dialogHint")}</DialogDescription>
        </DialogHeader>

        <div className="space-y-3">
          {lines.map((l) => {
            const qty = qtys[l.detailId] ?? 0
            const available = Math.max(l.soldQty - l.returnedQty, 0)
            return (
              <div key={l.detailId} className="flex items-center gap-3 rounded-md border p-3">
                <div className="flex-1 min-w-0">
                  <p className="truncate text-sm font-medium">{l.name}</p>
                  <p className="text-xs text-muted-foreground">
                    {t("soldOf", { sold: l.soldQty, returned: l.returnedQty })} · {l.unitPrice.toFixed(2)} / {l.unitName}
                  </p>
                </div>
                <Input
                  type="number"
                  step="1"
                  min={0}
                  max={available}
                  disabled={available === 0}
                  className="w-24 text-center"
                  aria-label={tp("quantity")}
                  value={qty === 0 ? "" : qty}
                  placeholder="0"
                  onChange={(e) => setQty(l.detailId, Number(e.target.value))}
                />
              </div>
            )
          })}
          {lines.length > 0 && lines.every((l) => Math.max(l.soldQty - l.returnedQty, 0) === 0) && (
            <p className="rounded-md bg-emerald-500/10 px-3 py-2 text-sm font-medium text-emerald-600">
              {t("fullyReturned")}
            </p>
          )}
        </div>

        <Input
          placeholder={t("notesPlaceholder")}
          value={notes}
          onChange={(e) => setNotes(e.target.value)}
        />

        <div className="flex items-center justify-between rounded-md bg-muted px-3 py-2">
          <span className="text-sm font-medium">{ti("total")}</span>
          <span className="text-lg font-bold">{refundTotal.toFixed(2)}</span>
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={onClose} disabled={submitting}>
            {tp("cancel")}
          </Button>
          <Button onClick={handleSubmit} disabled={submitting || refundTotal <= 0}>
            {submitting && <Loader2 className="ml-2 h-4 w-4 animate-spin" />}
            {t("confirmCreate")}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
