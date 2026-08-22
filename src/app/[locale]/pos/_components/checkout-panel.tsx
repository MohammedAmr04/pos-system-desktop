"use client"

import { useCallback, useEffect, useRef, useState } from "react"
import { useTranslations } from "next-intl"
import { toast } from "sonner"
import { Loader2 } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { usePOSStore } from "@/store/pos.store"
import { useAuth } from "@/components/common/auth-context"
import { FEATURES, PERMISSIONS } from "@/lib/constants"
import { cartTotals, lineDiscountAmount, lineSubtotal, round2 } from "./utils/pricing"
import { createInvoice } from "@/actions/invoices.actions"

export function CheckoutPanel() {
  const t = useTranslations("POS")
  const { hasAccess } = useAuth()
  const canInvoiceDiscount = hasAccess(PERMISSIONS.DISCOUNTS_INVOICE, FEATURES.INVOICE_DISCOUNT)
  const canPrintReceipt = hasAccess(PERMISSIONS.PRINTING_RECEIPT, FEATURES.RECEIPT_PRINTING)

  const cartItems = usePOSStore((s) => s.cartItems)
  const discount = usePOSStore((s) => s.discount)
  const discountType = usePOSStore((s) => s.discountType)
  const priceMode = usePOSStore((s) => s.priceMode)
  const setDiscount = usePOSStore((s) => s.setDiscount)
  const toggleDiscountType = usePOSStore((s) => s.toggleDiscountType)
  const clearCart = usePOSStore((s) => s.clearCart)

  const [isCheckingOut, setIsCheckingOut] = useState(false)
  const [amountPaid, setAmountPaid] = useState(0)
  const paidTouched = useRef(false)
  const discountInputRef = useRef<HTMLInputElement>(null)

  const { subtotal, itemsDiscount, eligibleSubtotal, effectiveDiscount, total } =
    cartTotals(cartItems, discount, discountType)

  const changeDue = Math.max(0, amountPaid - total)
  const canCheckout = cartItems.length > 0 && amountPaid >= total

  useEffect(() => {
    if (!paidTouched.current) {
      setAmountPaid(total)
    }
  }, [total])

  const validateDiscount = useCallback((): string | null => {
    if (discount <= 0 || (discountType === 'percentage' && discount > 100)) return null
    if (discountType === 'fixed' && discount > eligibleSubtotal) {
      return t("discountExceedsEligible")
    }
    for (const item of cartItems) {
      const ls = lineSubtotal(item)
      const ld = lineDiscountAmount(item)
      const effBefore = item.quantity > 0 ? (ls - ld) / item.quantity : 0
      if (effBefore < item.buyPrice) {
        return t("profitProtectionError")
      }
      if (item.allowDiscount && effectiveDiscount > 0 && eligibleSubtotal > 0) {
        const share = round2(effectiveDiscount * ((ls - ld) / eligibleSubtotal))
        const effAfter = item.quantity > 0 ? (ls - ld - share) / item.quantity : 0
        if (effAfter < item.buyPrice) {
          return t("profitProtectionError")
        }
      }
    }
    return null
  }, [cartItems, discount, discountType, eligibleSubtotal, effectiveDiscount, t])

  const handleCheckout = useCallback(async (print: boolean) => {
    if (cartItems.length === 0) return
    if (print && !canPrintReceipt) print = false
    const validationError = validateDiscount()
    if (validationError) {
      toast.error(validationError)
      return
    }
    setIsCheckingOut(true)
    try {
      await createInvoice(cartItems, effectiveDiscount, print, discountType || undefined, discount || undefined, priceMode)
      toast.success(t("checkoutSuccess"))
      paidTouched.current = false
      clearCart()
    } catch (e) {
      toast.error((e as Error).message || t("checkoutFailed"))
    } finally {
      setIsCheckingOut(false)
    }
  }, [cartItems, effectiveDiscount, discount, discountType, priceMode, validateDiscount, t, clearCart, canPrintReceipt])

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'F2') {
        e.preventDefault()
        discountInputRef.current?.focus()
        discountInputRef.current?.select()
        return
      }
      if (e.key === 'F11' && canCheckout) {
        e.preventDefault()
        handleCheckout(false)
        return
      }
      if ((e.key === 'F12' || (e.ctrlKey && e.key === 'Enter')) && canCheckout && canPrintReceipt) {
        e.preventDefault()
        handleCheckout(true)
        return
      }
      if (e.ctrlKey && e.key === ' ') {
        e.preventDefault()
        toggleDiscountType()
      }
    }

    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [handleCheckout, toggleDiscountType, canCheckout, canPrintReceipt])

  return (
    <Card className="flex-1 flex flex-col">
      <CardHeader>
        <CardTitle>{t("checkout")}</CardTitle>
      </CardHeader>
      <CardContent className="flex-1 flex flex-col justify-end gap-6">
        <div className="space-y-6">
          <div className="flex justify-between text-xl">
            <span className="text-muted-foreground">{t("subtotal")}</span>
            <span className="font-semibold">{subtotal.toFixed(2)}</span>
          </div>
          {itemsDiscount > 0 && (
            <div className="flex justify-between text-lg">
              <span className="text-muted-foreground">{t("lineDiscountTotal")}</span>
              <span className="text-destructive">-{itemsDiscount.toFixed(2)}</span>
            </div>
          )}
          {eligibleSubtotal > 0 && canInvoiceDiscount && (
            <div className="flex items-center justify-between gap-4">
              <span className="text-muted-foreground text-xl">{t("discount")}</span>
              <div className="flex items-center gap-2">
                <div className="relative w-32">
                  <Input
                    ref={discountInputRef}
                    type="number"
                    min="0"
                    step={discountType === 'percentage' ? "1" : "0.01"}
                    max={discountType === 'percentage' ? "100" : undefined}
                    className="text-right text-lg h-12"
                    value={discount || ""}
                    onChange={(e) => setDiscount(parseFloat(e.target.value) || 0)}
                  />
                </div>
                <Button
                  variant="ghost"
                  size="icon"
                  className="h-12 w-12 text-xl font-semibold"
                  onClick={toggleDiscountType}
                  title={discountType === 'fixed' ? t("discountTypePercentage") : t("discountTypeFixed")}
                >
                  {discountType === 'fixed' ? t("currency") : '%'}
                </Button>
                <kbd className="pointer-events-none inline-flex h-5 select-none items-center gap-1 rounded border bg-muted px-1.5 font-mono text-[10px] font-medium text-muted-foreground opacity-60">
                  ^␣
                </kbd>
              </div>
            </div>
          )}
          <div className="border-t pt-4 flex justify-between text-3xl font-bold">
            <span>{t("total")}</span>
            <span>{total.toFixed(2)}</span>
          </div>

          <div className="space-y-4 border-t pt-4">
            <h4 className="text-lg font-semibold">{t("totalDue")}</h4>
            <div className="flex justify-between text-2xl font-bold">
              <span className="text-muted-foreground">{t("totalDue")}</span>
              <span>{total.toFixed(2)}</span>
            </div>
            <div className="flex items-center justify-between gap-4">
              <span className="text-muted-foreground text-xl">{t("amountPaid")}</span>
              <Input
                type="number"
                min="0"
                step="0.01"
                className="w-40 text-right text-lg h-12"
                value={amountPaid || ""}
                onChange={(e) => {
                  paidTouched.current = true
                  setAmountPaid(parseFloat(e.target.value) || 0)
                }}
                onFocus={() => {
                  if (!paidTouched.current) {
                    paidTouched.current = true
                  }
                }}
              />
            </div>
            <div className="flex justify-between text-2xl font-bold">
              <span className="text-muted-foreground">{t("changeDue")}</span>
              <span className={changeDue > 0 ? "text-green-600" : "text-muted-foreground"}>
                {changeDue.toFixed(2)}
              </span>
            </div>
            {amountPaid > 0 && amountPaid < total && (
              <p className="text-destructive text-sm font-medium">
                {t("insufficientPayment")}
              </p>
            )}
          </div>
        </div>

        <div className="grid grid-cols-2 gap-4 mt-8">
          <Button className="h-16 text-lg" size="lg" onClick={() => handleCheckout(false)} disabled={isCheckingOut || !canCheckout}>
            {isCheckingOut && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
            {t("save")}
            <kbd className="mr-2 pointer-events-none inline-flex h-5 select-none items-center gap-1 rounded border bg-primary-foreground/20 px-1.5 font-mono text-[10px] font-medium opacity-70">
              F11
            </kbd>
          </Button>

          {canPrintReceipt && (
            <Button className="h-16 text-lg" size="lg" onClick={() => handleCheckout(true)} disabled={isCheckingOut || !canCheckout}>
              {isCheckingOut && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              {t("saveAndPrint")}
              <kbd className="mr-2 pointer-events-none inline-flex h-5 select-none items-center gap-1 rounded border bg-primary-foreground/20 px-1.5 font-mono text-[10px] font-medium opacity-70">
                F12
              </kbd>
            </Button>
          )}

          <Button variant="outline" className="h-16 col-span-2 text-lg" onClick={() => {
            paidTouched.current = false
            clearCart()
          }}>
            {t("clear")}
          </Button>
        </div>
      </CardContent>
    </Card>
  )
}
