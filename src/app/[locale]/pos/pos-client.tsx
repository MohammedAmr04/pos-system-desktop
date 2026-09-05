"use client"

import { useCallback, useEffect, useRef, useState } from "react"
import { useQueryClient } from "@tanstack/react-query"
import { Product, ProductUnit, Shift } from "@/types/domain/domain.types"
import { usePOSStore } from "@/store/pos.store"
import type { CartItem } from "@/store/pos.store"
import { useAuth } from "@/components/common/auth-context"
import { PERMISSIONS, FEATURES } from "@/lib/constants"
import { AccessDenied } from "@/components/common/access-denied"
import { Button } from "@/components/ui/button"
import { TooltipIconButton } from "@/components/common/tooltip-icon-button"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Link } from "@/i18n/navigation"
import { useTranslations } from "next-intl"
import { useApiError } from "@/lib/api-error"
import { toast } from "sonner"
import { ArrowRight } from "lucide-react"
import { addProductBarcode } from "@/actions/products.actions"
import { baseUnitOf, resolveBarcode } from "@/lib/barcode"
import { listProductsForPOS } from "@/api/products"
import { getInvoice } from "@/api/invoices"
import { productsKeys, useProductsForPOS } from "@/hooks/use-products"
import { invoicesKeys } from "@/hooks/use-invoices"
import { useActiveShift } from "@/hooks/use-shifts"
import { ProductSearchHandle, ProductSearchPopover } from "./_components/product-search-popover"
import { CartPanel } from "./_components/cart-panel"
import { CheckoutPanel } from "./_components/checkout-panel"
import { UnitPickerDialog, UnitPickerState } from "./_components/unit-picker-dialog"
import { UnknownBarcodeDialog } from "./_components/unknown-barcode-dialog"

