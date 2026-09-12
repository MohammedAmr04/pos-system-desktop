"use client"

import { useState } from "react"
import { CartItem, usePOSStore } from "@/store/pos.store"
import { useTranslations } from "next-intl"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"

export interface LineEditDraft {
  unitPrice: string
  note: string
  discountType: 'percentage' | 'fixed'
  discountValue: string
}

export interface LineEditState extends LineEditDraft {
  item: CartItem
}

interface LineEditDialogProps {
  initial: LineEditState
  canPriceOverride: boolean
  canLineDiscount: boolean
  onClose: () => void
}

export function LineEditDialog({ initial, canPriceOverride, canLineDiscount, onClose }: LineEditDialogProps) {
  const t = useTranslations("POS")
  const updateUnitPrice = usePOSStore((s) => s.updateUnitPrice)
  const setLineDiscount = usePOSStore((s) => s.setLineDiscount)
  const clearLineDiscount = usePOSStore((s) => s.clearLineDiscount)

  const { item } = initial
  const [unitPrice, setUnitPrice] = useState(initial.unitPrice)
  const [note, setNote] = useState(initial.note)
  const [discountType, setDiscountType] = useState<'percentage' | 'fixed'>(initial.discountType)
  const [discountValue, setDiscountValue] = useState(initial.discountValue)

  const save = () => {
    const price = parseFloat(unitPrice)
    if (isNaN(price) || price < 0) return
    updateUnitPrice(item.id, price, note.trim() || undefined)
    const dValue = parseFloat(discountValue)
    if (dValue > 0) {
      setLineDiscount(item.id, discountType, dValue)
    } else {
      clearLineDiscount(item.id)
    }
    onClose()
  }

  const parsedPrice = parseFloat(unitPrice)
  const priceChanged = !isNaN(parsedPrice) && parsedPrice !== item.originalUnitPrice

  return (
    <Dialog open onOpenChange={(o) => { if (!o) onClose() }}>
      <DialogContent className="sm:max-w-[420px]">
        <DialogHeader>
          <DialogTitle>{t("editLine")}</DialogTitle>
          <DialogDescription>
            {item.name} ({item.unitName})
          </DialogDescription>
        </DialogHeader>
        <div className="py-2 space-y-4">
          {canPriceOverride && (
            <div className="space-y-2">
              <label className="text-sm font-medium">{t("unitPrice")}</label>
              <Input
                type="number"
                min="0"
                step="1"
                className="text-right text-lg h-12"
                value={unitPrice}
                onChange={(e) => setUnitPrice(e.target.value)}
                autoFocus
              />
              {priceChanged && (
                <div className="text-xs text-muted-foreground">
                  {t("originalPrice")}: {item.originalUnitPrice.toFixed(2)}
                </div>
              )}
            </div>
          )}
          {canLineDiscount && item.allowDiscount && (
            <div className="space-y-2">
              <label className="text-sm font-medium">{t("lineDiscount")}</label>
              <div className="flex gap-2">
                <Button
                  variant={discountType === 'percentage' ? 'default' : 'outline'}
                  className="flex-1"
                  onClick={() => setDiscountType('percentage')}
                >
                  {t("discountTypePercentage")}
                </Button>
                <Button
                  variant={discountType === 'fixed' ? 'default' : 'outline'}
                  className="flex-1"
                  onClick={() => setDiscountType('fixed')}
                >
                  {t("discountTypeFixed")}
                </Button>
              </div>
              <Input
                type="number"
                min="0"
                max={discountType === 'percentage' ? 100 : undefined}
                step="1"
                className="text-right text-lg h-12"
                value={discountValue}
                onChange={(e) => setDiscountValue(e.target.value)}
              />
            </div>
          )}
          {canPriceOverride && priceChanged && (
            <div className="space-y-2">
              <label className="text-sm font-medium">{t("priceEditNote")}</label>
              <textarea
                placeholder={t("priceEditNotePlaceholder")}
                className="w-full min-w-0 rounded-lg border border-input bg-transparent px-2.5 py-1 text-base transition-colors outline-none placeholder:text-muted-foreground focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 disabled:pointer-events-none disabled:opacity-50 md:text-sm dark:bg-input/30 resize-none"
                rows={3}
                value={note}
                onChange={(e) => setNote(e.target.value)}
              />
              <p className="text-xs text-muted-foreground">{t("priceEditNoteHint")}</p>
            </div>
          )}
        </div>
        <DialogFooter className="flex-col gap-2 sm:flex-row">
          <Button variant="outline" onClick={onClose}>
            {t("cancel")}
          </Button>
          <Button onClick={save} disabled={isNaN(parsedPrice) || parsedPrice < 0}>
            {t("save")}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
