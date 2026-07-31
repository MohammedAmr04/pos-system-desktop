"use client"

import { Product, ProductBarcode } from "@/lib/api"
import { DataTable } from "@/components/common/data-table"
import { ResponsiveSheet } from "@/components/common/responsive-sheet"
import { ProductForm, PRODUCT_FORM_ID } from "./product-form"
import { useState } from "react"
import { Button } from "@/components/ui/button"
import { Plus, Edit, Trash, Printer, KeyRound } from "lucide-react"
import { ColumnDef } from "@tanstack/react-table"
import { deleteProduct, addProductBarcode, removeProductBarcode, setDefaultProductBarcode } from "@/features/products/actions"
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
  data: Product[]
  onRefresh?: () => void
}

export function ProductsClient({ data, onRefresh }: ProductsClientProps) {
  const t = useTranslations("Products")
  const [isSheetOpen, setIsSheetOpen] = useState(false)
  const [editingProduct, setEditingProduct] = useState<Product | null>(null)
  const [barcodePrintProduct, setBarcodePrintProduct] = useState<Product | null>(null)
  const [barcodeCount, setBarcodeCount] = useState(1)
  const [isBarcodeDialogOpen, setIsBarcodeDialogOpen] = useState(false)
  const [isAddBarcodeOpen, setIsAddBarcodeOpen] = useState(false)
  const [newBarcode, setNewBarcode] = useState("")

  const displayProduct = editingProduct
    ? (data.find((p) => p.id === editingProduct.id) ?? editingProduct)
    : null

  const handleEdit = (product: Product) => {
    setEditingProduct(product)
    setIsSheetOpen(true)
  }

  const handleAddBarcodeConfirm = async () => {
    if (!displayProduct || !newBarcode.trim()) return
    try {
      await addProductBarcode(displayProduct.id, newBarcode.trim())
      toast.success(t("barcodeAdded"))
      setNewBarcode("")
      setIsAddBarcodeOpen(false)
      if (onRefresh) onRefresh()
    } catch (e) {
      toast.error((e as Error).message || t("barcodeAddFailed"))
    }
  }

  const handleDeleteBarcode = async (product: Product, barcode: ProductBarcode) => {
    if (!confirm(t("deleteBarcodeConfirm"))) return
    try {
      await removeProductBarcode(product.id, barcode.id)
      toast.success(t("barcodeDeleted"))
      if (onRefresh) onRefresh()
    } catch (e) {
      toast.error((e as Error).message || t("barcodeDeleteFailed"))
    }
  }

  const handleSetDefault = async (product: Product, barcodeId: string) => {
    try {
      await setDefaultProductBarcode(product.id, barcodeId)
      toast.success(t("defaultBarcodeUpdated"))
      if (onRefresh) onRefresh()
    } catch {
      toast.error(t("defaultBarcodeUpdateFailed"))
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

  return (
    <>
      <div className="flex justify-end mb-4">
        <Button onClick={() => {
          setEditingProduct(null)
          setIsSheetOpen(true)
        }}>
          <Plus className="mr-2 h-4 w-4" /> {t("addProduct")}
        </Button>
      </div>

      <DataTable columns={columns} data={data} searchKey="name" />

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
          <div className="border-t pt-4">
            <div className="flex items-center gap-2 mb-2">
              <KeyRound className="h-4 w-4 text-muted-foreground" />
              <h4 className="text-sm font-semibold">{t("barcodes")}</h4>
            </div>
            {displayProduct.barcodes && displayProduct.barcodes.length > 0 ? (
              <div className="space-y-2">
                {displayProduct.barcodes.map((b) => (
                  <div key={b.id} className="flex items-center justify-between gap-2 rounded-lg border px-3 py-2">
                    <div className="flex items-center gap-2 min-w-0">
                      <span className="font-mono text-sm truncate">{b.barcode}</span>
                      {b.isDefault && (
                        <span className="shrink-0 inline-flex items-center rounded bg-primary/10 px-1.5 py-0.5 text-[10px] font-medium text-primary">
                          {t("primary")}
                        </span>
                      )}
                    </div>
                    {!b.isDefault && (
                      <div className="flex items-center gap-1 shrink-0">
                        <Button variant="ghost" size="sm" onClick={() => handleSetDefault(displayProduct, b.id)}>
                          {t("setAsDefault")}
                        </Button>
                        <Button variant="ghost" size="icon" onClick={() => handleDeleteBarcode(displayProduct, b)}>
                          <Trash className="h-4 w-4 text-destructive" />
                        </Button>
                      </div>
                    )}
                  </div>
                ))}
              </div>
            ) : (
              <p className="text-sm text-muted-foreground">{t("noBarcodes")}</p>
            )}
            <Button variant="outline" size="sm" className="mt-3" onClick={() => setIsAddBarcodeOpen(true)}>
              <Plus className="mr-2 h-4 w-4" /> {t("addBarcode")}
            </Button>
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
    </>
  )
}