export function POSClient() {
  const t = useTranslations("POS")
  const resolveError = useApiError()
  const { hasAccess, hasPermission, hasFeature } = useAuth()

  const canUsePOS = hasAccess(PERMISSIONS.INVOICES_CREATE) && hasPermission(PERMISSIONS.PRODUCTS_VIEW)
  const canWholesale = hasFeature(FEATURES.WHOLESALE_PRICE)
  const canCreateProduct = hasPermission(PERMISSIONS.PRODUCTS_CREATE)
  const canLinkBarcode = hasAccess(PERMISSIONS.PRODUCTS_UPDATE, FEATURES.MULTIPLE_BARCODES)
  const canPickUnit = hasFeature(FEATURES.MULTIPLE_UNITS)

  const addItem = usePOSStore((s) => s.addItem)
  const loadDraft = usePOSStore((s) => s.loadDraft)
  const priceMode = usePOSStore((s) => s.priceMode)
  const setPriceMode = usePOSStore((s) => s.setPriceMode)
  const queryClient = useQueryClient()

  const searchRef = useRef<ProductSearchHandle>(null)
  const [unitPicker, setUnitPicker] = useState<UnitPickerState | null>(null)
  const [unknownBarcode, setUnknownBarcode] = useState<string | null>(null)
  const [priceModeTarget, setPriceModeTarget] = useState<'retail' | 'wholesale' | null>(null)
  const cartItems = usePOSStore((s) => s.cartItems)

  const switchPriceMode = useCallback((mode: 'retail' | 'wholesale') => {
    const hasOverrides = cartItems.some((item) => item.overridden || item.priceEditNote)
    if (hasOverrides && mode !== priceMode) {
      setPriceModeTarget(mode)
    } else {
      setPriceMode(mode)
    }
  }, [cartItems, priceMode, setPriceMode])

  const { data: activeShift } = useActiveShift()
  const { data: productsData = [] } = useProductsForPOS()
  const products: Product[] = productsData

  useEffect(() => {
    if (!canWholesale && priceMode === 'wholesale') {
      setPriceMode('retail')
    }
  }, [canWholesale, priceMode, setPriceMode])

  const refresh = useCallback(async (): Promise<Product[]> => {
    return queryClient.fetchQuery({ queryKey: [...productsKeys.all, "pos"], queryFn: listProductsForPOS })
  }, [queryClient])

  useEffect(() => {
    const params = new URLSearchParams(window.location.search)
    const draftParam = params.get('draft')
    if (!draftParam) return
    window.history.replaceState({}, '', window.location.pathname)
    let cancelled = false
    queryClient
      .fetchQuery({ queryKey: invoicesKeys.detail(draftParam), queryFn: () => getInvoice(draftParam) })
      .then((inv) => {
        if (cancelled || (inv.status ?? 'posted') !== 'draft') return
        const items: CartItem[] = (inv.invoiceDetail ?? inv.InvoiceDetail ?? []).map((d) => {
          const unit = d.product?.units?.find((u) => u.id === d.productUnitId)
          return {
            id: d.productUnitId || d.productId || d.id,
            productId: d.productId ?? '',
            productUnitId: d.productUnitId ?? '',
            unitName: d.unitName ?? unit?.unitName ?? '',
            name: d.product?.name ?? '',
            buyPrice: d.buyPrice,
            retailPrice: unit?.retailPrice ?? d.salePrice,
            wholesalePrice: unit?.wholesalePrice ?? null,
            originalUnitPrice: d.originalUnitPrice ?? d.unitPrice ?? d.salePrice,
            unitPrice: d.unitPrice ?? d.salePrice,
            quantity: d.quantity,
            maxStock: d.product?.stockQuantity ?? 999999,
            quantityFactor: unit?.quantityFactor ?? 1,
            allowDiscount: true,
            discountType: (d.discountType as 'percentage' | 'fixed' | null) ?? null,
            discountValue: d.discountValue ?? 0,
            overridden: false,
            priceEditNote: d.priceEditNote ?? undefined,
          }
        })
        loadDraft({
          id: inv.id,
          clientId: inv.clientId ?? null,
          employeeId: inv.employeeId ?? null,
          paymentMethod: inv.paymentMethod ?? 'cash',
          discount: inv.discount,
          discountType: (inv.discountType === 'percentage' ? 'percentage' : 'fixed'),
          priceMode: inv.priceMode ?? 'retail',
          items,
        })
      })
      .catch(() => {})
    return () => {
      cancelled = true
    }
  }, [loadDraft, queryClient])

  const applyAdd = useCallback((product: Product, unit: ProductUnit) => {
    const result = addItem(product, unit)
    if (result === 'out') {
      toast.error(t("outOfStock"))
    } else if (result === 'max') {
      toast.error(t("maxStockReached"))
    }
  }, [addItem, t])

  const handleSelect = useCallback((product: Product, unit?: ProductUnit) => {
    if (unit) {
      applyAdd(product, unit)
      searchRef.current?.reset()
      return
    }
    const units = product.units?.length ? product.units : []
    if (units.length === 1) {
      applyAdd(product, units[0])
      searchRef.current?.reset()
      return
    }
    if (units.length > 1 && canPickUnit) {
      searchRef.current?.close()
      setUnitPicker({ product, onPick: (picked) => { setUnitPicker(null); applyAdd(product, picked) } })
      return
    }
    const base = baseUnitOf(product)
    if (base) {
      applyAdd(product, base)
      searchRef.current?.reset()
    }
  }, [applyAdd, canPickUnit])

  const closeUnknownDialog = useCallback(() => {
    setUnknownBarcode(null)
    searchRef.current?.reset()
  }, [])

  const doLink = useCallback(async (product: Product, unit: ProductUnit) => {
    if (!unknownBarcode) return
    try {
      await addProductBarcode(product.id, unit.id, unknownBarcode)
      toast.success(t("barcodeLinked"))
      addItem(product, unit)
      await queryClient.invalidateQueries({ queryKey: productsKeys.all })
    } catch (e) {
      toast.error(resolveError(e) || t("linkFailed"))
    } finally {
      closeUnknownDialog()
      setUnitPicker(null)
    }
  }, [unknownBarcode, queryClient, addItem, closeUnknownDialog, t, resolveError])

  const handleLinkConfirm = (product: Product) => {
    const units = product.units?.length ? product.units : []
    if (units.length === 1) {
      doLink(product, units[0])
      return
    }
    if (units.length > 1 && canPickUnit) {
      closeUnknownDialog()
      setUnitPicker({ product, onPick: (unit) => doLink(product, unit) })
      return
    }
    const base = baseUnitOf(product)
    if (base) doLink(product, base)
  }

  const handleCreatedProduct = useCallback(async () => {
    if (!unknownBarcode) return
    try {
      const fresh = await refresh()
      const resolved = resolveBarcode(fresh, unknownBarcode)
      if (resolved) addItem(resolved.product, resolved.unit)
    } finally {
      toast.success(t("productCreated"))
      closeUnknownDialog()
    }
  }, [unknownBarcode, refresh, addItem, closeUnknownDialog, t])

  const handleScan = useCallback((barcode: string) => {
    const resolved = resolveBarcode(products, barcode)
    if (resolved) {
      handleSelect(resolved.product, resolved.unit)
    } else {
      setUnknownBarcode(barcode)
    }
  }, [products, handleSelect])

  const scanBufferRef = useRef("")
  const scanTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null)

  useEffect(() => {
    const isEditable = (el: Element | null): boolean => {
      if (!el) return false
      const tag = el.tagName
      return tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT' || (el as HTMLElement).isContentEditable
    }

    const onKeyDown = (e: KeyboardEvent) => {
      if (document.querySelector('[data-slot="dialog-content"], [role="dialog"]')) return
      if (isEditable(document.activeElement)) return

      if (e.key === 'Enter') {
        const buffer = scanBufferRef.current.trim()
        if (buffer) {
          e.preventDefault()
          scanBufferRef.current = ""
          handleScan(buffer)
        }
        return
      }

      if (e.key.length === 1 && !e.ctrlKey && !e.metaKey && !e.altKey) {
        scanBufferRef.current += e.key
        if (scanTimerRef.current) clearTimeout(scanTimerRef.current)
        scanTimerRef.current = setTimeout(() => { scanBufferRef.current = "" }, 800)
      }
    }

    window.addEventListener('keydown', onKeyDown)
    return () => {
      window.removeEventListener('keydown', onKeyDown)
      if (scanTimerRef.current) clearTimeout(scanTimerRef.current)
    }
  }, [handleScan])

  if (!canUsePOS) {
    return <AccessDenied />
  }

  return (
    <>
      <div className="flex h-full flex-col lg:flex-row gap-4 p-4 lg:p-6 bg-muted/40">
        <div className="flex flex-1 flex-col gap-4">
          <div className="flex items-center gap-3">
            <Link href="/">
              <TooltipIconButton label={t("back")} variant="outline">
                <ArrowRight className="h-4 w-4" />
              </TooltipIconButton>
            </Link>
            {activeShift ? (
              <div className="flex shrink-0 items-center gap-2 rounded-lg border border-emerald-500/30 bg-emerald-500/10 px-3 py-1.5 text-sm font-medium text-emerald-700">
                <span className="h-2 w-2 animate-pulse rounded-full bg-emerald-500" />
                {t("shiftActive", { number: activeShift.number })}
              </div>
            ) : (
              <Link href="/shifts/">
                <div className="flex shrink-0 items-center gap-2 rounded-lg border border-amber-500/40 bg-amber-500/10 px-3 py-1.5 text-sm font-medium text-amber-700">
                  <span className="h-2 w-2 rounded-full bg-amber-500" />
                  {t("noOpenShift")}
                </div>
              </Link>
            )}
            {canWholesale && (
              <div className="flex shrink-0 rounded-lg border bg-background p-1">
                <Button
                  variant={priceMode === 'retail' ? 'default' : 'ghost'}
                  size="sm"
                  className="h-8 px-3"
                  onClick={() => switchPriceMode('retail')}
                >
                  {t("retail")}
                </Button>
                <Button
                  variant={priceMode === 'wholesale' ? 'default' : 'ghost'}
                  size="sm"
                  className="h-8 px-3"
                  onClick={() => switchPriceMode('wholesale')}
                >
                  {t("wholesale")}
                </Button>
              </div>
            )}
            <ProductSearchPopover
              ref={searchRef}
              products={products}
              onSelect={handleSelect}
              onUnknownBarcode={setUnknownBarcode}
            />
          </div>

          <CartPanel />
        </div>

        <div className="w-full lg:w-96 flex flex-col gap-4">
          <CheckoutPanel />
        </div>
      </div>

      {unknownBarcode !== null && (
        <UnknownBarcodeDialog
          key={unknownBarcode}
          barcode={unknownBarcode}
          products={products}
          canCreateProduct={canCreateProduct}
          canLinkBarcode={canLinkBarcode}
          onClose={closeUnknownDialog}
          onConfirmLink={handleLinkConfirm}
          onCreateSuccess={handleCreatedProduct}
        />
      )}

      <UnitPickerDialog picker={unitPicker} onClose={() => setUnitPicker(null)} />

      <Dialog open={priceModeTarget !== null} onOpenChange={(open) => { if (!open) setPriceModeTarget(null) }}>
        <DialogContent showCloseButton={false}>
          <DialogHeader>
            <DialogTitle>{t("confirmPriceModeTitle")}</DialogTitle>
            <DialogDescription>{t("confirmPriceModeDescription")}</DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setPriceModeTarget(null)}>{t("cancel")}</Button>
            <Button
              onClick={() => {
                if (priceModeTarget) setPriceMode(priceModeTarget)
                setPriceModeTarget(null)
              }}
            >
              {t("confirmSwitch")}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  )
}
