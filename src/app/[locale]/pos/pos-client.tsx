"use client"

import { Product, ProductUnit } from "@/lib/api"
import { usePOSStore, CartItem } from "@/features/pos/store/usePOSStore"
import { findUnitByBarcode } from "@/features/products/actions"
import { Input } from "@/components/ui/input"
import { createInvoice } from "@/features/invoices/actions"
import { addProductBarcode } from "@/features/products/actions"
import { ProductForm, PRODUCT_FORM_ID } from "@/app/[locale]/products/product-form"
import { toast } from "sonner"
import { useState, useRef, useEffect, useCallback } from "react"
import { Loader2, Ban, PackagePlus, Link2, X, Boxes, Pencil, StickyNote } from "lucide-react"
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

const round2 = (n: number) => Math.round(n * 100) / 100

const lineSubtotal = (item: CartItem) => round2(item.unitPrice * item.quantity)
const lineDiscountAmount = (item: CartItem) =>
  item.discountType === 'percentage'
    ? round2(lineSubtotal(item) * (item.discountValue ?? 0) / 100)
    : item.discountType === 'fixed'
      ? Math.min(item.discountValue ?? 0, lineSubtotal(item))
      : 0
const lineFinalTotal = (item: CartItem) => round2(lineSubtotal(item) - lineDiscountAmount(item))

const allBarcodes = (p: Product): string[] => [
  ...(p.barcodes ?? []).map((b) => b.barcode),
  ...(p.barcode ? [p.barcode] : []),
]

const baseUnitOf = (p: Product): ProductUnit | null =>
  p.units?.find((u) => u.isBaseUnit) ?? p.units?.[0] ?? null

const resolveBarcode = (list: Product[], barcode: string): { product: Product; unit: ProductUnit } | null => {
  const b = barcode.trim()
  for (const p of list) {
    const unit = findUnitByBarcode(p, b)
    if (unit) return { product: p, unit }
    if (p.barcode === b) {
      const bu = baseUnitOf(p)
      if (bu) return { product: p, unit: bu }
    }
  }
  return null
}

interface UnitPickerState {
  product: Product
  onPick: (unit: ProductUnit) => void
}

interface LineEditState {
  item: CartItem
  unitPrice: string
  note: string
  discountType: 'percentage' | 'fixed'
  discountValue: string
}

