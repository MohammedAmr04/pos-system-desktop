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
import { PurchaseInvoice, PurchaseReturn } from "@/types/domain/domain.types"
import { purchaseReturnsKeys, usePurchaseReturnsPage } from "@/hooks/use-returns"
import { createPurchaseReturn } from "@/actions/purchase-returns.actions"

interface CreatePurchaseReturnDialogProps {
  open: boolean
  purchase: PurchaseInvoice
  onClose: () => void
  onCreated?: (purchaseReturn: PurchaseReturn) => void
}

export function CreatePurchaseReturnDialog({ open, purchase, onClose, onCreated }: CreatePurchaseReturnDialogProps) {
  const t = useTranslations("Returns")
  const tp = useTranslations("POS")
  const resolveError = useApiError()
  const queryClient = useQueryClient()

  const [qtys, setQtys] = useState<Record<string, number>>({})
  const [notes, setNotes] = useState("")
  const [submitting, setSubmitting] = useState(false)

  const { data: past } = usePurchaseReturnsPage(1, 100, purchase.id)

  const items = purchase.items ?? []

  const returnedByItem = useMemo(() => {
    const map = new Map<string, number>()
    for (const ret of past?.items ?? []) {
      for (const d of ret.details ?? []) {
        map.set(d.purchaseItemId, (map.get(d.purchaseItemId) ?? 0) + d.quantity)
      }
    }
    return map
  }, [past])

  const lines = useMemo(
    () =>
      items.map((item) => ({
        itemId: item.id,
        name: item.product?.name ?? "—",
        unitName: item.unitName ?? "",
        unitCost: item.unitCost,
        purchasedQty: item.quantity,
        returnedQty: returnedByItem.get(item.id) ?? 0,
      })),
    [items, returnedByItem]
  )

  const refundTotal = lines.reduce((sum, l) => sum + (qtys[l.itemId] ?? 0) * l.unitCost, 0)

  const setQty = (itemId: string, value: number) => {
    const line = lines.find((l) => l.itemId === itemId)
    const max = line ? Math.max(line.purchasedQty - line.returnedQty, 0) : 0
    setQtys((prev) => ({ ...prev, [itemId]: Math.max(0, Math.min(value, max)) }))
  }

  const handleSubmit = async () => {
    const payloadItems = lines
      .filter((l) => (qtys[l.itemId] ?? 0) > 0)
      .map((l) => ({ purchaseItemId: l.itemId, quantity: qtys[l.itemId] }))
    if (payloadItems.length === 0) {
      toast.error(t("noItemsSelected"))
      return
    }
    setSubmitting(true)
    try {
      const created = await createPurchaseReturn(purchase.id, {
        items: payloadItems,
        notes: notes.trim() || undefined,
      })
      toast.success(t("created", { number: created.number }))
      await queryClient.invalidateQueries({ queryKey: purchaseReturnsKeys.all })
      await queryClient.invalidateQueries({ queryKey: ["purchases"] })
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
            {t("dialogTitlePurchase", { number: purchase.invoiceNumber })}
          </DialogTitle>
          <DialogDescription>{t("dialogHintPurchase")}</DialogDescription>
        </DialogHeader>

        <div className="space-y-3">
          {lines.map((l) => {
            const qty = qtys[l.itemId] ?? 0
            const available = Math.max(l.purchasedQty - l.returnedQty, 0)
            return (
              <div key={l.itemId} className="flex items-center gap-3 rounded-md border p-3">
                <div className="flex-1 min-w-0">
                  <p className="truncate text-sm font-medium">{l.name}</p>
                  <p className="text-xs text-muted-foreground">
                    {t("purchasedOf", { purchased: l.purchasedQty, returned: l.returnedQty })} · {l.unitCost.toFixed(2)} / {l.unitName}
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
                  onChange={(e) => setQty(l.itemId, Number(e.target.value))}
                />
              </div>
            )
          })}
          {lines.length > 0 && lines.every((l) => Math.max(l.purchasedQty - l.returnedQty, 0) === 0) && (
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
          <span className="text-sm font-medium">{t("refundAmount")}</span>
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
