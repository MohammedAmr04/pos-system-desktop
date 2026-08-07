"use client"

import { Product } from "@/lib/api"
import { ResponsiveSheet } from "@/components/common/responsive-sheet"
import { ProductForm, PRODUCT_FORM_ID } from "./product-form"
import { useEffect, useState } from "react"
import { Button } from "@/components/ui/button"
import { Plus, Edit, Trash, Printer, Loader2 } from "lucide-react"
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
import { deleteProduct } from "@/features/products/actions"
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
  const [sorting, setSorting] = useState<SortingState>([])
  const [searchInput, setSearchInput] = useState(query)

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
          initialData={editingProduct} 
          onSuccess={() => {
            setIsSheetOpen(false)
            if (onRefresh) onRefresh()
          }} 
        />
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
    </>
  )
}
