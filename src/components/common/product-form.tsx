"use client"

import { useForm, Controller } from "react-hook-form"
import { useState } from "react"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import { Input } from "@/components/ui/input"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { createProduct, updateProduct } from "@/actions/products.actions"
import { baseUnitOf } from "@/lib/barcode"
import { Product } from "@/types/domain/domain.types"
import { useAllBrands } from "@/hooks/use-brands"
import { useAllCategories } from "@/hooks/use-categories"
import { useAllUnits } from "@/hooks/use-units"
import { toast } from "sonner"
import { useTranslations } from "next-intl"
import { useApiError } from "@/lib/api-error"
import { useAllProducts } from "@/hooks/use-products"
import { Trash2 } from "lucide-react"
import { Button } from "@/components/ui/button"

interface ProductFormProps {
  initialData?: Product | null
  defaultBarcode?: string
  onSuccess?: (product?: Product) => void
}

export const PRODUCT_FORM_ID = "product-form"

const money = (requiredMessage: string, invalidMessage: string) =>
  z
    .string()
    .trim()
    .min(1, requiredMessage)
    .refine((v) => Number.isFinite(parseFloat(v)), invalidMessage)

const optionalMoney = (invalidMessage: string) =>
  z
    .string()
    .trim()
    .refine((v) => v === "" || Number.isFinite(parseFloat(v)), invalidMessage)

const nonNegativeInt = (invalidMessage: string) =>
  z
    .string()
    .refine((v) => v === "" || (Number.isInteger(parseInt(v, 10)) && parseInt(v, 10) >= 0), invalidMessage)

function makeProductSchema(t: (key: string) => string) {
  const required = t("fieldRequired")
  const invalid = t("invalidNumber")
  return z.object({
    name: z.string().trim().min(1, required),
    barcode: z.string(),
    categoryId: z.string(),
    brandId: z.string(),
    unitId: z.string(),
    retailPrice: money(required, invalid).refine((v) => parseFloat(v) >= 0, invalid),
    wholesalePrice: optionalMoney(invalid),
    lowStockThreshold: nonNegativeInt(invalid),
    allowDiscount: z.boolean(),
    isHiddenFromPOS: z.boolean(),
    notes: z.string(),
    productType: z.enum(["product", "service", "bundle"]),
    serviceCost: optionalMoney(invalid),
  })
}

type ProductFormValues = z.infer<ReturnType<typeof makeProductSchema>>

const errorClass = "text-sm text-destructive"

function FieldError({ message }: { message?: string }) {
  if (!message) return null
  return <p className={errorClass}>{message}</p>
}

