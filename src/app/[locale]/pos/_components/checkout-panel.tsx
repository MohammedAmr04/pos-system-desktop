"use client"

import { useCallback, useEffect, useRef, useState } from "react"
import { useQueryClient } from "@tanstack/react-query"
import { useTranslations } from "next-intl"
import { useApiError } from "@/lib/api-error"
import { toast } from "sonner"
import { Loader2 } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { TooltipIconButton } from "@/components/common/tooltip-icon-button"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { usePOSStore } from "@/store/pos.store"
import { useAuth } from "@/components/common/auth-context"
import { FEATURES, PERMISSIONS } from "@/lib/constants"
import { cartTotals, lineDiscountAmount, lineSubtotal, round2 } from "./utils/pricing"
import { createInvoice, saveDraftInvoice } from "@/actions/invoices.actions"
import { postInvoice } from "@/actions/invoices.lifecycle.actions"
import { invoicesKeys } from "@/hooks/use-invoices"
import { shiftsKeys, useActiveShift } from "@/hooks/use-shifts"
import { useActiveClients } from "@/hooks/use-clients"
import { Client } from "@/types/domain/domain.types"

export function CheckoutPanel() {
  const t = useTranslations("POS")
  const resolveError = useApiError()
  const { hasAccess } = useAuth()
  const canInvoiceDiscount = hasAccess(PERMISSIONS.DISCOUNTS_INVOICE, FEATURES.INVOICE_DISCOUNT)
  const canPrintReceipt = hasAccess(PERMISSIONS.PRINTING_RECEIPT, FEATURES.RECEIPT_PRINTING)

  const cartItems = usePOSStore((s) => s.cartItems)
  const discount = usePOSStore((s) => s.discount)
  const discountType = usePOSStore((s) => s.discountType)
  const priceMode = usePOSStore((s) => s.priceMode)
  const clientId = usePOSStore((s) => s.clientId)
  const paymentMethod = usePOSStore((s) => s.paymentMethod)
  const draftId = usePOSStore((s) => s.draftId)
  const setClient = usePOSStore((s) => s.setClient)
  const setPaymentMethod = usePOSStore((s) => s.setPaymentMethod)
  const setDiscount = usePOSStore((s) => s.setDiscount)
  const toggleDiscountType = usePOSStore((s) => s.toggleDiscountType)
  const clearCart = usePOSStore((s) => s.clearCart)

  const [isCheckingOut, setIsCheckingOut] = useState(false)
  const [amountPaid, setAmountPaid] = useState(0)
  const [confirmClear, setConfirmClear] = useState(false)
  const paidTouched = useRef(false)
  const discountInputRef = useRef<HTMLInputElement>(null)
  const queryClient = useQueryClient()

  const { data: activeShift } = useActiveShift()
  const { data: allClients = [] } = useActiveClients()
  const clients: Client[] = allClients

  const refreshAfterSale = useCallback(async () => {
    await queryClient.invalidateQueries({ queryKey: invoicesKeys.all })
    if (activeShift) await queryClient.invalidateQueries({ queryKey: shiftsKeys.all })
  }, [queryClient, activeShift])

  const { subtotal, itemsDiscount, eligibleSubtotal, effectiveDiscount, total } =
    cartTotals(cartItems, discount, discountType)

  const isCredit = paymentMethod === 'credit'
  const changeDue = Math.max(0, amountPaid - total)
  const canCheckout = cartItems.length > 0 && !!activeShift && (isCredit || amountPaid >= total)

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
    if (!activeShift) {
      toast.error(t("requiresOpenShift"))
      return
    }
    if (paymentMethod === 'credit' && !clientId) {
      toast.error(t("creditRequiresClient"))
      return
    }
    if (print && !canPrintReceipt) print = false
    const validationError = validateDiscount()
    if (validationError) {
      toast.error(validationError)
      return
    }
    setIsCheckingOut(true)
    try {
      if (draftId) {
        await saveDraftInvoice(cartItems, effectiveDiscount, discountType || undefined, discount || undefined, priceMode, {
          clientId,
          paymentMethod,
          status: 'posted',
          draftId,
        })
        await postInvoice(draftId)
      } else {
        await createInvoice(cartItems, effectiveDiscount, print, discountType || undefined, discount || undefined, priceMode, {
          clientId,
          paymentMethod,
          status: 'posted',
        })
      }
      await refreshAfterSale()
      toast.success(t("checkoutSuccess"))
      paidTouched.current = false
      clearCart()
    } catch (e) {
      toast.error(resolveError(e) || t("checkoutFailed"))
    } finally {
      setIsCheckingOut(false)
    }
  }, [cartItems, effectiveDiscount, discount, discountType, priceMode, clientId, paymentMethod, draftId, validateDiscount, t, clearCart, canPrintReceipt, refreshAfterSale, activeShift, resolveError])

  const handleSaveDraft = useCallback(async () => {
    if (cartItems.length === 0) return
    if (!activeShift) {
      toast.error(t("requiresOpenShift"))
      return
    }
    if (paymentMethod === 'credit' && !clientId) {
      toast.error(t("creditRequiresClient"))
      return
    }
    setIsCheckingOut(true)
    try {
      await saveDraftInvoice(cartItems, effectiveDiscount, discountType || undefined, discount || undefined, priceMode, {
        clientId,
        paymentMethod,
        status: 'draft',
        draftId,
      })
      await refreshAfterSale()
      toast.success(t("draftSaved"))
      paidTouched.current = false
      clearCart()
    } catch (e) {
      toast.error(resolveError(e) || t("checkoutFailed"))
    } finally {
      setIsCheckingOut(false)
    }
  }, [cartItems, effectiveDiscount, discount, discountType, priceMode, clientId, paymentMethod, draftId, t, clearCart, refreshAfterSale, activeShift, resolveError])

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      // Skip when a dialog is open
      if (document.querySelector('[data-slot="dialog-content"], [role="dialog"]')) return
      // Skip when the target is a form element (except F11/F12 checkout keys)
      const el = document.activeElement
      const editable = el && (el.tagName === 'INPUT' || el.tagName === 'TEXTAREA' || (el as HTMLElement).isContentEditable)

      if (e.key === 'F2') {
        if (!canInvoiceDiscount) return
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
        if (editable || !canInvoiceDiscount) return
        e.preventDefault()
        toggleDiscountType()
      }
    }

    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [handleCheckout, toggleDiscountType, canCheckout, canPrintReceipt, canInvoiceDiscount])

  return (
    <Card className="flex-1 flex flex-col">
      <CardHeader>
        <CardTitle>{t("checkout")}</CardTitle>
      </CardHeader>
      <CardContent className="flex-1 flex flex-col justify-end gap-6">
        <div className="space-y-6">
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1.5">
              <label htmlFor="pos-client" className="text-sm font-medium">{t("client")}</label>
              <Select
                value={clientId ?? ""}
                onValueChange={(v) => setClient(v || null)}
                items={{
                  "": t("walkIn"),
                  ...Object.fromEntries(clients.filter((c) => c.isActive).map((c) => [c.id, c.name])),
                }}
              >
                <SelectTrigger id="pos-client" className="w-full">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="">{t("walkIn")}</SelectItem>
                  {clients.filter((c) => c.isActive).map((c) => (
                    <SelectItem key={c.id} value={c.id}>{c.name}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-1.5">
              <label htmlFor="pos-payment" className="text-sm font-medium">{t("paymentMethod")}</label>
              <Select
                value={paymentMethod}
                onValueChange={(v) => v && setPaymentMethod(v as 'cash' | 'credit' | 'card' | 'bank_transfer')}
                items={{
                  cash: t("cash"),
                  credit: t("credit"),
                  card: t("card"),
                  bank_transfer: t("bankTransfer"),
                }}
              >
                <SelectTrigger id="pos-payment" className="w-full">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="cash">{t("cash")}</SelectItem>
                  <SelectItem value="credit">{t("credit")}</SelectItem>
                  <SelectItem value="card">{t("card")}</SelectItem>
                  <SelectItem value="bank_transfer">{t("bankTransfer")}</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>
          {draftId && (
            <p className="rounded-md bg-amber-500/10 px-3 py-2 text-xs font-medium text-amber-600">
              {t("editingDraft")}
            </p>
          )}
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
                    inputMode="decimal"
                    dir="ltr"
                    min="0"
                    step={discountType === 'percentage' ? "1" : "0.01"}
                    max={discountType === 'percentage' ? "100" : undefined}
                    aria-label={t("discount")}
                    className="text-right text-lg h-12"
                    value={discount || ""}
                    onChange={(e) => setDiscount(parseFloat(e.target.value) || 0)}
                  />
                  {discount > 0 && discountType === 'percentage' && discount > 100 && (
                    <p className="absolute -bottom-4 left-0 text-[10px] font-medium text-destructive">
                      {t("maxPercentageDiscount")}
                    </p>
                  )}
                </div>
                <TooltipIconButton
                  label={discountType === 'fixed' ? t("discountTypePercentage") : t("discountTypeFixed")}
                  className="h-12 w-12 text-xl font-semibold"
                  onClick={toggleDiscountType}
                >
                  {discountType === 'fixed' ? t("currency") : '%'}
                </TooltipIconButton>
                <kbd className="pointer-events-none inline-flex h-5 select-none items-center gap-1 rounded border bg-muted px-1.5 font-mono text-[10px] font-medium text-muted-foreground opacity-60">
                  F2
                </kbd>
              </div>
            </div>
          )}
          <div className="border-t pt-4 flex justify-between text-3xl font-bold">
            <span>{t("total")}</span>
            <span>{total.toFixed(2)}</span>
          </div>

          {!isCredit ? (
            <div className="space-y-4 border-t pt-4">
              <div className="flex items-center justify-between gap-4">
                <span className="text-muted-foreground text-xl">{t("amountPaid")}</span>
                <Input
                  type="number"
                  inputMode="decimal"
                  dir="ltr"
                  min="0"
                  step="1"
                  aria-label={t("amountPaid")}
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
              <div className="flex flex-wrap gap-2">
                {[5, 10, 20, 50, 100, 200]
                  .filter((n) => n < total)
                  .map((n) => (
                    <Button
                      key={n}
                      type="button"
                      variant="outline"
                      size="sm"
                      className="h-9 min-w-16"
                      onClick={() => {
                        paidTouched.current = true
                        setAmountPaid(n)
                      }}
                    >
                      {n.toFixed(0)}
                    </Button>
                  ))}
                <Button
                  type="button"
                  variant="secondary"
                  size="sm"
                  className="h-9 min-w-16"
                  onClick={() => {
                    paidTouched.current = true
                    setAmountPaid(total)
                  }}
                >
                  {t("exactAmount")}
                </Button>
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
          ) : (
            <div className="flex items-center justify-between rounded-md bg-primary/10 px-3 py-2">
              <span className="text-sm font-medium text-primary">{t("creditDue")}</span>
              <span className="text-lg font-bold text-primary">{total.toFixed(2)}</span>
            </div>
          )}
        </div>

        <div className="grid grid-cols-2 gap-4 mt-8">
          {!activeShift && cartItems.length > 0 && (
            <p className="col-span-2 rounded-md bg-amber-500/10 px-3 py-2 text-center text-xs font-medium text-amber-600">
              {t("requiresOpenShift")}
            </p>
          )}
          <Button variant="outline" className="h-16 text-lg" onClick={handleSaveDraft} disabled={isCheckingOut || cartItems.length === 0 || !activeShift}>
            {t("saveDraft")}
          </Button>

          <Button className="h-16 text-lg" size="lg" onClick={() => handleCheckout(false)} disabled={isCheckingOut || !canCheckout}>
            {isCheckingOut && <Loader2 className="ms-2 h-4 w-4 animate-spin" />}
            {draftId ? t("saveAndPostDraft") : t("save")}
            <kbd className="ms-2 pointer-events-none inline-flex h-5 select-none items-center gap-1 rounded border bg-primary-foreground/20 px-1.5 font-mono text-[10px] font-medium opacity-70">
              F11
            </kbd>
          </Button>

          {canPrintReceipt && (
            <Button className="h-16 text-lg" size="lg" onClick={() => handleCheckout(true)} disabled={isCheckingOut || !canCheckout}>
              {isCheckingOut && <Loader2 className="ms-2 h-4 w-4 animate-spin" />}
              {t("saveAndPrint")}
              <kbd className="ms-2 pointer-events-none inline-flex h-5 select-none items-center gap-1 rounded border bg-primary-foreground/20 px-1.5 font-mono text-[10px] font-medium opacity-70">
                F12
              </kbd>
            </Button>
          )}

          <Button variant="outline" className="h-16 col-span-2 text-lg"
            disabled={cartItems.length === 0}
            onClick={() => setConfirmClear(true)}>
            {t("clear")}
          </Button>
        </div>
      </CardContent>

      <Dialog open={confirmClear} onOpenChange={setConfirmClear}>
        <DialogContent showCloseButton={false}>
          <DialogHeader>
            <DialogTitle>{t("confirmClearTitle")}</DialogTitle>
            <DialogDescription>{t("confirmClearDescription")}</DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setConfirmClear(false)}>{t("cancel")}</Button>
            <Button
              className="text-destructive-foreground"
              variant="destructive"
              onClick={() => {
                paidTouched.current = false
                clearCart()
                setConfirmClear(false)
              }}
            >
              {t("confirmClear")}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </Card>
  )
}
