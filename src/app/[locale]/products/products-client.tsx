"use client"

import { Product, ProductBarcode, ProductUnit } from "@/lib/api"
import { ResponsiveSheet } from "@/components/common/responsive-sheet"
import { ProductForm, PRODUCT_FORM_ID } from "./product-form"
import { useEffect, useState } from "react"
import { Button } from "@/components/ui/button"
import { Plus, Edit, Trash, Printer, Boxes, Loader2 } from "lucide-react"
import {
  ColumnDef,
  SortingState,
  flexRender,
  getCoreRowModel,
  getSortedRowModel,
  useReactTable,
} from "@tanstack/react-table"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import {
  deleteProduct,
  addProductUnit,
  updateProductUnit,
  deleteProductUnit,
  addProductBarcode,
  removeProductBarcode,
  setDefaultProductBarcode,
} from "@/features/products/actions"
import { toast } from "sonner"
import { useTranslations } from "next-intl"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Input } from "@/components/ui/input"

interface ProductsClientProps {
  items: Product[]
  total: number
  page: number
  pageSize: number
  loading: boolean
  query: string
  onQueryChange: (query: string) => void
  onPageChange: (page: number) => void
  onRefresh?: () => void
}

interface UnitFormState {
  unit?: ProductUnit
  unitName: string
  quantityFactor: string
  retailPrice: string
  wholesalePrice: string
}

