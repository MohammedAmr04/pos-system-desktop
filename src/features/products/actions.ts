import { api, Product, ProductUnit } from "@/lib/api"

export const getProducts = async () => {
  return api.products.list()
}

export async function createProduct(data: {
  barcode?: string | null
  name: string
  buyPrice: number
  retailPrice: number
  wholesalePrice?: number | null
  stockQuantity: number
  unitName?: string | null
  allowDiscount?: boolean
  lowStockThreshold?: number
  notes?: string | null
}) {
  await api.products.create(data)
}

export async function updateProduct(
  id: string,
  data: {
    barcode?: string | null
    name: string
    buyPrice: number
    retailPrice: number
    wholesalePrice?: number | null
    stockQuantity: number
    unitName?: string | null
    allowDiscount?: boolean
    lowStockThreshold?: number
    notes?: string | null
  }
) {
  await api.products.update(id, data)
}

export async function deleteProduct(id: string) {
  await api.products.delete(id)
}

export async function addProductUnit(
  productId: string,
  data: { unitName: string; quantityFactor: number; retailPrice: number; wholesalePrice?: number | null }
) {
  return api.products.units.add(productId, data)
}

export async function updateProductUnit(
  productId: string,
  unitId: string,
  data: { unitName?: string; quantityFactor?: number; retailPrice?: number; wholesalePrice?: number | null }
) {
  return api.products.units.update(productId, unitId, data)
}

export async function deleteProductUnit(productId: string, unitId: string) {
  await api.products.units.remove(productId, unitId)
}

export async function addProductBarcode(productId: string, unitId: string, barcode: string) {
  return api.products.units.barcodes.add(productId, unitId, barcode)
}

export async function removeProductBarcode(productId: string, unitId: string, barcodeId: string) {
  await api.products.units.barcodes.remove(productId, unitId, barcodeId)
}

export async function setDefaultProductBarcode(productId: string, unitId: string, barcodeId: string) {
  await api.products.units.barcodes.setDefault(productId, unitId, barcodeId)
}

export const findUnitByBarcode = (product: Product, barcode: string): ProductUnit | null => {
  for (const unit of product.units ?? []) {
    for (const b of unit.barcodes ?? []) {
      if (b.barcode === barcode) return unit
    }
  }
  return null
}
