"use client"

import { Product } from "@/lib/api"
import { usePOSStore } from "@/features/pos/store/usePOSStore"
import { Input } from "@/components/ui/input"
import { createInvoice } from "@/features/invoices/actions"
import { addProductBarcode } from "@/features/products/actions"
import { ProductForm, PRODUCT_FORM_ID } from "@/app/[locale]/products/product-form"
import { toast } from "sonner"
import { useState, useRef, useEffect, useCallback } from "react"
import { Loader2, Ban, PackagePlus, Link2, X } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Trash2, Plus, Minus, ArrowLeft } from "lucide-react"
import { Link } from "@/i18n/navigation"
import { useTranslations } from "next-intl"
import {
  Command,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
} from "@/components/ui/command"
import {
  Popover,
  PopoverPopup,
  PopoverPortal,
  PopoverPositioner,
  PopoverTrigger,
} from "@/components/ui/popover"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"

interface POSClientProps {
  products: Product[]
  onRefresh?: () => Promise<Product[]>
}

const allBarcodes = (p: Product): string[] => [
  ...(p.barcodes ?? []).map((b) => b.barcode),
  ...(p.barcode ? [p.barcode] : []),
]

export function POSClient({ products, onRefresh }: POSClientProps) {
  const t = useTranslations("POS")
  const {
    cartItems,
    searchQuery,
    setSearchQuery,
    addItem,
    removeItem,
    updateQuantity,
    discount,
    discountType,
    setDiscount,
    setDiscountType,
    toggleDiscountType,
    clearCart
  } = usePOSStore()

  const [open, setOpen] = useState(false)
  const [inputValue, setInputValue] = useState("")
  const inputRef = useRef<HTMLInputElement>(null)
  const triggerRef = useRef<HTMLButtonElement>(null)
  const discountInputRef = useRef<HTMLInputElement>(null)

  const [isCheckingOut, setIsCheckingOut] = useState(false)
  const [amountPaid, setAmountPaid] = useState(0)
  const paidTouched = useRef(false)

  const [unknownBarcode, setUnknownBarcode] = useState<string | null>(null)
  const [unknownFlow, setUnknownFlow] = useState<"options" | "create" | "link">("options")
  const [linkQuery, setLinkQuery] = useState("")
  const [selectedLinkProduct, setSelectedLinkProduct] = useState<Product | null>(null)

  const subtotal = cartItems.reduce((acc, item) => acc + (item.salePrice * item.quantity), 0)
  const eligibleSubtotal = cartItems
    .filter(item => item.allowDiscount)
    .reduce((acc, item) => acc + (item.salePrice * item.quantity), 0)

  const effectiveDiscount = discountType === 'percentage'
    ? Math.round(eligibleSubtotal * (discount / 100) * 100) / 100
    : Math.min(discount, eligibleSubtotal)

  const total = Math.max(0, subtotal - effectiveDiscount)

  const changeDue = Math.max(0, amountPaid - total)
  const canCheckout = cartItems.length > 0 && amountPaid >= total

  const totalItems = cartItems.reduce((acc, item) => acc + item.quantity, 0)

  useEffect(() => {
    if (open && inputRef.current) {
      inputRef.current.focus()
    }
  }, [open])

  useEffect(() => {
    if (!paidTouched.current) {
      setAmountPaid(total)
    }
  }, [total])

  const filteredProducts = products.filter((p) =>
    p.name.toLowerCase().includes(inputValue.toLowerCase()) ||
    allBarcodes(p).some((b) => b.includes(inputValue))
  )

  const findExact = useCallback((barcode: string) => {
    const b = barcode.trim()
    return products.find(
      (p) => p.barcode === b || (p.barcodes ?? []).some((x) => x.barcode === b)
    ) ?? null
  }, [products])

  const handleSelect = useCallback((product: Product) => {
    addItem(product)
    setInputValue("")
    setSearchQuery("")
    setOpen(false)
    triggerRef.current?.focus()
  }, [addItem, setSearchQuery])

  const openUnknownDialog = useCallback((barcode: string) => {
    setUnknownBarcode(barcode)
    setUnknownFlow("options")
    setLinkQuery("")
    setSelectedLinkProduct(null)
  }, [])

  const closeUnknownDialog = useCallback(() => {
    setUnknownBarcode(null)
    setSelectedLinkProduct(null)
    setLinkQuery("")
    setInputValue("")
    triggerRef.current?.focus()
  }, [])

  const handleLinkConfirm = async () => {
    if (!selectedLinkProduct || !unknownBarcode) return
    try {
      await addProductBarcode(selectedLinkProduct.id, unknownBarcode)
      toast.success(t("barcodeLinked"))
      addItem(selectedLinkProduct)
      if (onRefresh) await onRefresh()
    } catch (e) {
      toast.error((e as Error).message || t("linkFailed"))
    } finally {
      closeUnknownDialog()
    }
  }

  const handleCreatedProduct = async () => {
    if (!unknownBarcode) return
    try {
      const fresh = onRefresh ? await onRefresh() : products
      const created = fresh.find(
        (p) => p.barcode === unknownBarcode || (p.barcodes ?? []).some((x) => x.barcode === unknownBarcode)
      )
      if (created) addItem(created)
    } finally {
      toast.success(t("productCreated"))
      closeUnknownDialog()
    }
  }

  const linkedProducts = products.filter((p) => {
    const q = linkQuery.trim().toLowerCase()
    if (!q) return true
    return p.name.toLowerCase().includes(q) || allBarcodes(p).some((b) => b.toLowerCase().includes(q))
  })

  const validateDiscount = useCallback((): string | null => {
    if (discount <= 0 || (discountType === 'percentage' && discount > 100)) return null
    if (discountType === 'fixed' && discount > eligibleSubtotal) {
      return t("discountExceedsEligible")
    }
    if (discountType === 'percentage') {
      for (const item of cartItems) {
        if (!item.allowDiscount) continue
        const lineTotal = item.salePrice * item.quantity
        const lineDiscount = Math.round(lineTotal * (discount / 100) * 100) / 100
        const effectivePrice = item.quantity > 0 ? (lineTotal - lineDiscount) / item.quantity : item.salePrice
        if (effectivePrice < item.buyPrice) {
          return t("profitProtectionError")
        }
      }
    } else if (discountType === 'fixed' && discount > 0 && eligibleSubtotal > 0) {
      for (const item of cartItems) {
        if (!item.allowDiscount) continue
        const lineTotal = item.salePrice * item.quantity
        const lineDiscount = Math.round(discount * (lineTotal / eligibleSubtotal) * 100) / 100
        const effectivePrice = item.quantity > 0 ? (lineTotal - lineDiscount) / item.quantity : item.salePrice
        if (effectivePrice < item.buyPrice) {
          return t("profitProtectionError")
        }
      }
    }
    return null
  }, [cartItems, discount, discountType, eligibleSubtotal, t])

  const handleCheckout = useCallback(async () => {
    if (cartItems.length === 0) return
    const validationError = validateDiscount()
    if (validationError) {
      toast.error(validationError)
      return
    }
    setIsCheckingOut(true)
    try {
      await createInvoice(cartItems, effectiveDiscount, true, discountType || undefined, discount || undefined)
      toast.success(t("checkoutSuccess"))
      paidTouched.current = false
      clearCart()
    } catch (e) {
      toast.error(t("checkoutFailed"))
    } finally {
      setIsCheckingOut(false)
    }
  }, [cartItems, effectiveDiscount, discount, discountType, validateDiscount, t, clearCart])

  const handleCheckoutWithoutSave = useCallback(async () => {
    if (cartItems.length === 0) return
    const validationError = validateDiscount()
    if (validationError) {
      toast.error(validationError)
      return
    }
    setIsCheckingOut(true)
    try {
      await createInvoice(cartItems, effectiveDiscount, false, discountType || undefined, discount || undefined)
      toast.success(t("checkoutSuccess"))
      paidTouched.current = false
      clearCart()
    } catch (e) {
      toast.error(t("checkoutFailed"))
    } finally {
      setIsCheckingOut(false)
    }
  }, [cartItems, effectiveDiscount, discount, discountType, validateDiscount, t, clearCart])

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      const target = e.target as HTMLElement
      const isInput = target.tagName === 'INPUT' || target.tagName === 'TEXTAREA'

      if (e.key === 'F1') {
        e.preventDefault()
        if (!open) setOpen(true)
        requestAnimationFrame(() => inputRef.current?.focus())
        return
      }

      if (e.key === 'F2') {
        e.preventDefault()
        discountInputRef.current?.focus()
        discountInputRef.current?.select()
        return
      }
if (e.key === 'F11' && canCheckout) {
  e.preventDefault()
  handleCheckoutWithoutSave()
  return
}

      if ((e.key === 'F12' || (e.ctrlKey && e.key === 'Enter')) && canCheckout) {
        e.preventDefault()
        handleCheckout()
        return
      }

      if (e.ctrlKey && e.key === ' ') {
        e.preventDefault()
        toggleDiscountType()
        return
      }
    }

    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [open, handleCheckout, handleCheckoutWithoutSave, toggleDiscountType, canCheckout])

  return (
    <>
    <div className="flex h-full flex-col lg:flex-row gap-4 p-4 lg:p-6 bg-muted/40">
      <div className="flex flex-1 flex-col gap-4">
        <div className="flex items-center gap-4">
          <Link href="/">
            <Button variant="outline" size="icon">
              <ArrowLeft className="h-4 w-4" />
            </Button>
          </Link>
          <Popover open={open} onOpenChange={setOpen}>
            <PopoverTrigger
              ref={triggerRef}
              render={
                <Button
                  variant="outline"
                  className="w-full justify-between bg-background text-lg h-12 font-normal"
                />
              }
            >
              <span className="truncate">{inputValue || t("searchPlaceholder")}</span>
              <kbd className="ml-auto pointer-events-none inline-flex h-5 select-none items-center gap-1 rounded border bg-muted px-1.5 font-mono text-[10px] font-medium text-muted-foreground opacity-60">
                F1
              </kbd>
            </PopoverTrigger>
            <PopoverPortal>
              <PopoverPositioner>
                <PopoverPopup className="p-0 w-full" style={{ width: triggerRef.current?.offsetWidth }}>
                  <Command>
                    <CommandInput
                      placeholder={t("searchPlaceholder")}
                      ref={inputRef}
                      value={inputValue}
                      onChange={(e) => setInputValue(e.target.value)}
                      onKeyDown={(e) => {
                        if (e.key === "Enter") {
                          const trimmed = inputValue.trim()
                          if (trimmed) {
                            const exact = findExact(trimmed)
                            if (exact) {
                              handleSelect(exact)
                              return
                            }
                            if (/^\d{4,}$/.test(trimmed)) {
                              openUnknownDialog(trimmed)
                              return
                            }
                          }
                          if (filteredProducts.length > 0) {
                            handleSelect(filteredProducts[0])
                          }
                        }
                        if (e.key === "Escape") {
                          setOpen(false)
                          triggerRef.current?.focus()
                        }
                      }}
                    />
                    {inputValue.length >= 2 && (
                      <CommandList>
                        <CommandEmpty>{t("productNotFound")}</CommandEmpty>
                        <CommandGroup>
                          {filteredProducts.slice(0, 20).map((product) => (
                            <CommandItem
                              key={product.id}
                              onClick={() => handleSelect(product)}
                              onKeyDown={(e) => {
                                if (e.key === "Enter") {
                                  e.preventDefault()
                                  handleSelect(product)
                                }
                              }}
                            >
                              <div className="flex flex-1 items-center justify-between">
                                <span>{product.name}</span>
                                <span className="text-muted-foreground text-sm">
                                  {product.salePrice.toFixed(2)}
                                </span>
                              </div>
                            </CommandItem>
                          ))}
                        </CommandGroup>
                      </CommandList>
                    )}
                  </Command>
                </PopoverPopup>
              </PopoverPositioner>
            </PopoverPortal>
          </Popover>
        </div>

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
                {cartItems.map((item) => (
                  <div key={item.id} className="flex items-center justify-between border-b pb-4 gap-4">
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2">
                        <h4 className="text-lg font-semibold truncate">{item.name}</h4>
                        {!item.allowDiscount && (
                          <span className="shrink-0 inline-flex items-center gap-1 rounded bg-destructive/10 px-1.5 py-0.5 text-[10px] font-medium text-destructive">
                            <Ban className="h-3 w-3" />
                            {t("noDiscount")}
                          </span>
                        )}
                      </div>
                      <p className="text-base text-muted-foreground">{item.salePrice.toFixed(2)} {t("each")}</p>
                    </div>
                    <div className="flex items-center gap-4 shrink-0">
                      <div className="flex items-center gap-2">
                        <Button variant="outline" size="icon" className="h-10 w-10" onClick={() => updateQuantity(item.id, item.quantity - 1)}>
                          <Minus className="h-5 w-5" />
                        </Button>
                        <Input
                          type="number"
                          min={1}
                          max={item.maxStock}
                          value={item.quantity}
                          onChange={(e) => {
                            const val = parseInt(e.target.value, 10)
                            if (!isNaN(val)) updateQuantity(item.id, val)
                          }}
                          className="w-20 h-12 text-lg text-center [appearance:textfield] [&::-webkit-inner-spin-button]:appearance-none [&::-webkit-outer-spin-button]:appearance-none"
                        />
                        <Button variant="outline" size="icon" className="h-10 w-10" onClick={() => updateQuantity(item.id, item.quantity + 1)}>
                          <Plus className="h-5 w-5" />
                        </Button>
                      </div>
                      <div className="w-24 text-right text-lg font-semibold">
                        {(item.salePrice * item.quantity).toFixed(2)}
                      </div>
                      <Button variant="ghost" size="icon" onClick={() => removeItem(item.id)} className="text-destructive h-10 w-10">
                        <Trash2 className="h-5 w-5" />
                      </Button>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </CardContent>
        </Card>
      </div>

      <div className="w-full lg:w-96 flex flex-col gap-4">
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
                      onKeyDown={(e) => {
                        if (e.ctrlKey && e.key === ' ') {
                          e.preventDefault()
                          toggleDiscountType()
                        }
                      }}
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
             
              <Button className="h-16 text-lg" size="lg" onClick={handleCheckoutWithoutSave} disabled={isCheckingOut || !canCheckout}>
                {isCheckingOut && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                {t("save")}
      <kbd className="mr-2 pointer-events-none inline-flex h-5 select-none items-center gap-1 rounded border bg-primary-foreground/20 px-1.5 font-mono text-[10px] font-medium opacity-70">
  F11
</kbd>
                              </Button>

              <Button className="h-16 text-lg" size="lg" onClick={handleCheckout} disabled={isCheckingOut || !canCheckout}>
                {isCheckingOut && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                {t("saveAndPrint")}
                <kbd className="mr-2 pointer-events-none inline-flex h-5 select-none items-center gap-1 rounded border bg-primary-foreground/20 px-1.5 font-mono text-[10px] font-medium opacity-70">
                  F12
                </kbd>
                              </Button>

                 <Button variant="outline" className="h-16 col-span-2 text-lg" onClick={() => {
                paidTouched.current = false
                clearCart()
              }}>
                {t("clear")}
              </Button>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>

    <Dialog open={unknownBarcode !== null} onOpenChange={(o) => { if (!o) closeUnknownDialog() }}>
      <DialogContent className="sm:max-w-[440px]">
        {unknownBarcode !== null && unknownFlow === "options" && (
          <>
            <DialogHeader>
              <DialogTitle>{t("barcodeNotFoundTitle")}</DialogTitle>
              <DialogDescription>{t("barcodeNotFoundMessage")}</DialogDescription>
            </DialogHeader>
            <div className="py-2">
              <div className="flex items-center gap-2 text-sm text-muted-foreground">
                <span>{t("scannedBarcode")}:</span>
                <span className="rounded-lg bg-muted px-3 py-1 font-mono text-base text-foreground">{unknownBarcode}</span>
              </div>
            </div>
            <DialogFooter className="flex-col gap-2 sm:flex-col">
              <Button onClick={() => setUnknownFlow("create")}>
                <PackagePlus className="mr-2 h-4 w-4" /> {t("createNewProduct")}
              </Button>
              <Button variant="outline" onClick={() => setUnknownFlow("link")}>
                <Link2 className="mr-2 h-4 w-4" /> {t("linkToExistingProduct")}
              </Button>
              <Button variant="ghost" onClick={closeUnknownDialog}>
                {t("cancel")}
              </Button>
            </DialogFooter>
          </>
        )}

        {unknownBarcode !== null && unknownFlow === "create" && (
          <>
            <DialogHeader>
              <DialogTitle>{t("createNewProduct")}</DialogTitle>
              <DialogDescription>{t("createNewProductDescription")}</DialogDescription>
            </DialogHeader>
            <div className="py-2">
              <ProductForm
                key={unknownBarcode}
                defaultBarcode={unknownBarcode}
                onSuccess={handleCreatedProduct}
              />
            </div>
            <DialogFooter className="flex-col gap-2 sm:flex-row">
              <Button variant="outline" onClick={() => setUnknownFlow("options")}>
                {t("back")}
              </Button>
              <Button type="submit" form={PRODUCT_FORM_ID}>
                {t("save")}
              </Button>
            </DialogFooter>
          </>
        )}

        {unknownBarcode !== null && unknownFlow === "link" && (
          <>
            <DialogHeader>
              <DialogTitle>{t("linkToExistingProduct")}</DialogTitle>
              <DialogDescription>{t("linkToExistingProductDescription")}</DialogDescription>
            </DialogHeader>
            <div className="py-2 space-y-3">
              <div className="flex items-center gap-2 rounded-lg border bg-muted/50 px-3 py-1.5 font-mono text-base">
                {unknownBarcode}
              </div>
              <Command className="rounded-lg border">
                <CommandInput
                  placeholder={t("searchProduct")}
                  value={linkQuery}
                  onChange={(e) => setLinkQuery(e.target.value)}
                  autoFocus
                />
                <CommandList>
                  <CommandEmpty>{t("noResults")}</CommandEmpty>
                  <CommandGroup>
                    {linkedProducts.slice(0, 10).map((p) => (
                      <CommandItem
                        key={p.id}
                        onSelect={() => setSelectedLinkProduct(p)}
                        className={selectedLinkProduct?.id === p.id ? "bg-primary/10" : undefined}
                      >
                        <div className="flex flex-1 items-center justify-between">
                          <span>{p.name}</span>
                          <span className="text-muted-foreground text-sm">{p.salePrice.toFixed(2)}</span>
                        </div>
                      </CommandItem>
                    ))}
                  </CommandGroup>
                </CommandList>
              </Command>
              {selectedLinkProduct && (
                <div className="flex items-center justify-between rounded-lg border px-3 py-2 text-sm">
                  <span className="font-medium">{selectedLinkProduct.name}</span>
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => setSelectedLinkProduct(null)}
                    aria-label={t("clear")}
                  >
                    <X className="h-4 w-4" />
                  </Button>
                </div>
              )}
            </div>
            <DialogFooter className="flex-col gap-2 sm:flex-row">
              <Button variant="outline" onClick={() => setUnknownFlow("options")}>
                {t("back")}
              </Button>
              <Button onClick={handleLinkConfirm} disabled={!selectedLinkProduct}>
                {t("confirmLink")}
              </Button>
            </DialogFooter>
          </>
        )}
      </DialogContent>
    </Dialog>
    </>
  )
}