export function ProductForm({ initialData, defaultBarcode, onSuccess }: ProductFormProps) {
  const t = useTranslations("Products")
  const resolveError = useApiError()
  const { data: categories = [] } = useAllCategories()
  const { data: brands = [] } = useAllBrands()
  const { data: units = [] } = useAllUnits()
  const { data: allProducts = [] } = useAllProducts()

  const baseUnit = initialData ? baseUnitOf(initialData) : null
  const defaultUnitName = t("defaultUnitName")

  const selectableCategories = categories.filter(
    (c) => c.isActive || c.id === initialData?.categoryId
  )
  const selectableBrands = brands.filter((b) => b.isActive || b.id === initialData?.brandId)
  const selectableUnits = units.filter((u) => u.isActive)
  const defaultUnitId = baseUnit?.unitId ?? selectableUnits.find((u) => u.isActive)?.id ?? ""
  const [bundleComponents, setBundleComponents] = useState<Array<{ productId: string; quantity: string }>>(
    () => initialData?.bundleComponents?.map((c) => ({ productId: c.componentProductId, quantity: String(c.quantity) })) ?? []
  )

  const schema = makeProductSchema(t as (key: string) => string)

  const {
    register,
    handleSubmit,
    reset,
    control,
    watch,
    formState: { errors },
  } = useForm<ProductFormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      name: initialData?.name ?? "",
      barcode: initialData?.barcode || defaultBarcode || "",
      categoryId: initialData?.categoryId ?? "",
      brandId: initialData?.brandId ?? "",
      unitId: defaultUnitId,
      retailPrice:
        (baseUnit?.retailPrice ?? initialData?.salePrice) != null
          ? String(baseUnit?.retailPrice ?? initialData?.salePrice)
          : "",
      wholesalePrice: baseUnit?.wholesalePrice != null ? String(baseUnit.wholesalePrice) : "",
      lowStockThreshold: String(initialData?.lowStockThreshold ?? 0),
      allowDiscount: initialData?.allowDiscount ?? true,
      isHiddenFromPOS: initialData?.isHiddenFromPOS ?? false,
      notes: initialData?.notes ?? "",
      productType: initialData?.productType ?? "product",
      serviceCost: initialData?.serviceCost ? String(initialData.serviceCost) : "",
    },
  })
  const productType = watch("productType")

  async function onSubmit(values: ProductFormValues) {
    const selectedUnit = units.find((u) => u.id === values.unitId)
    const wholesaleRaw = values.wholesalePrice.trim()
    const serviceCost = values.serviceCost.trim() ? parseFloat(values.serviceCost) : 0
    const lowStockThreshold = values.productType === "product" ? parseInt(values.lowStockThreshold, 10) || 0 : undefined
    const components = bundleComponents
      .filter((component) => component.productId && parseFloat(component.quantity) > 0)
      .map((component) => ({ productId: component.productId, quantity: parseFloat(component.quantity) }))
    if (values.productType === "bundle" && components.length === 0) {
      toast.error(t("bundleNeedsComponents"))
      return
    }
    try {
      if (initialData) {
        await updateProduct(initialData.id, {
          name: values.name,
          barcode: values.barcode.trim() || undefined,
          retailPrice: parseFloat(values.retailPrice),
          wholesalePrice: wholesaleRaw ? parseFloat(wholesaleRaw) : null,
          unitId: values.unitId || undefined,
          unitName: selectedUnit ? selectedUnit.name : undefined,
          categoryId: values.categoryId || "",
          brandId: values.brandId || "",
          notes: values.notes || null,
          allowDiscount: values.allowDiscount,
          isHiddenFromPOS: values.isHiddenFromPOS,
          lowStockThreshold,
          productType: values.productType,
          serviceCost,
          bundleComponents: components,
        })
        toast.success(t("productUpdated"))
      } else {
        const product = await createProduct({
          name: values.name,
          barcode: values.barcode.trim() || undefined,
          retailPrice: parseFloat(values.retailPrice),
          wholesalePrice: wholesaleRaw ? parseFloat(wholesaleRaw) : null,
          unitId: values.unitId || undefined,
          unitName: selectedUnit ? selectedUnit.name : defaultUnitName,
          categoryId: values.categoryId || "",
          brandId: values.brandId || "",
          notes: values.notes || null,
          allowDiscount: values.allowDiscount,
          isHiddenFromPOS: values.isHiddenFromPOS,
          lowStockThreshold,
          productType: values.productType,
          serviceCost,
          bundleComponents: components,
        })
        toast.success(t("productCreated"))
        onSuccess?.(product)
        reset()
      }
    } catch (e) {
      toast.error(resolveError(e) || t("createError"))
    }
  }

  return (
    <form id={PRODUCT_FORM_ID} noValidate onSubmit={handleSubmit(onSubmit)} className="space-y-6">
      <div className="space-y-2">
        <label className="text-sm font-medium">{t("name")} *</label>
        <Input {...register("name")} />
        <FieldError message={errors.name?.message} />
      </div>
      <div className="space-y-2">
        <label className="text-sm font-medium">{t("productType")}</label>
        <Controller
          name="productType"
          control={control}
          render={({ field }) => (
            <select {...field} className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm">
              <option value="product">{t("productTypeProduct")}</option>
              <option value="service">{t("productTypeService")}</option>
              <option value="bundle">{t("productTypeBundle")}</option>
            </select>
          )}
        />
      </div>
      <div className="space-y-2">
        <label className="text-sm font-medium">{t("barcode")}</label>
        <Input {...register("barcode")} placeholder={t("barcode")} dir="ltr" />
      </div>
      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-2">
          <label className="text-sm font-medium">{t("category")}</label>
          <Controller
            name="categoryId"
            control={control}
            render={({ field }) => (
              <Select
                value={field.value}
                onValueChange={(v) => v != null && field.onChange(v)}
                items={{
                  "": t("noCategory"),
                  ...Object.fromEntries(selectableCategories.map((c) => [c.id, c.name])),
                }}
              >
                <SelectTrigger className="w-full">
                  <SelectValue placeholder={t("noCategory")} />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="">{t("noCategory")}</SelectItem>
                  {selectableCategories.map((c) => (
                    <SelectItem key={c.id} value={c.id} disabled={!c.isActive}>
                      {c.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            )}
          />
        </div>
        <div className="space-y-2">
          <label className="text-sm font-medium">{t("brand")}</label>
          <Controller
            name="brandId"
            control={control}
            render={({ field }) => (
              <Select
                value={field.value}
                onValueChange={(v) => v != null && field.onChange(v)}
                items={{
                  "": t("noBrand"),
                  ...Object.fromEntries(selectableBrands.map((b) => [b.id, b.name])),
                }}
              >
                <SelectTrigger className="w-full">
                  <SelectValue placeholder={t("noBrand")} />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="">{t("noBrand")}</SelectItem>
                  {selectableBrands.map((b) => (
                    <SelectItem key={b.id} value={b.id} disabled={!b.isActive}>
                      {b.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            )}
          />
        </div>
      </div>
      {productType === "service" ? (
          <div className="space-y-2">
            <label className="text-sm font-medium">{t("serviceCost")}</label>
            <Input {...register("serviceCost")} type="number" min="0" step="0.01" dir="ltr" />
            <FieldError message={errors.serviceCost?.message} />
          </div>
        ) : productType === "bundle" ? (
          <div className="space-y-3 rounded-md border p-3">
            <div className="flex items-center justify-between">
              <label className="text-sm font-medium">{t("bundleComponents")}</label>
              <Button type="button" variant="outline" size="sm" onClick={() => setBundleComponents((current) => [...current, { productId: "", quantity: "1" }])}>
                {t("addComponent")}
              </Button>
            </div>
            {bundleComponents.map((component, index) => {
              const available = allProducts.filter((product) => product.id !== initialData?.id && product.productType !== "bundle")
              return (
                <div key={`${index}-${component.productId}`} className="flex items-center gap-2">
                  <select
                    value={component.productId}
                    onChange={(event) => setBundleComponents((current) => current.map((item, i) => i === index ? { ...item, productId: event.target.value } : item))}
                    className="h-9 min-w-0 flex-1 rounded-md border border-input bg-background px-2 text-sm"
                  >
                    <option value="">{t("selectComponent")}</option>
                    {available.map((product) => <option key={product.id} value={product.id}>{product.name}</option>)}
                  </select>
                  <Input
                    type="number"
                    min="0.001"
                    step="0.001"
                    value={component.quantity}
                    onChange={(event) => setBundleComponents((current) => current.map((item, i) => i === index ? { ...item, quantity: event.target.value } : item))}
                    className="w-24"
                    dir="ltr"
                  />
                  <Button type="button" variant="ghost" size="icon" onClick={() => setBundleComponents((current) => current.filter((_, i) => i !== index))}>
                    <Trash2 className="h-4 w-4 text-destructive" />
                  </Button>
                </div>
              )
            })}
          </div>
        ) : null}
      <div className="space-y-2">
        <label className="text-sm font-medium">{t("unitName")}</label>
        <Controller
          name="unitId"
          control={control}
          render={({ field }) => (
            <Select
              value={field.value}
              onValueChange={(v) => v != null && field.onChange(v)}
              items={{
                ...Object.fromEntries(selectableUnits.map((u) => [u.id, u.name])),
              }}
            >
              <SelectTrigger className="w-full">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {selectableUnits
                  .filter((u) => u.isActive || u.id === field.value)
                  .map((u) => (
                    <SelectItem key={u.id} value={u.id}>
                      {u.name}
                    </SelectItem>
                  ))}
              </SelectContent>
            </Select>
          )}
        />
      </div>
      <div className="grid grid-cols-2 gap-4">
        {initialData && (
          <div className="space-y-2">
            <label className="text-sm font-medium">{t("buyPrice")}</label>
            <Input value={String(initialData.buyPrice)} disabled readOnly dir="ltr" />
            <p className="text-xs text-muted-foreground">{t("buyPriceHint")}</p>
          </div>
        )}
        <div className="space-y-2">
          <label className="text-sm font-medium">{t("retailPrice")} *</label>
          <Input {...register("retailPrice")} type="number" step="1" dir="ltr" />
          <FieldError message={errors.retailPrice?.message} />
        </div>
      </div>
      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-2">
          <label className="text-sm font-medium">{t("wholesalePrice")}</label>
          <Input {...register("wholesalePrice")} type="number" step="1" placeholder={t("optional")} dir="ltr" />
          <FieldError message={errors.wholesalePrice?.message} />
        </div>
        {initialData && (
          <div className="space-y-2">
            <label className="text-sm font-medium">{t("stockQuantity")}</label>
            <Input value={String(initialData.stockQuantity)} disabled readOnly dir="ltr" />
            <p className="text-xs text-muted-foreground">{t("stockHint")}</p>
          </div>
        )}
      </div>
      <div className="grid grid-cols-2 gap-4">
        {productType === "product" && (
          <div className="space-y-2">
            <label className="text-sm font-medium">{t("lowStockThreshold")}</label>
            <Input {...register("lowStockThreshold")} type="number" min="0" dir="ltr" />
            <FieldError message={errors.lowStockThreshold?.message} />
          </div>
        )}
        <div className={productType === "product" ? "space-y-2 flex items-end pb-1" : "col-span-2 space-y-2 flex items-end pb-1"}>
          <div className="flex flex-col gap-2">
            <label className="flex items-center gap-2 text-sm font-medium cursor-pointer">
              <input
                type="checkbox"
                {...register("allowDiscount")}
                className="h-4 w-4 rounded border-input"
              />
              {t("allowDiscount")}
            </label>
            <label className="flex items-center gap-2 text-sm font-medium cursor-pointer">
              <input
                type="checkbox"
                {...register("isHiddenFromPOS")}
                className="h-4 w-4 rounded border-input"
              />
              {t("hideFromPOS")}
            </label>
          </div>
        </div>
      </div>
      <div className="space-y-2">
        <label className="text-sm font-medium">{t("notes")}</label>
        <textarea
          {...register("notes")}
          className="h-24 w-full min-w-0 rounded-lg border border-input bg-transparent px-2.5 py-1.5 text-base transition-colors outline-none placeholder:text-muted-foreground focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 disabled:pointer-events-none disabled:cursor-not-allowed disabled:bg-input/50 disabled:opacity-50 aria-invalid:border-destructive aria-invalid:ring-3 aria-invalid:ring-destructive/20 md:text-sm dark:bg-input/30 dark:disabled:bg-input/80 dark:aria-invalid:border-destructive/50 dark:aria-invalid:ring-destructive/40 resize-y"
          placeholder={t("notes")}
        />
      </div>
    </form>
  )
}
