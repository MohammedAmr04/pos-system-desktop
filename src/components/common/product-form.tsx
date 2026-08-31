"use client"

import { useForm, Controller } from "react-hook-form"
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

  const baseUnit = initialData ? baseUnitOf(initialData) : null
  const defaultUnitName = t("defaultUnitName")

  const selectableCategories = categories.filter(
    (c) => c.isActive || c.id === initialData?.categoryId
  )
  const selectableBrands = brands.filter((b) => b.isActive || b.id === initialData?.brandId)
  const selectableUnits = units.filter((u) => u.isActive)
  const defaultUnitId = baseUnit?.unitId ?? selectableUnits.find((u) => u.isActive)?.id ?? ""

  const schema = makeProductSchema(t as (key: string) => string)

  const {
    register,
    handleSubmit,
    reset,
    control,
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
    },
  })

  async function onSubmit(values: ProductFormValues) {
    const selectedUnit = units.find((u) => u.id === values.unitId)
    const wholesaleRaw = values.wholesalePrice.trim()
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
          lowStockThreshold: parseInt(values.lowStockThreshold, 10) || 0,
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
          lowStockThreshold: parseInt(values.lowStockThreshold, 10) || 0,
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
        <div className="space-y-2">
          <label className="text-sm font-medium">{t("lowStockThreshold")}</label>
          <Input {...register("lowStockThreshold")} type="number" min="0" dir="ltr" />
          <FieldError message={errors.lowStockThreshold?.message} />
        </div>
        <div className="space-y-2 flex items-end pb-1">
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
