"use client"

import { Input } from "@/components/ui/input"
import { createProduct, updateProduct } from "@/features/products/actions"
import { Product } from "@/lib/api"
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
      notes: formData.get("notes") as string || null,
      allowDiscount: formData.get("allowDiscount") === "on",
      lowStockThreshold: parseInt(formData.get("lowStockThreshold") as string, 10) || 0,
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
      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-2">
          <label className="text-sm font-medium">{t("lowStockThreshold")}</label>
          <Input name="lowStockThreshold" type="number" min="0" defaultValue={initialData?.lowStockThreshold ?? 0} />
        </div>
        <div className="space-y-2 flex items-end pb-1">
          <label className="flex items-center gap-2 text-sm font-medium cursor-pointer">
            <input
              type="checkbox"
              name="allowDiscount"
              defaultChecked={initialData?.allowDiscount ?? true}
              className="h-4 w-4 rounded border-input"
            />
            {t("allowDiscount")}
          </label>
        </div>
      </div>
      <div className="space-y-2">
        <label className="text-sm font-medium">{t("notes")}</label>
        <textarea
          name="notes"
          defaultValue={initialData?.notes || ""}
          className="h-24 w-full min-w-0 rounded-lg border border-input bg-transparent px-2.5 py-1.5 text-base transition-colors outline-none placeholder:text-muted-foreground focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 disabled:pointer-events-none disabled:cursor-not-allowed disabled:bg-input/50 disabled:opacity-50 aria-invalid:border-destructive aria-invalid:ring-3 aria-invalid:ring-destructive/20 md:text-sm dark:bg-input/30 dark:disabled:bg-input/80 dark:aria-invalid:border-destructive/50 dark:aria-invalid:ring-destructive/40 resize-y"
          placeholder={t("notes")}
        />
      </div>
    </form>
  )
}