export function ProductsClient({
  items,
  total,
  page,
  pageSize,
  loading,
  query,
  onQueryChange,
  onPageChange,
  onRefresh,
}: ProductsClientProps) {
  const t = useTranslations("Products")
  const tc = useTranslations("Common")
  const [isSheetOpen, setIsSheetOpen] = useState(false)
  const [editingProduct, setEditingProduct] = useState<Product | null>(null)
  const [barcodePrintProduct, setBarcodePrintProduct] = useState<Product | null>(null)
  const [barcodeCount, setBarcodeCount] = useState(1)
  const [isBarcodeDialogOpen, setIsBarcodeDialogOpen] = useState(false)
  const [isAddBarcodeOpen, setIsAddBarcodeOpen] = useState(false)
  const [newBarcode, setNewBarcode] = useState("")
  const [addBarcodeForUnit, setAddBarcodeForUnit] = useState<ProductUnit | null>(null)
  const [isUnitFormOpen, setIsUnitFormOpen] = useState(false)
  const [unitForm, setUnitForm] = useState<UnitFormState>({
    unitName: "",
    quantityFactor: "1",
    retailPrice: "",
    wholesalePrice: "",
  })
  const [sorting, setSorting] = useState<SortingState>([])
  const [searchInput, setSearchInput] = useState(query)

  const displayProduct = editingProduct
    ? (items.find((p) => p.id === editingProduct.id) ?? editingProduct)
    : null

  useEffect(() => {
    const value = searchInput.trim()
    if (value === query) return
    const timer = setTimeout(() => onQueryChange(value), 300)
    return () => clearTimeout(timer)
  }, [searchInput, query, onQueryChange])

  const pageCount = Math.max(1, Math.ceil(total / pageSize))

  const columns: ColumnDef<Product>[] = [
    {
      accessorKey: "name",
      header: t("name"),
    },
    {
      accessorKey: "barcode",
      header: t("barcode"),
    },
    {
      accessorKey: "stockQuantity",
      header: t("stock"),
    },
    {
      accessorKey: "salePrice",
      header: t("salePrice"),
      cell: ({ row }) => `${row.original.salePrice.toFixed(2)}`
    },
    {
      id: "actions",
      header: t("actions"),
      cell: ({ row }) => (
        <div className="flex gap-2">
          <Button variant="ghost" size="icon" onClick={() => handlePrintBarcode(row.original)} title={t("printBarcode")}>
            <Printer className="h-4 w-4" />
          </Button>
          <Button variant="ghost" size="icon" onClick={() => handleEdit(row.original)}>
            <Edit className="h-4 w-4" />
          </Button>
          <Button variant="ghost" size="icon" onClick={() => handleDelete(row.original.id)}>
            <Trash className="h-4 w-4 text-destructive" />
          </Button>
        </div>
      )
    }
  ]

  const table = useReactTable({
    data: items,
    columns,
    getCoreRowModel: getCoreRowModel(),
    getSortedRowModel: getSortedRowModel(),
    onSortingChange: setSorting,
    manualPagination: true,
    pageCount,
    state: {
      sorting,
      pagination: { pageIndex: page - 1, pageSize },
    },
    onPaginationChange: (updater) => {
      const next =
        typeof updater === "function"
          ? updater({ pageIndex: page - 1, pageSize })
          : updater
      onPageChange(next.pageIndex + 1)
    },
  })

  const handleEdit = (product: Product) => {    setEditingProduct(product)
    setIsSheetOpen(true)
  }

  const openAddUnit = () => {
    setUnitForm({ unitName: "", quantityFactor: "1", retailPrice: "", wholesalePrice: "" })
    setIsUnitFormOpen(true)
  }

  const openEditUnit = (unit: ProductUnit) => {
    setUnitForm({
      unit,
      unitName: unit.unitName,
      quantityFactor: String(unit.quantityFactor),
      retailPrice: String(unit.retailPrice),
      wholesalePrice: unit.wholesalePrice != null ? String(unit.wholesalePrice) : "",
    })
    setIsUnitFormOpen(true)
  }

  const handleSaveUnit = async () => {
    if (!displayProduct) return
    const unitName = unitForm.unitName.trim()
    const quantityFactor = parseFloat(unitForm.quantityFactor)
    const retailPrice = parseFloat(unitForm.retailPrice)
    const wholesalePrice = unitForm.wholesalePrice.trim()
      ? parseFloat(unitForm.wholesalePrice)
      : null
    if (!unitName || isNaN(quantityFactor) || quantityFactor <= 0 || isNaN(retailPrice) || retailPrice <= 0) return
    try {
      if (unitForm.unit) {
        await updateProductUnit(displayProduct.id, unitForm.unit.id, {
          unitName,
          quantityFactor,
          retailPrice,
          wholesalePrice,
        })
        toast.success(t("unitUpdated"))
      } else {
        await addProductUnit(displayProduct.id, {
          unitName,
          quantityFactor,
          retailPrice,
          wholesalePrice,
        })
        toast.success(t("unitAdded"))
      }
      setIsUnitFormOpen(false)
      if (onRefresh) onRefresh()
    } catch (e) {
      toast.error((e as Error).message || t("unitSaveFailed"))
    }
  }

  const handleDeleteUnit = async (unit: ProductUnit) => {
    if (!displayProduct || unit.isBaseUnit) return
    if (!confirm(t("unitDeleteConfirm"))) return
    try {
      await deleteProductUnit(displayProduct.id, unit.id)
      toast.success(t("unitDeleted"))
      if (onRefresh) onRefresh()
    } catch (e) {
      toast.error((e as Error).message || t("unitDeleteFailed"))
    }
  }

  const openAddBarcode = (unit: ProductUnit) => {
    setAddBarcodeForUnit(unit)
    setNewBarcode("")
    setIsAddBarcodeOpen(true)
  }

  const handleAddBarcodeConfirm = async () => {
    if (!displayProduct || !addBarcodeForUnit || !newBarcode.trim()) return
    try {
      await addProductBarcode(displayProduct.id, addBarcodeForUnit.id, newBarcode.trim())
      toast.success(t("barcodeAdded"))
      setNewBarcode("")
      setIsAddBarcodeOpen(false)
      if (onRefresh) onRefresh()
    } catch (e) {
      toast.error((e as Error).message || t("barcodeAddFailed"))
    }
  }

  const handleDeleteBarcode = async (unit: ProductUnit, barcode: ProductBarcode) => {
    if (!displayProduct) return
    if (!confirm(t("deleteBarcodeConfirm"))) return
    try {
      await removeProductBarcode(displayProduct.id, unit.id, barcode.id)
      toast.success(t("barcodeDeleted"))
      if (onRefresh) onRefresh()
    } catch (e) {
      toast.error((e as Error).message || t("barcodeDeleteFailed"))
    }
  }

  const handleSetDefault = async (unit: ProductUnit, barcodeId: string) => {
    if (!displayProduct) return
    try {
      await setDefaultProductBarcode(displayProduct.id, unit.id, barcodeId)
      toast.success(t("defaultBarcodeUpdated"))
      if (onRefresh) onRefresh()
    } catch (e) {
      toast.error((e as Error).message || t("defaultBarcodeUpdateFailed"))
    }
  }

  const handleDelete = async (id: string) => {
    if (confirm(t("deleteConfirm"))) {
      try {
        await deleteProduct(id)
        toast.success(t("productDeleted"))
        if (onRefresh) onRefresh()
      } catch {
        toast.error(t("deleteError"))
      }
    }
  }

  const handlePrintBarcode = (product: Product) => {
    setBarcodePrintProduct(product)
    setBarcodeCount(1)
    setIsBarcodeDialogOpen(true)
  }

  const handleBarcodePrintConfirm = async () => {
    if (!barcodePrintProduct) return
    try {
      await fetch("http://localhost:3001/api/printing/print-barcode", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          barcode: barcodePrintProduct.barcode,
          name: barcodePrintProduct.name,
          price: barcodePrintProduct.salePrice,
          count: barcodeCount,
        }),
      })
      setIsBarcodeDialogOpen(false)
      toast.success(t("barcodePrinting"))
    } catch {
      toast.error(t("barcodePrintFailed"))
    }
  }

  return (
    <>
      <div className="flex items-center justify-between gap-4 mb-4">
        <Input
          placeholder={tc("search")}
          value={searchInput}
          onChange={(e) => setSearchInput(e.target.value)}
          className="max-w-sm"
        />
        <Button onClick={() => {
          setEditingProduct(null)
          setIsSheetOpen(true)
        }}>
          <Plus className="mr-2 h-4 w-4" /> {t("addProduct")}
        </Button>
      </div>

      <div className="rounded-md border bg-card">
        <Table>
          <TableHeader>
            {table.getHeaderGroups().map((headerGroup) => (
              <TableRow key={headerGroup.id}>
                {headerGroup.headers.map((header) => (
                  <TableHead key={header.id}>
                    {header.isPlaceholder
                      ? null
                      : flexRender(
                          header.column.columnDef.header,
                          header.getContext()
                        )}
                  </TableHead>
                ))}
              </TableRow>
            ))}
          </TableHeader>
          <TableBody>
            {loading ? (
              <TableRow>
                <TableCell colSpan={columns.length} className="h-24 text-center">
                  <Loader2 className="mx-auto h-6 w-6 animate-spin" />
                </TableCell>
              </TableRow>
            ) : table.getRowModel().rows?.length ? (
              table.getRowModel().rows.map((row) => (
                <TableRow key={row.id}>
                  {row.getVisibleCells().map((cell) => (
                    <TableCell key={cell.id}>
                      {flexRender(cell.column.columnDef.cell, cell.getContext())}
                    </TableCell>
                  ))}
                </TableRow>
              ))
            ) : (
              <TableRow>
                <TableCell colSpan={columns.length} className="h-24 text-center">
                  {tc("noResults")}
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </div>

      <div className="flex items-center justify-between py-4">
        <span className="text-sm text-muted-foreground">
          {t("productsCount", { count: total })}
        </span>
        <div className="flex items-center gap-2">
          <Button
            variant="outline"
            size="sm"
            onClick={() => onPageChange(page - 1)}
            disabled={page <= 1}
          >
            {tc("previous")}
          </Button>
          <span className="text-sm text-muted-foreground whitespace-nowrap">
            {t("pageInfo", { page, pageCount })}
          </span>
          <Button
            variant="outline"
            size="sm"
            onClick={() => onPageChange(page + 1)}
            disabled={page >= pageCount}
          >
            {tc("next")}
          </Button>
        </div>
      </div>

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
            if (onRefresh) onRefresh()
          }} 
        />
        {displayProduct && (
          <div className="border-t pt-4 space-y-4">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2">
                <Boxes className="h-4 w-4 text-muted-foreground" />
                <h4 className="text-sm font-semibold">{t("units")}</h4>
              </div>
              <Button variant="outline" size="sm" onClick={openAddUnit}>
                <Plus className="mr-2 h-4 w-4" /> {t("addUnit")}
              </Button>
            </div>
            {displayProduct.units && displayProduct.units.length > 0 ? (
              <div className="space-y-3">
                {displayProduct.units.map((unit) => (
                  <div key={unit.id} className="rounded-lg border p-3">
                    <div className="flex items-center justify-between gap-2">
                      <div className="min-w-0">
                        <div className="flex items-center gap-2">
                          <span className="font-semibold">{unit.unitName}</span>
                          {unit.isBaseUnit && (
                            <span className="shrink-0 inline-flex items-center rounded bg-primary/10 px-1.5 py-0.5 text-[10px] font-medium text-primary">
                              {t("baseUnit")}
                            </span>
                          )}
                          {unit.quantityFactor !== 1 && (
                            <span className="text-xs text-muted-foreground">
                              {t("factorLabel")}: {unit.quantityFactor}
                            </span>
                          )}
                        </div>
                        <div className="text-xs text-muted-foreground mt-0.5">
                          {t("retailPrice")}: {unit.retailPrice.toFixed(2)}
                          {unit.wholesalePrice != null && (
                            <> | {t("wholesalePrice")}: {unit.wholesalePrice.toFixed(2)}</>
                          )}
                        </div>
                      </div>
                      {!unit.isBaseUnit && (
                        <div className="flex items-center gap-1 shrink-0">
                          <Button variant="ghost" size="sm" onClick={() => openEditUnit(unit)}>
                            <Edit className="h-4 w-4" />
                          </Button>
                          <Button variant="ghost" size="icon" onClick={() => handleDeleteUnit(unit)}>
                            <Trash className="h-4 w-4 text-destructive" />
                          </Button>
                        </div>
                      )}
                    </div>
                    <div className="mt-2 space-y-1">
                      {unit.barcodes && unit.barcodes.length > 0 ? (
                        unit.barcodes.map((b) => (
                          <div key={b.id} className="flex items-center justify-between gap-2 rounded border bg-muted/40 px-2 py-1">
                            <div className="flex items-center gap-2 min-w-0">
                              <span className="font-mono text-xs truncate">{b.barcode}</span>
                              {b.isDefault && (
                                <span className="shrink-0 inline-flex items-center rounded bg-primary/10 px-1.5 py-0.5 text-[10px] font-medium text-primary">
                                  {t("primary")}
                                </span>
                              )}
                            </div>
                            {!b.isDefault && (
                              <div className="flex items-center gap-1 shrink-0">
                                <Button variant="ghost" size="sm" onClick={() => handleSetDefault(unit, b.id)}>
                                  {t("setAsDefault")}
                                </Button>
                                <Button variant="ghost" size="icon" onClick={() => handleDeleteBarcode(unit, b)}>
                                  <Trash className="h-4 w-4 text-destructive" />
                                </Button>
                              </div>
                            )}
                          </div>
                        ))
                      ) : (
                        <p className="text-xs text-muted-foreground">{t("noBarcodes")}</p>
                      )}
                      <Button variant="ghost" size="sm" onClick={() => openAddBarcode(unit)}>
                        <Plus className="mr-2 h-4 w-4" /> {t("addBarcode")}
                      </Button>
                    </div>
                  </div>
                ))}
              </div>
            ) : (
              <p className="text-sm text-muted-foreground">{t("noUnits")}</p>
            )}
          </div>
        )}
      </ResponsiveSheet>

      <Dialog open={isBarcodeDialogOpen} onOpenChange={setIsBarcodeDialogOpen}>
        <DialogContent className="sm:max-w-[400px]">
          <DialogHeader>
            <DialogTitle>{t("printBarcode")}</DialogTitle>
            <DialogDescription>
              {t("barcodePrintDescription")}
            </DialogDescription>
          </DialogHeader>
          <div className="py-4">
            {barcodePrintProduct && (
              <div className="mb-4 text-sm">
                <p><strong>{t("name")}:</strong> {barcodePrintProduct.name}</p>
                <p><strong>{t("barcode")}:</strong> {barcodePrintProduct.barcode}</p>
                <p><strong>{t("salePrice")}:</strong> {barcodePrintProduct.salePrice.toFixed(2)}</p>
              </div>
            )}
            <div className="flex items-center gap-4">
              <label className="text-sm font-medium whitespace-nowrap">{t("barcodePrintCount")}:</label>
              <Input
                type="number"
                min="1"
                max="100"
                value={barcodeCount}
                onChange={(e) => setBarcodeCount(parseInt(e.target.value) || 1)}
              />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setIsBarcodeDialogOpen(false)}>
              {t("cancel")}
            </Button>
            <Button onClick={handleBarcodePrintConfirm}>
              {t("print")}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={isAddBarcodeOpen} onOpenChange={setIsAddBarcodeOpen}>
        <DialogContent className="sm:max-w-[400px]">
          <DialogHeader>
            <DialogTitle>{t("addBarcode")}</DialogTitle>
            <DialogDescription>
              {t("addBarcodeDescription")}
            </DialogDescription>
          </DialogHeader>
          <div className="py-4">
            {displayProduct && (
              <p className="mb-3 text-sm text-muted-foreground">
                <strong>{t("name")}:</strong> {displayProduct.name}
                {addBarcodeForUnit && <> ({addBarcodeForUnit.unitName})</>}
              </p>
            )}
            <label className="text-sm font-medium">{t("barcode")} *</label>
            <Input
              className="mt-1 font-mono"
              value={newBarcode}
              onChange={(e) => setNewBarcode(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === "Enter") handleAddBarcodeConfirm()
              }}
              placeholder={t("barcode")}
              autoFocus
            />
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setIsAddBarcodeOpen(false)}>
              {t("cancel")}
            </Button>
            <Button onClick={handleAddBarcodeConfirm} disabled={!newBarcode.trim()}>
              {t("save")}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={isUnitFormOpen} onOpenChange={setIsUnitFormOpen}>
        <DialogContent className="sm:max-w-[440px]">
          <DialogHeader>
            <DialogTitle>{unitForm.unit ? t("editUnit") : t("addUnit")}</DialogTitle>
            <DialogDescription>
              {unitForm.unit?.isBaseUnit ? t("baseUnitLocked") : t("unitDescription")}
            </DialogDescription>
          </DialogHeader>
          <div className="py-4 space-y-4">
            <div className="space-y-2">
              <label className="text-sm font-medium">{t("unitName")} *</label>
              <Input
                value={unitForm.unitName}
                onChange={(e) => setUnitForm({ ...unitForm, unitName: e.target.value })}
                placeholder={t("unitNamePlaceholder")}
                autoFocus
              />
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <label className="text-sm font-medium">{t("quantityFactor")} *</label>
                <Input
                  type="number"
                  step="0.01"
                  min="0.01"
                  value={unitForm.quantityFactor}
                  disabled={!!unitForm.unit?.isBaseUnit}
                  onChange={(e) => setUnitForm({ ...unitForm, quantityFactor: e.target.value })}
                />
              </div>
              <div className="space-y-2">
                <label className="text-sm font-medium">{t("retailPrice")} *</label>
                <Input
                  type="number"
                  step="0.01"
                  min="0.01"
                  value={unitForm.retailPrice}
                  onChange={(e) => setUnitForm({ ...unitForm, retailPrice: e.target.value })}
                />
              </div>
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t("wholesalePrice")}</label>
              <Input
                type="number"
                step="0.01"
                min="0"
                value={unitForm.wholesalePrice}
                onChange={(e) => setUnitForm({ ...unitForm, wholesalePrice: e.target.value })}
                placeholder={t("optional")}
              />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setIsUnitFormOpen(false)}>
              {t("cancel")}
            </Button>
            <Button
              onClick={handleSaveUnit}
              disabled={!unitForm.unitName.trim() || isNaN(parseFloat(unitForm.quantityFactor)) || parseFloat(unitForm.quantityFactor) <= 0 || isNaN(parseFloat(unitForm.retailPrice)) || parseFloat(unitForm.retailPrice) <= 0}
            >
              {t("save")}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  )
}
