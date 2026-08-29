"use client"

import { useEffect, useState } from "react"
import { useTranslations } from "next-intl"
import { toast } from "sonner"
import { Edit, Eye, EyeOff, Plus, Printer, Trash } from "lucide-react"

import { Product } from "@/types/domain/domain.types"
import { useProductsPage } from "@/hooks/use-products"
import { deleteProduct, updateProduct } from "@/actions/products.actions"
import { useDebouncedCallback } from "@/hooks/use-debounced-callback"
import { useAuth } from "@/components/common/auth-context"
import { PERMISSIONS, FEATURES } from "@/lib/constants"
import { ResponsiveSheet } from "@/components/common/responsive-sheet"
import { ProductForm, PRODUCT_FORM_ID } from "@/components/common/product-form"
import { TableColumn, TableBuilder } from "@/components/common/table-builder"
import { DataPagination } from "@/components/common/data-pagination"
import { TooltipIconButton } from "@/components/common/tooltip-icon-button"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { BarcodePrintDialog } from "./_components/barcode-print-dialog"
import { ProductUnitsManager } from "./_components/product-units-manager"

const PAGE_SIZE = 20

export function ProductsClient() {
  const t = useTranslations("Products")
  const tc = useTranslations("Common")
  const { hasAccess, hasPermission } = useAuth()
  const canCreate = hasPermission(PERMISSIONS.PRODUCTS_CREATE)
  const canUpdate = hasPermission(PERMISSIONS.PRODUCTS_UPDATE)
  const canDelete = hasPermission(PERMISSIONS.PRODUCTS_DELETE)
  const canManageUnits = hasAccess(PERMISSIONS.PRODUCTS_UPDATE, FEATURES.MULTIPLE_UNITS)
  const canManageBarcodes = hasAccess(PERMISSIONS.PRODUCTS_UPDATE, FEATURES.MULTIPLE_BARCODES)
  const canPrintBarcode = hasAccess(PERMISSIONS.PRINTING_BARCODE, FEATURES.BARCODE_PRINTING)

  const [page, setPage] = useState(1)
  const [searchInput, setSearchInput] = useState("")
  const [query, setQuery] = useState("")

  const debouncedQueryChange = useDebouncedCallback((value: string) => {
    setQuery(value)
    setPage(1)
  }, 300)

  useEffect(() => {
    const value = searchInput.trim()
    if (value === query) return
    debouncedQueryChange(value)
  }, [debouncedQueryChange, query, searchInput])

  const { data, isPending } = useProductsPage(page, PAGE_SIZE, { q: query })
  const items = data?.items ?? []
  const total = data?.total ?? 0

  const [isSheetOpen, setIsSheetOpen] = useState(false)
  const [editingProduct, setEditingProduct] = useState<Product | null>(null)
  const [barcodePrintProduct, setBarcodePrintProduct] = useState<Product | null>(null)

  // Keep the sheet in sync with the latest row data after refreshes.
  const displayProduct = editingProduct
    ? (items.find((p) => p.id === editingProduct.id) ?? editingProduct)
    : null

  const handleEdit = (product: Product) => {
    setEditingProduct(product)
    setIsSheetOpen(true)
  }

  const handleDelete = async (id: string) => {
    if (confirm(t("deleteConfirm"))) {
      try {
        await deleteProduct(id)
        toast.success(t("productDeleted"))
      } catch {
        toast.error(t("deleteError"))
      }
    }
  }

  const handleToggleHidden = async (product: Product) => {
    try {
      await updateProduct(product.id, {
        name: product.name,
        retailPrice: product.salePrice,
        allowDiscount: product.allowDiscount,
        lowStockThreshold: product.lowStockThreshold,
        isHiddenFromPOS: !product.isHiddenFromPOS,
      })
      toast.success(
        product.isHiddenFromPOS ? t("shownOnPOS") : t("hiddenFromPOS")
      )
    } catch {
      toast.error(t("actionError"))
    }
  }

  const columns: TableColumn<Product>[] = [
    {
      key: "name",
      header: t("name"),
      cell: (p) => p.name,
    },
    {
      key: "barcode",
      header: t("barcode"),
      cell: (p) => p.barcode,
    },
    {
      key: "stockQuantity",
      header: t("stock"),
      cell: (p) => p.stockQuantity,
    },
    {
      key: "salePrice",
      header: t("salePrice"),
      cell: (p) => p.salePrice.toFixed(2),
    },
    {
      key: "actions",
      header: t("actions"),
      headClassName: "w-36",
      cell: (p) => (
        <div className="flex gap-2">
          {canPrintBarcode && (
            <TooltipIconButton
              label={t("printBarcode")}
              variant="ghost"
              size="icon"
              onClick={() => setBarcodePrintProduct(p)}
            >
              <Printer className="h-4 w-4" />
            </TooltipIconButton>
          )}
          {canUpdate && (
            <TooltipIconButton
              label={!p.isHiddenFromPOS ? t("shownOnPOS") : t("hiddenFromPOS")}
              variant="ghost"
              size="icon"
              onClick={() => handleToggleHidden(p)}
            >
              {p.isHiddenFromPOS ?<EyeOff className="h-4 w-4" />:<Eye className="size-4"/>}
            </TooltipIconButton>
          )}
          {canUpdate && (
            <TooltipIconButton
              label={t("editProduct")}
              variant="ghost"
              size="icon"
              onClick={() => handleEdit(p)}
            >
              <Edit className="h-4 w-4" />
            </TooltipIconButton>
          )}
          {canDelete && (
            <TooltipIconButton
              label={t("deleteConfirm")}
              variant="ghost"
              size="icon"
              onClick={() => handleDelete(p.id)}
            >
              <Trash className="h-4 w-4 text-destructive" />
            </TooltipIconButton>
          )}
        </div>
      ),
    },
  ]

  return (
    <>
      <div className="flex items-center justify-between gap-4 mb-4">
        <Input
          placeholder={tc("search")}
          value={searchInput}
          onChange={(e) => setSearchInput(e.target.value)}
          className="max-w-sm"
        />
        {canCreate && (
          <Button onClick={() => {
            setEditingProduct(null)
            setIsSheetOpen(true)
          }}>
            <Plus className="mr-2 h-4 w-4" /> {t("addProduct")}
          </Button>
        )}
      </div>

      <TableBuilder
        columns={columns}
        data={items}
        rowKey={(p) => p.id}
        loading={isPending}
        emptyMessage={tc("noResults")}
      />

      <DataPagination page={page} pageSize={PAGE_SIZE} total={total} onPageChange={setPage} />

      <ResponsiveSheet
        open={isSheetOpen}
        onOpenChange={setIsSheetOpen}
        title={editingProduct ? t("editProduct") : t("newProduct")}
        description=""
        footer={
          <div className="flex gap-3 justify-end">
            <Button variant="outline" onClick={() => setIsSheetOpen(false)}>
              {t("cancel")}
            </Button>
            <Button type="submit" form={PRODUCT_FORM_ID}>
              {editingProduct ? t("update") : t("create")}
            </Button>
          </div>
        }
      >
        <ProductForm
          initialData={displayProduct}
          onSuccess={() => {
            setIsSheetOpen(false)
          }}
        />
        {displayProduct && (canManageUnits || canManageBarcodes) && (
          <ProductUnitsManager
            product={displayProduct}
            canManageUnits={canManageUnits}
            canManageBarcodes={canManageBarcodes}
          />
        )}
      </ResponsiveSheet>

      <BarcodePrintDialog product={barcodePrintProduct} onOpenChange={(open) => {
        if (!open) setBarcodePrintProduct(null)
      }} />
    </>
  )
}
