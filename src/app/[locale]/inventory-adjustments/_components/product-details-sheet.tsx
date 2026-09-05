"use client"

import { useTranslations } from "next-intl"
import { ResponsiveSheet } from "@/components/common/responsive-sheet"
import { useProduct } from "@/hooks/use-products"

export function ProductDetailsSheet({
  productId,
  open,
  onOpenChange,
}: {
  productId: string | null
  open: boolean
  onOpenChange: (open: boolean) => void
}) {
  const t = useTranslations("InventoryCounts")
  const { data: product } = useProduct(productId ?? "", !!productId && open)
  return (
    <ResponsiveSheet open={open} onOpenChange={onOpenChange} title={t("productDetails")}>
      {product && (
        <div className="space-y-5 text-sm">
          <div>
            <p className="font-semibold">{product.name}</p>
            <p className="text-muted-foreground" dir="ltr">{product.barcode ?? "-"}</p>
          </div>
          <div className="grid grid-cols-2 gap-3">
            <p>{t("stock")}: <span dir="ltr">{product.stockQuantity}</span></p>
            <p>{t("lowStock")}: <span dir="ltr">{product.lowStockThreshold}</span></p>
            <p>{t("cost")}: <span dir="ltr">{product.buyPrice.toFixed(2)}</span></p>
            <p>{t("posVisibility")}: {product.isHiddenFromPOS ? t("hidden") : t("visible")}</p>
          </div>
          <div>
            <p className="mb-2 font-medium">{t("units")}</p>
            <div className="space-y-2">
              {product.units?.map((unit) => (
                <div key={unit.id} className="rounded-lg border p-3">
                  <div className="flex justify-between gap-3">
                    <span>{unit.unitName}</span>
                    <span dir="ltr">x{unit.quantityFactor}</span>
                  </div>
                  <div className="mt-1 flex gap-3 text-muted-foreground" dir="ltr">
                    <span>{t("retail")}: {unit.retailPrice.toFixed(2)}</span>
                    <span>{t("wholesale")}: {unit.wholesalePrice?.toFixed(2) ?? "-"}</span>
                  </div>
                  <p className="mt-1 text-muted-foreground" dir="ltr">
                    {unit.barcodes?.map((barcode) => barcode.barcode).join(" · ") || "-"}
                  </p>
                </div>
              ))}
            </div>
          </div>
        </div>
      )}
    </ResponsiveSheet>
  )
}
