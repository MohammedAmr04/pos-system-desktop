"use client"

import { useState } from "react"
import { useTranslations } from "next-intl"
import { Ban, Minus, Pencil, Plus, StickyNote, Trash2 } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { CartItem, usePOSStore } from "@/store/pos.store"
import { useAuth } from "@/components/common/auth-context"
import { FEATURES, PERMISSIONS } from "@/lib/constants"
import { lineDiscountAmount, lineFinalTotal } from "./utils/pricing"
import { LineEditDialog, LineEditState } from "./line-edit-dialog"

export function CartPanel() {
  const t = useTranslations("POS")
  const { hasAccess } = useAuth()
  const canPriceOverride = hasAccess(PERMISSIONS.PRICE_OVERRIDE, FEATURES.PRICE_OVERRIDE)
  const canLineDiscount = hasAccess(PERMISSIONS.DISCOUNTS_PRODUCT, FEATURES.PRODUCT_DISCOUNT)

  const cartItems = usePOSStore((s) => s.cartItems)
  const updateQuantity = usePOSStore((s) => s.updateQuantity)
  const removeItem = usePOSStore((s) => s.removeItem)

  const [editState, setEditState] = useState<LineEditState | null>(null)
  const totalItems = cartItems.reduce((acc, item) => acc + item.quantity, 0)

  const openLineEdit = (item: CartItem) => {
    setEditState({
      item,
      unitPrice: String(item.unitPrice),
      note: item.priceEditNote ?? '',
      discountType: item.discountType ?? 'percentage',
      discountValue: item.discountValue && item.discountValue > 0 ? String(item.discountValue) : '',
    })
  }

  return (
    <Card className="flex-1 flex flex-col min-h-0">
      <CardHeader className="py-4 flex-row items-center justify-between">
        <CardTitle>{t("currentCart")}</CardTitle>
        {totalItems > 0 && (
          <div className="rounded-full bg-primary px-3 py-1 text-xs font-medium text-primary-foreground">
            {t("totalItems")}: {totalItems}
          </div>
        )}
      </CardHeader>
      <CardContent className="flex-1 overflow-y-auto px-4 py-0">
        {cartItems.length === 0 ? (
          <div className="flex h-full items-center justify-center text-muted-foreground">
            {t("emptyCart")}
          </div>
        ) : (
          <div className="space-y-4 py-4">
            {cartItems.map((item) => {
              const ld = lineDiscountAmount(item)
              return (
                <div key={item.id} className="flex items-center justify-between border-b pb-4 gap-4">
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2">
                      <h4 className="text-lg font-semibold truncate">{item.name}</h4>
                      <span className="shrink-0 inline-flex items-center rounded bg-muted px-1.5 py-0.5 text-[10px] font-medium text-muted-foreground">
                        {item.unitName}
                      </span>
                      {!item.allowDiscount && (
                        <span className="shrink-0 inline-flex items-center gap-1 rounded bg-destructive/10 px-1.5 py-0.5 text-[10px] font-medium text-destructive">
                          <Ban className="h-3 w-3" />
                          {t("noDiscount")}
                        </span>
                      )}
                      {item.overridden && (
                        <span className="shrink-0 inline-flex items-center rounded bg-primary/10 px-1.5 py-0.5 text-[10px] font-medium text-primary">
                          {t("overridePrice")}
                        </span>
                      )}
                    </div>
                    <div className="flex items-center gap-2 mt-1">
                      {item.overridden && (
                        <span className="text-sm text-muted-foreground line-through">
                          {item.originalUnitPrice.toFixed(2)}
                        </span>
                      )}
                      <span className="text-base font-medium">{item.unitPrice.toFixed(2)} {t("currency")} / {item.unitName}</span>
                    </div>
                    {ld > 0 && (
                      <div className="mt-1">
                        <span className="inline-flex items-center rounded bg-destructive/10 px-1.5 py-0.5 text-[10px] font-medium text-destructive">
                          {t("discount")} {item.discountValue}{item.discountType === 'percentage' ? '%' : ''} ({t("minus")} {ld.toFixed(2)})
                        </span>
                      </div>
                    )}
                    {item.priceEditNote && (
                      <div className="mt-1 flex items-start gap-1 text-xs text-muted-foreground">
                        <StickyNote className="h-3.5 w-3.5 shrink-0 mt-0.5" />
                        <span className="truncate">{item.priceEditNote}</span>
                      </div>
                    )}
                  </div>
                  <div className="flex items-center gap-3 shrink-0">
                    <div className="flex items-center gap-2">
                      <Button variant="outline" size="icon" className="h-10 w-10" onClick={() => updateQuantity(item.id, item.quantity - 1)} aria-label={t("decreaseQuantity")}>
                        <Minus className="h-5 w-5" />
                      </Button>
                      <Input
                        type="number"
                        min={1}
                        value={item.quantity}
                        onChange={(e) => {
                          const val = parseFloat(e.target.value)
                          if (!isNaN(val)) updateQuantity(item.id, val)
                        }}
                        className="w-20 h-12 text-lg text-center [appearance:textfield] [&::-webkit-inner-spin-button]:appearance-none [&::-webkit-outer-spin-button]:appearance-none"
                      />
                      <Button variant="outline" size="icon" className="h-10 w-10" onClick={() => updateQuantity(item.id, item.quantity + 1)} aria-label={t("increaseQuantity")}>
                        <Plus className="h-5 w-5" />
                      </Button>
                    </div>
                    <div className="w-24 text-right text-lg font-semibold">
                      {lineFinalTotal(item).toFixed(2)}
                    </div>
                    {(canPriceOverride || canLineDiscount) && (
                      <Button variant="ghost" size="icon" className="h-10 w-10" onClick={() => openLineEdit(item)} title={t("editLine")} aria-label={t("editLine")}>
                        <Pencil className="h-5 w-5" />
                      </Button>
                    )}
                    <Button variant="ghost" size="icon" onClick={() => removeItem(item.id)} className="text-destructive h-10 w-10" aria-label={t("remove")}>
                      <Trash2 className="h-5 w-5" />
                    </Button>
                  </div>
                </div>
              )
            })}
          </div>
        )}
      </CardContent>

      {editState && (
        <LineEditDialog
          initial={editState}
          canPriceOverride={canPriceOverride}
          canLineDiscount={canLineDiscount}
          onClose={() => setEditState(null)}
        />
      )}
    </Card>
  )
}
