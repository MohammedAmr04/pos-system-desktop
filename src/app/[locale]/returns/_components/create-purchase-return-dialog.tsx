"use client"

import { useEffect, useMemo, useState } from "react"
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
import { toast } from "sonner"
import { Loader2 } from "lucide-react"
import { api, PurchaseInvoice, PurchaseReturn } from "@/lib/api"

interface CreatePurchaseReturnDialogProps {
  open: boolean
  purchase: PurchaseInvoice
  onClose: () => void
  onCreated?: (purchaseReturn: PurchaseReturn) => void
}

interface LineState {
  itemId: string
  name: string
  unitName: string
  unitCost: number
  purchasedQty: number
  returnedQty: number
  qty: number
}

export function CreatePurchaseReturnDialog({ open, purchase, onClose, onCreated }: CreatePurchaseReturnDialogProps) {
  const t = useTranslations("Returns")
  const tp = useTranslations("POS")

  const [lines, setLines] = useState<LineState[]>([])
  const [notes, setNotes] = useState("")
  const [submitting, setSubmitting] = useState(false)

  useEffect(() => {
    if (!open) return
    let cancelled = false

    async function load() {
      const items = purchase.items ?? []
      const past = await api.purchaseReturns.listPaged(1, 100, purchase.id).catch(() => ({ items: [], total: 0 }))
      const returnedByItem = new Map<string, number>()
      for (const ret of past.items) {
        for (const d of ret.details ?? []) {
          returnedByItem.set(d.purchaseItemId, (returnedByItem.get(d.purchaseItemId) ?? 0) + d.quantity)
        }
      }
      if (cancelled) return
      setLines(
        items.map((item) => {
          const purchased = item.quantity
          const returned = returnedByItem.get(item.id) ?? 0
          return {
            itemId: item.id,
            name: item.product?.name ?? "—",
            unitName: item.unitName ?? "",
            unitCost: item.unitCost,
            purchasedQty: purchased,
            returnedQty: returned,
            qty: 0,
          }
        })
      )
    }

    void load()
    return () => {
      cancelled = true
    }
  }, [open, purchase])

  const refundTotal = useMemo(() => lines.reduce((sum, l) => sum + l.qty * l.unitCost, 0), [lines])

  const setQty = (itemId: string, value: number) => {
    setLines((prev) =>
      prev.map((l) =>
        l.itemId === itemId
          ? { ...l, qty: Math.max(0, Math.min(value, Math.max(l.purchasedQty - l.returnedQty, 0))) }
          : l
      )
    )
  }

  const handleSubmit = async () => {
    const items = lines
      .filter((l) => l.qty > 0)
      .map((l) => ({ purchaseItemId: l.itemId, quantity: l.qty }))
    if (items.length === 0) {
      toast.error(t("noItemsSelected"))
      return
    }
    setSubmitting(true)
    try {
      const created = await api.purchaseReturns.create(purchase.id, { items, notes: notes.trim() || undefined })
      toast.success(t("created", { number: created.number }))
      onCreated?.(created)
      onClose()
    } catch (e) {
      toast.error((e as Error).message || t("createFailed"))
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
                  value={l.qty === 0 ? "" : l.qty}
                  placeholder="0"
                  onChange={(e) => setQty(l.itemId, Number(e.target.value))}
                />
              </div>
            )
          })}
          {lines.every((l) => Math.max(l.purchasedQty - l.returnedQty, 0) === 0) && (
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
