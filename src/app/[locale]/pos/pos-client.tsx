"use client"

import { useCallback, useEffect, useRef, useState } from "react"
import { api, Product, ProductUnit } from "@/lib/api"
import { usePOSStore } from "@/store/pos.store"
import { useAuth } from "@/components/common/auth-context"
import { PERMISSIONS, FEATURES } from "@/lib/constants"
import { AccessDenied } from "@/components/common/access-denied"
import { Button } from "@/components/ui/button"
import { Link } from "@/i18n/navigation"
import { useTranslations } from "next-intl"
import { toast } from "sonner"
import { ArrowLeft } from "lucide-react"
import { addProductBarcode } from "@/actions/products.actions"
import { baseUnitOf, resolveBarcode } from "@/lib/barcode"
import { ProductSearchHandle, ProductSearchPopover } from "./_components/product-search-popover"
import { CartPanel } from "./_components/cart-panel"
import { CheckoutPanel } from "./_components/checkout-panel"
import { UnitPickerDialog, UnitPickerState } from "./_components/unit-picker-dialog"
import { UnknownBarcodeDialog } from "./_components/unknown-barcode-dialog"

export function POSClient() {
  const t = useTranslations("POS")
  const { hasAccess, hasPermission, hasFeature } = useAuth()

  const canUsePOS = hasAccess(PERMISSIONS.INVOICES_CREATE) && hasPermission(PERMISSIONS.PRODUCTS_VIEW)
  const canWholesale = hasFeature(FEATURES.WHOLESALE_PRICE)
  const canCreateProduct = hasPermission(PERMISSIONS.PRODUCTS_CREATE)
  const canLinkBarcode = hasAccess(PERMISSIONS.PRODUCTS_UPDATE, FEATURES.MULTIPLE_BARCODES)
  const canPickUnit = hasFeature(FEATURES.MULTIPLE_UNITS)

  const addItem = usePOSStore((s) => s.addItem)
  const priceMode = usePOSStore((s) => s.priceMode)
  const setPriceMode = usePOSStore((s) => s.setPriceMode)

  const searchRef = useRef<ProductSearchHandle>(null)
  const [products, setProducts] = useState<Product[]>([])
  const [unitPicker, setUnitPicker] = useState<UnitPickerState | null>(null)
  const [unknownBarcode, setUnknownBarcode] = useState<string | null>(null)

  useEffect(() => {
    if (!canWholesale && priceMode === 'wholesale') {
      setPriceMode('retail')
    }
  }, [canWholesale, priceMode, setPriceMode])

  const refresh = useCallback(async () => {
    const list = await api.products.list()
    setProducts(list)
    return list
  }, [])

  useEffect(() => {
    let cancelled = false
    api.products
      .list()
      .then((list) => {
        if (!cancelled) setProducts(list)
      })
      .catch(() => {})
    return () => {
      cancelled = true
    }
  }, [])

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
      await refresh()
    } catch (e) {
      toast.error((e as Error).message || t("linkFailed"))
    } finally {
      closeUnknownDialog()
      setUnitPicker(null)
    }
  }, [unknownBarcode, refresh, addItem, closeUnknownDialog, t])

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

  if (!canUsePOS) {
    return <AccessDenied />
  }

  return (
    <>
      <div className="flex h-full flex-col lg:flex-row gap-4 p-4 lg:p-6 bg-muted/40">
        <div className="flex flex-1 flex-col gap-4">
          <div className="flex items-center gap-3">
            <Link href="/">
              <Button variant="outline" size="icon" aria-label={t("back")}>
                <ArrowLeft className="h-4 w-4" />
              </Button>
            </Link>
            {canWholesale && (
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
    </>
  )
}
