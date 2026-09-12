import { Product, ProductUnit } from "@/types/domain/domain.types"

export const findUnitByBarcode = (product: Product, barcode: string): ProductUnit | null => {
  for (const unit of product.units ?? []) {
    for (const b of unit.barcodes ?? []) {
      if (b.barcode === barcode) return unit
    }
  }
  return null
}

export const allBarcodes = (p: Product): string[] => [
  ...(p.barcodes ?? []).map((b) => b.barcode),
  ...(p.barcode ? [p.barcode] : []),
]

export const baseUnitOf = (p: Product): ProductUnit | null =>
  p.units?.find((u) => u.isBaseUnit) ?? p.units?.[0] ?? null

export const resolveBarcode = (
  list: Product[],
  barcode: string
): { product: Product; unit: ProductUnit } | null => {
  const b = barcode.trim()
  for (const p of list) {
    const unit = findUnitByBarcode(p, b)
    if (unit) return { product: p, unit }
    if (p.barcode === b) {
      const bu = baseUnitOf(p)
      if (bu) return { product: p, unit: bu }
    }
  }
  return null
}
