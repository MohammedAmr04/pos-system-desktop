"use client"

import { Input } from "@/components/ui/input"
import { SubmitButton } from "@/components/common/submit-button"
import { createProduct, updateProduct } from "@/features/products/actions"
import { Product } from "@prisma/client"
import { toast } from "sonner"
import { useRef } from "react"

interface ProductFormProps {
  initialData?: Product | null
  onSuccess: () => void
}

export function ProductForm({ initialData, onSuccess }: ProductFormProps) {
  const formRef = useRef<HTMLFormElement>(null)

  async function handleSubmit(formData: FormData) {
    const data = {
      name: formData.get("name") as string,
      barcode: formData.get("barcode") as string || null,
      buyPrice: parseFloat(formData.get("buyPrice") as string),
      salePrice: parseFloat(formData.get("salePrice") as string),
      stockQuantity: parseInt(formData.get("stockQuantity") as string, 10),
    }

    try {
      if (initialData) {
        await updateProduct(initialData.id, data)
        toast.success("Product updated!")
      } else {
        await createProduct(data)
        toast.success("Product created!")
        formRef.current?.reset()
      }
      onSuccess()
    } catch (e) {
      toast.error("An error occurred")
    }
  }

  return (
    <form ref={formRef} action={handleSubmit} className="space-y-4">
      <div>
        <label className="text-sm font-medium">Name *</label>
        <Input name="name" defaultValue={initialData?.name} required />
      </div>
      <div>
        <label className="text-sm font-medium">Barcode</label>
        <Input name="barcode" defaultValue={initialData?.barcode || ""} />
      </div>
      <div className="grid grid-cols-2 gap-4">
        <div>
          <label className="text-sm font-medium">Buy Price *</label>
          <Input name="buyPrice" type="number" step="0.01" defaultValue={initialData?.buyPrice} required />
        </div>
        <div>
          <label className="text-sm font-medium">Sale Price *</label>
          <Input name="salePrice" type="number" step="0.01" defaultValue={initialData?.salePrice} required />
        </div>
      </div>
      <div>
        <label className="text-sm font-medium">Stock Quantity *</label>
        <Input name="stockQuantity" type="number" defaultValue={initialData?.stockQuantity} required />
      </div>
      <SubmitButton className="w-full">
        {initialData ? "Update Product" : "Create Product"}
      </SubmitButton>
    </form>
  )
}
