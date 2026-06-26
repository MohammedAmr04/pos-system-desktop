"use client"

import { Product } from "@prisma/client"
import { DataTable } from "@/components/common/data-table"
import { ResponsiveSheet } from "@/components/common/responsive-sheet"
import { ProductForm } from "./product-form"
import { useState } from "react"
import { Button } from "@/components/ui/button"
import { Plus, Edit, Trash } from "lucide-react"
import { ColumnDef } from "@tanstack/react-table"
import { deleteProduct } from "@/features/products/actions"
import { toast } from "sonner"

interface ProductsClientProps {
  data: Product[]
}

export function ProductsClient({ data }: ProductsClientProps) {
  const [isSheetOpen, setIsSheetOpen] = useState(false)
  const [editingProduct, setEditingProduct] = useState<Product | null>(null)

  const handleEdit = (product: Product) => {
    setEditingProduct(product)
    setIsSheetOpen(true)
  }

  const handleDelete = async (id: string) => {
    if (confirm("Are you sure you want to delete this product?")) {
      try {
        await deleteProduct(id)
        toast.success("Product deleted")
      } catch {
        toast.error("Error deleting product")
      }
    }
  }

  const columns: ColumnDef<Product>[] = [
    {
      accessorKey: "name",
      header: "Name",
    },
    {
      accessorKey: "barcode",
      header: "Barcode",
    },
    {
      accessorKey: "stockQuantity",
      header: "Stock",
    },
    {
      accessorKey: "salePrice",
      header: "Sale Price",
      cell: ({ row }) => `$${row.original.salePrice.toFixed(2)}`
    },
    {
      id: "actions",
      cell: ({ row }) => (
        <div className="flex gap-2">
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
          <Plus className="mr-2 h-4 w-4" /> Add Product
        </Button>
      </div>

      <DataTable columns={columns} data={data} searchKey="name" />

      <ResponsiveSheet
        open={isSheetOpen}
        onOpenChange={setIsSheetOpen}
        title={editingProduct ? "Edit Product" : "New Product"}
        description="Fill in the product details below."
      >
        <ProductForm 
          initialData={editingProduct} 
          onSuccess={() => setIsSheetOpen(false)} 
        />
      </ResponsiveSheet>
    </>
  )
}