export function POSClient({ products, onRefresh }: POSClientProps) {
  const t = useTranslations("POS")
  const {
    cartItems,
    setSearchQuery,
    addItem,
    removeItem,
    updateQuantity,
    updateUnitPrice,
    setLineDiscount,
    clearLineDiscount,
    discount,
    discountType,
    setDiscount,
    toggleDiscountType,
    priceMode,
    setPriceMode,
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

  const [unitPicker, setUnitPicker] = useState<UnitPickerState | null>(null)
  const [lineEditFor, setLineEditFor] = useState<LineEditState | null>(null)
  const [triggerWidth, setTriggerWidth] = useState(0)

  const subtotal = cartItems.reduce((acc, item) => acc + lineSubtotal(item), 0)
  const itemsDiscount = cartItems.reduce((acc, item) => acc + lineDiscountAmount(item), 0)
  const eligibleSubtotal = cartItems
    .filter(item => item.allowDiscount)
    .reduce((acc, item) => acc + lineFinalTotal(item), 0)

  const effectiveDiscount = discountType === 'percentage'
    ? round2(eligibleSubtotal * (discount / 100))
    : Math.min(discount, eligibleSubtotal)

  const total = Math.max(0, subtotal - itemsDiscount - effectiveDiscount)

  const changeDue = Math.max(0, amountPaid - total)
  const canCheckout = cartItems.length > 0 && amountPaid >= total

  const totalItems = cartItems.reduce((acc, item) => acc + item.quantity, 0)

  useEffect(() => {
    if (open && triggerRef.current) {
      setTriggerWidth(triggerRef.current.offsetWidth)
    }
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

  const addAndClose = useCallback((product: Product, unit: ProductUnit) => {
    addItem(product, unit)
    setInputValue("")
    setSearchQuery("")
    setOpen(false)
    setUnitPicker(null)
    triggerRef.current?.focus()
  }, [addItem, setSearchQuery])

  const handleSelect = useCallback((product: Product) => {
    const units = product.units?.length ? product.units : []
    if (units.length === 1) {
      addAndClose(product, units[0])
    } else if (units.length > 1) {
      setOpen(false)
      setUnitPicker({ product, onPick: (unit) => addAndClose(product, unit) })
    }
  }, [addAndClose])

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

  const doLink = useCallback(async (product: Product, unit: ProductUnit) => {
    if (!unknownBarcode) return
    try {
      await addProductBarcode(product.id, unit.id, unknownBarcode)
      toast.success(t("barcodeLinked"))
      addItem(product, unit)
      if (onRefresh) await onRefresh()
    } catch (e) {
      toast.error((e as Error).message || t("linkFailed"))
    } finally {
      closeUnknownDialog()
      setUnitPicker(null)
    }
  }, [unknownBarcode, onRefresh, addItem, closeUnknownDialog, t])

  const handleLinkConfirm = () => {
    if (!selectedLinkProduct || !unknownBarcode) return
    const units = selectedLinkProduct.units?.length ? selectedLinkProduct.units : []
    if (units.length === 1) {
      doLink(selectedLinkProduct, units[0])
    } else if (units.length > 1) {
      setUnitPicker({ product: selectedLinkProduct, onPick: (unit) => doLink(selectedLinkProduct, unit) })
      closeUnknownDialog()
    }
  }

  const handleCreatedProduct = useCallback(async () => {
    if (!unknownBarcode) return
    try {
      const fresh = onRefresh ? await onRefresh() : products
      const resolved = resolveBarcode(fresh, unknownBarcode)
      if (resolved) addItem(resolved.product, resolved.unit)
    } finally {
      toast.success(t("productCreated"))
      closeUnknownDialog()
    }
  }, [unknownBarcode, onRefresh, products, addItem, closeUnknownDialog, t])

  const linkedProducts = products.filter((p) => {
    const q = linkQuery.trim().toLowerCase()
    if (!q) return true
    return p.name.toLowerCase().includes(q) || allBarcodes(p).some((b) => b.toLowerCase().includes(q))
  })

  const openLineEdit = (item: CartItem) => {
    setLineEditFor({
      item,
      unitPrice: String(item.unitPrice),
      note: item.priceEditNote ?? '',
      discountType: item.discountType ?? 'percentage',
      discountValue: item.discountValue && item.discountValue > 0 ? String(item.discountValue) : '',
    })
  }

  const saveLineEdit = () => {
    if (!lineEditFor) return
    const { item, unitPrice, note, discountType, discountValue } = lineEditFor
    const price = parseFloat(unitPrice)
    if (isNaN(price) || price < 0) return
    updateUnitPrice(item.id, price, note.trim() || undefined)
    const dValue = parseFloat(discountValue)
    if (dValue > 0) {
      setLineDiscount(item.id, discountType, dValue)
    } else {
      clearLineDiscount(item.id)
    }
    setLineEditFor(null)
  }

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
  }, [cartItems, effectiveDiscount, discount, discountType, priceMode, validateDiscount, t, clearCart])

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
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
  handleCheckout(false)
  return
}

      if ((e.key === 'F12' || (e.ctrlKey && e.key === 'Enter')) && canCheckout) {
        e.preventDefault()
        handleCheckout(true)
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
  }, [open, handleCheckout, toggleDiscountType, canCheckout])

  return (
    <>
    <div className="flex h-full flex-col lg:flex-row gap-4 p-4 lg:p-6 bg-muted/40">
      <div className="flex flex-1 flex-col gap-4">
        <div className="flex items-center gap-3">
          <Link href="/">
            <Button variant="outline" size="icon">
              <ArrowLeft className="h-4 w-4" />
            </Button>
          </Link>
          <div className="flex shrink-0 rounded-lg border bg-background p-1">
            <Button
              variant={priceMode === 'retail' ? 'default' : 'ghost'}
              size="sm"
              className="h-8 px-3"
              onClick={() => setPriceMode('retail')}
            >
              {t("retail")}
            </Button>
            <Button
              variant={priceMode === 'wholesale' ? 'default' : 'ghost'}
              size="sm"
              className="h-8 px-3"
              onClick={() => setPriceMode('wholesale')}
            >
              {t("wholesale")}
            </Button>
          </div>
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
                <PopoverPopup className="p-0 w-full" style={{ width: triggerWidth || undefined }}>
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
                            const exact = resolveBarcode(products, trimmed)
                            if (exact) {
                              addAndClose(exact.product, exact.unit)
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
                          <Button variant="outline" size="icon" className="h-10 w-10" onClick={() => updateQuantity(item.id, item.quantity - 1)}>
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
                          <Button variant="outline" size="icon" className="h-10 w-10" onClick={() => updateQuantity(item.id, item.quantity + 1)}>
                            <Plus className="h-5 w-5" />
                          </Button>
                        </div>
                        <div className="w-24 text-right text-lg font-semibold">
                          {lineFinalTotal(item).toFixed(2)}
                        </div>
                        <Button variant="ghost" size="icon" className="h-10 w-10" onClick={() => openLineEdit(item)} title={t("editLine")}>
                          <Pencil className="h-5 w-5" />
                        </Button>
                        <Button variant="ghost" size="icon" onClick={() => removeItem(item.id)} className="text-destructive h-10 w-10">
                          <Trash2 className="h-5 w-5" />
                        </Button>
                      </div>
                    </div>
                  )
                })}
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
              {itemsDiscount > 0 && (
                <div className="flex justify-between text-lg">
                  <span className="text-muted-foreground">{t("lineDiscountTotal")}</span>
                  <span className="text-destructive">-{itemsDiscount.toFixed(2)}</span>
                </div>
              )}
              {eligibleSubtotal > 0 && (
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

              <Button className="h-16 text-lg" size="lg" onClick={() => handleCheckout(true)} disabled={isCheckingOut || !canCheckout}>
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

    <Dialog open={unitPicker !== null} onOpenChange={(o) => { if (!o) setUnitPicker(null) }}>
      <DialogContent className="sm:max-w-[440px]">
        <DialogHeader>
          <DialogTitle>{t("chooseUnit")}</DialogTitle>
          <DialogDescription>{t("chooseUnitDescription")}</DialogDescription>
        </DialogHeader>
        <div className="py-2 space-y-2">
          {unitPicker?.product.units?.map((unit) => (
            <Button
              key={unit.id}
              variant="outline"
              className="w-full h-auto py-3 justify-between"
              onClick={() => unitPicker.onPick(unit)}
            >
              <div className="flex items-center gap-3">
                <Boxes className="h-5 w-5 text-muted-foreground" />
                <div className="text-left">
                  <div className="font-semibold">
                    {unit.unitName}
                    {unit.isBaseUnit && <span className="mr-2 text-[10px] text-muted-foreground">({t("baseUnit")})</span>}
                  </div>
                  <div className="text-xs text-muted-foreground">
                    {unit.quantityFactor === 1
                      ? `1 ${unit.unitName}`
                      : `1 ${unit.unitName} = ${unit.quantityFactor}`}
                  </div>
                </div>
              </div>
              <div className="text-right">
                <div className="font-semibold">{unit.retailPrice.toFixed(2)}</div>
                {unit.wholesalePrice != null && (
                  <div className="text-xs text-muted-foreground">{t("wholesale")}: {unit.wholesalePrice.toFixed(2)}</div>
                )}
              </div>
            </Button>
          ))}
        </div>
      </DialogContent>
    </Dialog>

    <Dialog open={lineEditFor !== null} onOpenChange={(o) => { if (!o) setLineEditFor(null) }}>
      <DialogContent className="sm:max-w-[420px]">
        <DialogHeader>
          <DialogTitle>{t("editLine")}</DialogTitle>
          <DialogDescription>
            {lineEditFor?.item.name} ({lineEditFor?.item.unitName})
          </DialogDescription>
        </DialogHeader>
        <div className="py-2 space-y-4">
          <div className="space-y-2">
            <label className="text-sm font-medium">{t("unitPrice")}</label>
            <Input
              type="number"
              min="0"
              step="0.01"
              className="text-right text-lg h-12"
              value={lineEditFor?.unitPrice ?? ''}
              onChange={(e) => setLineEditFor((s) => s ? { ...s, unitPrice: e.target.value } : s)}
              autoFocus
            />
            {lineEditFor && parseFloat(lineEditFor.unitPrice) !== lineEditFor.item.originalUnitPrice && (
              <div className="text-xs text-muted-foreground">
                {t("originalPrice")}: {lineEditFor.item.originalUnitPrice.toFixed(2)}
              </div>
            )}
          </div>
          {lineEditFor && lineEditFor.item.allowDiscount && (
            <div className="space-y-2">
              <label className="text-sm font-medium">{t("lineDiscount")}</label>
              <div className="flex gap-2">
                <Button
                  variant={lineEditFor.discountType === 'percentage' ? 'default' : 'outline'}
                  className="flex-1"
                  onClick={() => setLineEditFor((s) => s ? { ...s, discountType: 'percentage' } : s)}
                >
                  {t("discountTypePercentage")}
                </Button>
                <Button
                  variant={lineEditFor.discountType === 'fixed' ? 'default' : 'outline'}
                  className="flex-1"
                  onClick={() => setLineEditFor((s) => s ? { ...s, discountType: 'fixed' } : s)}
                >
                  {t("discountTypeFixed")}
                </Button>
              </div>
              <Input
                type="number"
                min="0"
                max={lineEditFor?.discountType === 'percentage' ? 100 : undefined}
                step="0.01"
                className="text-right text-lg h-12"
                value={lineEditFor?.discountValue ?? ''}
                onChange={(e) => setLineEditFor((s) => s ? { ...s, discountValue: e.target.value } : s)}
              />
            </div>
          )}
          {lineEditFor && parseFloat(lineEditFor.unitPrice) !== lineEditFor.item.originalUnitPrice && (
            <div className="space-y-2">
              <label className="text-sm font-medium">{t("priceEditNote")}</label>
              <textarea
                placeholder={t("priceEditNotePlaceholder")}
                className="w-full min-w-0 rounded-lg border border-input bg-transparent px-2.5 py-1 text-base transition-colors outline-none placeholder:text-muted-foreground focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 disabled:pointer-events-none disabled:opacity-50 md:text-sm dark:bg-input/30 resize-none"
                rows={3}
                value={lineEditFor?.note ?? ''}
                onChange={(e) => setLineEditFor((s) => s ? { ...s, note: e.target.value } : s)}
              />
              <p className="text-xs text-muted-foreground">{t("priceEditNoteHint")}</p>
            </div>
          )}
        </div>
        <DialogFooter className="flex-col gap-2 sm:flex-row">
          <Button variant="outline" onClick={() => setLineEditFor(null)}>
            {t("cancel")}
          </Button>
          <Button onClick={saveLineEdit} disabled={!lineEditFor || isNaN(parseFloat(lineEditFor.unitPrice)) || parseFloat(lineEditFor.unitPrice) < 0}>
            {t("save")}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
    </>
  )
}
