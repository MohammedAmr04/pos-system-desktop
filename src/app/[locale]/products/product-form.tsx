"use client"

import { Input } from "@/components/ui/input"
import { createProduct, updateProduct } from "@/features/products/actions"
import { Product } from "@prisma/client"
import { toast } from "sonner"
import { useRef } from "react"
import { useTranslations } from "next-intl"

interface ProductFormProps {
  initialData?: Product | null
  onSuccess: () => void
}

export const PRODUCT_FORM_ID = "product-form"

export function ProductForm({ initialData, onSuccess }: ProductFormProps) {
  const t = useTranslations("Products")
  const formRef = useRef<HTMLFormElement>(null)

  async function handleSubmit(formData: FormData) {
    const data = {
      name: formData.get("name") as string,
      barcode: formData.get("barcode") as string || undefined,
      buyPrice: parseFloat(formData.get("buyPrice") as string),
      salePrice: parseFloat(formData.get("salePrice") as string),
      stockQuantity: parseInt(formData.get("stockQuantity") as string, 10),
    }

    try {
      if (initialData) {
        await updateProduct(initialData.id, data)
        toast.success(t("productUpdated"))
      } else {
        await createProduct(data)
        toast.success(t("productCreated"))
        formRef.current?.reset()
      }
      onSuccess()
    } catch (e) {
      toast.error(t("createError"))
    }
  }

  return (
    <form ref={formRef} id={PRODUCT_FORM_ID} action={handleSubmit} className="space-y-6">
      <div className="space-y-2">
        <label className="text-sm font-medium">{t("name")} *</label>
        <Input name="name" defaultValue={initialData?.name} required />
      </div>
      <div className="space-y-2">
        <label className="text-sm font-medium">{t("barcode")}</label>
        <Input name="barcode" defaultValue={initialData?.barcode || ""} placeholder={t("barcode")} />
      </div>
      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-2">
          <label className="text-sm font-medium">{t("buyPrice")} *</label>
          <Input name="buyPrice" type="number" step="0.01" defaultValue={initialData?.buyPrice} required />
        </div>
        <div className="space-y-2">
          <label className="text-sm font-medium">{t("salePrice")} *</label>
          <Input name="salePrice" type="number" step="0.01" defaultValue={initialData?.salePrice} required />
        </div>
      </div>
      <div className="space-y-2">
        <label className="text-sm font-medium">{t("stockQuantity")} *</label>
        <Input name="stockQuantity" type="number" defaultValue={initialData?.stockQuantity} required />
      </div>
    </form>
  )
}
